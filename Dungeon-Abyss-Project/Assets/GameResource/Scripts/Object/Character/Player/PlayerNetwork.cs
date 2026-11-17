using DdaIT.Object.Character.Player;
using DdaIT.Object.Session;
using R3;
using Unity.Netcode;
using UnityEngine;

namespace DdaIT.Object.Character
{
    public class PlayerNetwork : NetworkBehaviour
    {
        [SerializeField] private PlayerSessionActor _actor;

        //네트워크 변수
        private readonly NetworkVariable<byte> _slot = new();
        private readonly NetworkVariable<PlayerPose> _pose = new();
        private readonly NetworkVariable<PlayerVital> _vitals = new(
            readPerm: NetworkVariableReadPermission.Owner
        );

        private readonly NetworkVariable<LifeState> _lifeState = new(
            LifeState.Absent,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        //수치 변경 이벤트
        private readonly Subject<PlayerVital> _vitalChanged = new();
        public PlayerVital Vitals => _vitals.Value;
        public Observable<PlayerVital> VitalChanged => _vitalChanged;
        public SlotId Slot => new SlotId(_slot.Value);

        private CharacterCommand _pending;
        private bool _hasCommand;
        private byte _assignedSlot;

        #region 네트워크 상태 관리
        public override void OnNetworkSpawn()
        {
            _hasCommand = false;
            if (IsServer)
            {
                _slot.Value = _assignedSlot;
                _pose.Value = new PlayerPose
                {
                    Position = transform.position,
                    Yaw = transform.eulerAngles.y,
                };
                _lifeState.Value = LifeState.Absent;
            }

            _slot.OnValueChanged += OnSlotChanged;
            _vitals.OnValueChanged += OnVitalsChanged;
            _lifeState.OnValueChanged += OnLifeStateChanged;

            ApplySlot(_slot.Value);
            _actor?.ApplyLifeState(_lifeState.Value);
            RefreshInputAuthority();
        }

        public override void OnNetworkDespawn()
        {
            _slot.OnValueChanged -= OnSlotChanged;
            _vitals.OnValueChanged -= OnVitalsChanged;
            _lifeState.OnValueChanged -= OnLifeStateChanged;
            _hasCommand = false;

            if (_actor != null)
            {
                _actor.SetInputAuthority(false);
                _actor.ReleaseLocalUi();
                
                _actor.UnBind();
                _actor.ApplyLifeState(LifeState.Absent);
            }
        }

        public override void OnGainedOwnership() => RefreshInputAuthority();
        public override void OnLostOwnership() => RefreshInputAuthority();

        private void RefreshInputAuthority()
        {
            _actor?.SetInputAuthority(IsSpawned && IsOwner);
        }

        #endregion

        #region 틱
        private void FixedUpdate()
        {
            if (!IsSpawned || _actor == null) return;

            if (IsOwner && _actor.CanAct)
            {
                SubmitCommandRpc(_actor.TickLocal(Time.fixedDeltaTime));
                return;
            }

            _actor.ApplyRemote(_pose.Value, Time.fixedDeltaTime);
        }
        #endregion

        #region 명령 처리
        public bool TryConsumeCommand(out CharacterCommand command)
        {
            command = _pending;
            bool has = _hasCommand;
            _hasCommand = false;
            return has;
        }

        public void PrepareSlot(byte slot)
        {
            _assignedSlot = slot;
        }

        private void OnLifeStateChanged(LifeState oldValue, LifeState newValue)
        {
            if (newValue != LifeState.Alive)
            {
                _hasCommand = false;
            }

            _actor?.ApplyLifeState(newValue);
        }

        private void OnVitalsChanged(PlayerVital oldValue, PlayerVital newValue)
        {
            _vitalChanged.OnNext(newValue);
        }

        private void OnSlotChanged(byte oldValue, byte newValue)
        {
            ApplySlot(newValue);
        }

        private void ApplySlot(byte slot)
        {
            if (_actor == null) return;

            _actor.SetSlot(new SlotId(slot));
        }

        #endregion

        #region 서버 명령 처리
        public void ServerSetPose(in PlayerPose pose)
        {
            if (!IsServer || !IsSpawned) return;

            _pose.Value = pose;
        }

        public void ServerPlayAttack()
        {
            if (!IsServer || !IsSpawned || _lifeState.Value != LifeState.Alive) return;

            AttackApproveRpc();
        }

        public void ServerSetVitals(in PlayerVital vitals)
        {
            if (!IsServer || !IsSpawned) return;

            _vitals.Value = vitals;
        }

        public void ServerSetLifeState(LifeState state)
        {
            if (!IsServer || !IsSpawned) return;

            _lifeState.Value = state;
        }

        public void ServerNotifyDamage(in DamageApplied damage)
        {
            if (!IsServer || !IsSpawned) return;

            if (damage.TargetKind != (int)HitTargetKind.Player || damage.TargetId != _slot.Value)
            {
                return;
            }

            ServerSetLifeState(damage.LifeStateAfter);

            if(damage.Amount > 0f && damage.LifeStateAfter == LifeState.Alive){
                PlayHitRpc();
            }
        }
        #endregion

        #region RPC
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitCommandRpc(CharacterCommand command)
        {
            if(_lifeState.Value != LifeState.Alive) return;

            command.SlotId = Slot;
            _pending = command;
            _hasCommand = true;
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void AttackApproveRpc()
        {
            _actor?.PlayRemoteAttack(canDetectHits: IsServer);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void PlayHitRpc()
        {
            _actor?.PlayRemoteHit();
        }
        #endregion
    }
}
