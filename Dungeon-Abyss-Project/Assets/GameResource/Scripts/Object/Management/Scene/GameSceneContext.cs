using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DdaIT.AddressableKey;
using DdaIT.Object.Character;
using DdaIT.Object.Character.Player;
using DdaIT.Object.Session;
using DdaIT.Object.UI;
using DdaIT.Util.Debug;
using R3;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.iOS;

namespace DdaIT.Object.Management
{
    public sealed class GameSceneContext : SceneContext
    {
        private GameObject _loopPrefab;
        private GameObject _playerPrefab;
        private GameObject _sessionPrefab;
        private GameObject _vcamPrefab;
        private GameObject _vcamInstance;
        private GameObject _loopInstance;
        private GameObject _sessionInstance;
        private PlayerSessionActor[] _actors;

        private RunSession _session;
        private readonly Dictionary<ulong, byte> _clientSlots = new();

        private IDisposable _phaseSub;
        public override async UniTask PreloadAsync()
        {
            Debugger.LogMessage("GameSceneContext PreloadAsync");

            var loopTask = ResourceManager.LoadResourceAsync<GameObject>(AddressableKeys.InGame.Get<HostRunLoop>());
            var playerTask = ResourceManager.LoadResourceAsync<GameObject>(AddressableKeys.InGame.Get("Player"));
            var sessionTask = ResourceManager.LoadResourceAsync<GameObject>(AddressableKeys.Network.Get<RunSession>());
            var vcamTask = ResourceManager.LoadResourceAsync<GameObject>(AddressableKeys.InGame.Get("PlayerFollowCamera"));
            var hudTask = UIManager.PreloadAsync<PlayerHudPanel>();

            (_loopPrefab, _playerPrefab, _vcamPrefab, _sessionPrefab) = await UniTask.WhenAll(loopTask, playerTask, vcamTask, sessionTask);
            await hudTask;
        }

        public override async UniTask EnterAsync()
        {
            Debugger.LogMessage("GameSceneContext EnterAsync");

            var network = NetworkManager.Singleton;
            if (network == null)
            {
                Debugger.LogError("NetworkManager is not found.");
                return;
            }

            if (network.IsServer)
            {
                await EnterServerAsync();
            }
            else
            {
                await EnterClientAsync();
            }
        }

        public override UniTask ExitAsync()
        {
            _phaseSub?.Dispose();
            _phaseSub = null;

            var network = NetworkManager.Singleton;
            if (network != null && network.IsServer)
            {
                network.OnClientConnectedCallback -= OnClientConnected;
                network.OnClientDisconnectCallback -= OnClientDisconnect;

                DespawnActors();
                DespawnSession();
            }

            _clientSlots.Clear();
            _session = null;

            if (_vcamInstance != null)
            {
                UnityEngine.Object.Destroy(_vcamInstance);
                _vcamInstance = null;
            }

            if (_loopInstance != null)
            {
                UnityEngine.Object.Destroy(_loopInstance);
                _loopInstance = null;
            }

            return UniTask.CompletedTask;
        }

        private void DespawnActors()
        {
            if (_actors == null)
                return;

            for (int i = 0; i < _actors.Length; i++)
            {
                if (_actors[i] == null)
                    continue;

                if (_actors[i].TryGetComponent<NetworkObject>(out var net) && net.IsSpawned)
                {
                    net.Despawn();
                }
                else
                {
                    UnityEngine.Object.Destroy(_actors[i].gameObject);
                }
            }
        }
        private void DespawnSession()
        {
            if (_sessionInstance == null)
                return;

            if (_sessionInstance.TryGetComponent<NetworkObject>(out var net) && net.IsSpawned)
            {
                net.Despawn();
            }
            else
            {
                UnityEngine.Object.Destroy(_sessionInstance);
            }
            _sessionInstance = null;
        }

        #region Server && Client Entry
        public async UniTask EnterServerAsync()
        {
            //세션 생성
            if (!UnityEngine.Object.Instantiate(_sessionPrefab).TryGetComponent<RunSession>(out var session))
            {
                Debugger.LogError("RunSession not found");
                return;
            }

            _sessionInstance = session.gameObject;
            if (!_sessionInstance.TryGetComponent<NetworkObject>(out var sessionNet))
            {
                Debugger.LogError("NetworkObject not found");
                return;
            }
            sessionNet.Spawn();
            _session = session;

            //루프 인스턴스 생성
            if (!UnityEngine.Object.Instantiate(_loopPrefab).TryGetComponent<HostRunLoop>(out var loop))
            {
                Debugger.LogError("HostRunLoop not found");
                return;
            }
            _loopInstance = loop.gameObject;

            _actors = new PlayerSessionActor[RunRules.MaxPlayer];

            var network = NetworkManager.Singleton;
            var localActor = SpawnJoinedPlayer(network.LocalClientId, isLocal: true);
            if (localActor == null)
                return;

            byte localSlot = localActor.Slot.Value;

            var hud = await UIManager.OpenAsync<PlayerHudPanel>();
            if(localActor.TryGetComponent<PlayerNetwork>(out var localNet)){
                hud.Bind(localNet);
            }

            loop.Bind(session.Simulation, session.State, _actors, localSlot);

            network.OnClientConnectedCallback += OnClientConnected;
            network.OnClientDisconnectCallback += OnClientDisconnect;
            foreach (var clientId in network.ConnectedClientsIds)
            {
                OnClientConnected(clientId);
            }

            _phaseSub = session.Events.PhaseChanged.Subscribe(phase =>
            {
                if (phase == RunPhase.PartyDown)
                {
                    GameManager.GameOver();
                    Debugger.LogMessage("Party Down");
                }
            });
        }

