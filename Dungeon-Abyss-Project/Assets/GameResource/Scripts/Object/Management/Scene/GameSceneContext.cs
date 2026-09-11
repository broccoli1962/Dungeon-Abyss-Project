using System;
using Cysharp.Threading.Tasks;
using DdaIT.AddressableKey;
using DdaIT.Object.Character.Player;
using DdaIT.Object.Session;
using DdaIT.Object.UI;
using DdaIT.Util.Debug;
using R3;
using Unity.Cinemachine;
using UnityEngine;

namespace DdaIT.Object.Management
{
    public sealed class GameSceneContext : SceneContext
    {
        private GameObject _loopPrefab;
        private GameObject _playerPrefab;
        private GameObject _vcamPrefab;
        private GameObject _vcamInstance;
        private GameObject _loopInstance;
        private PlayerSessionActor[] _actors;

        private IDisposable _phaseSub;
        public override async UniTask PreloadAsync()
        {
            Debugger.LogMessage("GameSceneContext PreloadAsync");

            var loopTask = ResourceManager.LoadResourceAsync<GameObject>(AddressableKeys.InGame.Get<HostRunLoop>());
            var playerTask = ResourceManager.LoadResourceAsync<GameObject>(AddressableKeys.InGame.Get("Player"));
            var vcamTask = ResourceManager.LoadResourceAsync<GameObject>(AddressableKeys.InGame.Get("PlayerFollowCamera"));
            var hudTask = UIManager.PreloadAsync<PlayerHudPanel>();

            (_loopPrefab, _playerPrefab, _vcamPrefab) = await UniTask.WhenAll(loopTask, playerTask, vcamTask);
            await hudTask;
        }

        public override async UniTask EnterAsync()
        {
            Debugger.LogMessage("GameSceneContext EnterAsync");

            var state = new RunState();
            var events = new RunEvents();
            var sim = new RunSimulation(state, events, new SoloHostAuthority());

            //루프 인스턴스 생성
            if (!UnityEngine.Object.Instantiate(_loopPrefab).TryGetComponent<HostRunLoop>(out var loop))
            {
                Debugger.LogError("HostRunLoop not found");
                return;
            }
            _loopInstance = loop.gameObject;

            byte[] joinedSlots = ResolveJoinedSlots();
            byte localSlot = ResolveLocalSlot();

            _actors = new PlayerSessionActor[joinedSlots.Length];

            for (int i = 0; i < joinedSlots.Length; i++)
            {
                byte slot = joinedSlots[i];

                if (!UnityEngine.Object.Instantiate(_playerPrefab).TryGetComponent<PlayerSessionActor>(out var actor))
                {
                    Debugger.LogError("PlayerSessionActor not found");
                    return;
                }

                actor.SetSlot(new SlotId(slot));
                actor.CachedTransform.position = new Vector3(slot * 10, 0, 0);

                var p = state.Players[slot];
                p.IsJoined = true;
                p.LifeState = LifeState.Alive;
                p.Combat.Hp = p.Combat.MaxHp = 100f;
                p.Combat.BaseAttack = 10f;
                p.Combat.Attack = 10f;
                p.Combat.MoveSpeedMul = 1f;

                state.Players[slot] = p;

                actor.Bind(state, events, isLocal: slot == localSlot);

                if (slot == localSlot)
                {
                    var vcamGameObject = UnityEngine.Object.Instantiate(_vcamPrefab);
                    _vcamInstance = vcamGameObject;

                    if (!vcamGameObject.TryGetComponent<CinemachineCamera>(out var vcam))
                    {
                        Debugger.LogError("CinemachineCamera not found");
                        return;
                    }

                    vcam.Target.TrackingTarget = actor.CachedTransform;
                    actor.SetViewCamera(Camera.main);
                }

                _actors[i] = actor;
            }

            //HUD 오픈 및 바인딩
            var hud = await UIManager.OpenAsync<PlayerHudPanel>();
            hud.Bind(state, events, new SlotId(localSlot));
            
            loop.Bind(sim, state, _actors, localSlot);

            _phaseSub = events.PhaseChanged.Subscribe(phase =>
            {
                if (phase == RunPhase.PartyDown)
                    GameManager.GameOver();
            });

            sim.StartRun(12345u);
            await UniTask.NextFrame();
        }

        public override UniTask ExitAsync()
        {
            _phaseSub?.Dispose();
            _phaseSub = null;

            if (_actors != null)
            {
                for (int i = 0; i < _actors.Length; i++)
                {
                    if (_actors[i] != null)
                    {
                        UnityEngine.Object.Destroy(_actors[i].gameObject);
                    }
                }
                _actors = null;
            }

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

        private static byte[] ResolveJoinedSlots()
        {
            return new byte[] { 0 };
        }

        private static byte ResolveLocalSlot() => 0;
    }
}