        public async UniTask EnterClientAsync()
        {
            var network = NetworkManager.Singleton;
            if (!network.IsClient && !network.StartClient())
            {
                Debugger.LogError("Failed to start client");
                return;
            }

            var localPlayer = await WaitForLocalPlayerAsync(network);
            if (localPlayer == null || !localPlayer.TryGetComponent<PlayerSessionActor>(out var actor))
            {
                Debugger.LogError("Local Player not found");
                return;
            }

            AttachLocalCamera(actor);
            var hud = await UIManager.OpenAsync<PlayerHudPanel>();
            hud.Bind(localPlayer);
            await UniTask.NextFrame();
        }

        #endregion

        #region Connection CallBacks
        private void OnClientConnected(ulong clientId)
        {
            if (_clientSlots.ContainsKey(clientId))
            {
                return;
            }

            if (SpawnJoinedPlayer(clientId, isLocal: false) == null)
            {
                NetworkManager.Singleton.DisconnectClient(clientId);
            }

        }
        private void OnClientDisconnect(ulong clientId)
        {
            if (!_clientSlots.Remove(clientId, out byte slot))
            {
                return;
            }

            var state = _session.State;
            var player = state.Players[slot];
            player.IsJoined = false;
            player.LifeState = LifeState.Absent;
            state.Players[slot] = player;

            _actors[slot] = null;
        }

        #endregion

        #region PlayerSpawn && Camera Attach Methods
        private PlayerSessionActor SpawnJoinedPlayer(ulong clientId, bool isLocal)
        {

            if (!TryFindFreeSlot(out byte slot))
            {
                Debugger.LogError("No free slot found");
                return null;
            }

            var actor = SpawnPlayer(slot, clientId);
            if (actor == null)
                return null;

            var state = _session.State;
            var player = state.Players[slot];
            player.IsJoined = true;
            player.LifeState = LifeState.Alive;
            
            player.AnimationParams = default;
            player.AnimationParams.LifeState = LifeState.Alive;
            player.Aiming = false;
            player.Yaw = actor.CachedTransform.eulerAngles.y;

            player.Position = actor.CachedTransform.position;
            player.Combat.Hp = player.Combat.MaxHp = 20f;
            player.Stamina = player.MaxStamina = 10f;
            player.Hunger = player.MaxHunger = 10f;
            player.Combat.BaseAttack = 10f;
            player.Combat.Attack = 10f;
            player.Combat.MoveSpeedMul = 1f;
            state.Players[slot] = player;

            _session.Simulation.ResetPlayerTimer(new SlotId(slot));

            if(actor.TryGetComponent<PlayerNetwork>(out var playerNet)){
                playerNet.ServerSetVitals(new PlayerVital{
                    Health = player.Combat.Hp,
                    MaxHealth = player.Combat.MaxHp,
                    Stamina = player.Stamina,
                    MaxStamina = player.MaxStamina,
                    Hunger = player.Hunger,
                    MaxHunger = player.MaxHunger,
                });
                playerNet.ServerSetLifeState(player.LifeState);
            }

            actor.Bind(_session.Events);

            if(isLocal){
                AttachLocalCamera(actor);
            }

            _actors[slot] = actor;
            _clientSlots[clientId] = slot;
            return actor;
        }

        private bool TryFindFreeSlot(out byte slot)
        {
            var players = _session.State.Players;
            for (int i = 0; i < players.Length; i++)
            {
                if (!players[i].IsJoined)
                {
                    slot = (byte)i;
                    return true;
                }
            }

            slot = 0;
            return false;
        }

        private PlayerSessionActor SpawnPlayer(byte slot, ulong ownerClientId)
        {
            var playerObject = UnityEngine.Object.Instantiate(_playerPrefab, new Vector3(slot * 3, 5, 0), Quaternion.identity);

            if (!playerObject.TryGetComponent<PlayerSessionActor>(out var actor) ||
                !playerObject.TryGetComponent<PlayerNetwork>(out var playerNet) ||
                !playerObject.TryGetComponent<NetworkObject>(out var playerObjectNet)
            )
            {
                Debugger.LogError("PlayerPrefab is Missing Component (PlayerSessionActor, NetworkObject, PlayerSessionActor)");
                UnityEngine.Object.Destroy(playerObject);
                return null;
            }

            playerNet.PrepareSlot(slot);
            playerObjectNet.SpawnAsPlayerObject(ownerClientId);
            return actor;
        }

        private void AttachLocalCamera(PlayerSessionActor actor)
        {
            var vcamObject = UnityEngine.Object.Instantiate(_vcamPrefab);
            _vcamInstance = vcamObject;

            if (!vcamObject.TryGetComponent<CinemachineCamera>(out var vcam))
            {
                Debugger.LogError("CinemachineCamera not found");
                return;
            }

            vcam.Target.TrackingTarget = actor.CachedTransform;
            actor.SetViewCamera(Camera.main);
        }

        #endregion

        #region Static Methods
        private static async UniTask<PlayerNetwork> WaitForLocalPlayerAsync(NetworkManager network)
        {
            while (network.IsListening)
            {
                var players = UnityEngine.Object.FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player.IsOwner)
                    {
                        return player;
                    }
                }
                await UniTask.Yield();
            }
            return null;
        }

        #endregion
    }
}
