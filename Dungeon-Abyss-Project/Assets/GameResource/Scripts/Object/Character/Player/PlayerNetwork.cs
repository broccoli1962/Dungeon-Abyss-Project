using System;
using System.Collections.Generic;
using DdaIT.Object.Character.Player;
using DdaIT.Object.Session;
using R3;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace DdaIT.Object.Character
{
    public class PlayerNetwork : NetworkBehaviour
    {
        [FormerlySerializedAs("_actor")]
        [SerializeField] private PlayerPawn _pawn;
        [SerializeField] private LocalPlayerDriver _driver;

        private readonly NetworkVariable<byte> _slot = new();
        private readonly NetworkVariable<PlayerPose> _pose = new();
        private readonly NetworkVariable<PlayerVital> _vitals = new(
            readPerm: NetworkVariableReadPermission.Owner
        );
        private readonly NetworkVariable<LifeState> _lifeState = new(
            LifeState.Absent, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
        );

        private readonly Subject<PlayerVital> _vitalChanged = new();
        private IDisposable _damageSub;
        private CharacterCommand _pending;
        private bool _hasCommand;
        private byte _assignedSlot;

        public PlayerPawn Pawn => _pawn;
        public LocalPlayerDriver LocalDriver => _driver;
        public PlayerVital Vitals => _vitals.Value;
        public Observable<PlayerVital> VitalChanged => _vitalChanged;
        public SlotId Slot => new SlotId(_slot.Value);

        /// <summary>초기 복제 상태와 소유자의 입력 권한을 적용한다.</summary>
        public override void OnNetworkSpawn()
        {
            _hasCommand = false;
            if (IsServer)
            {
                _slot.Value = _assignedSlot;
                _pose.Value = new PlayerPose { Position = transform.position, Yaw = transform.eulerAngles.y };
                _lifeState.Value = LifeState.Absent;
            }

            _slot.OnValueChanged += OnSlotChanged;
            _vitals.OnValueChanged += OnVitalsChanged;
            _lifeState.OnValueChanged += OnLifeStateChanged;
            _pawn.SetSlot(Slot);
            _pawn.ApplyLifeState(_lifeState.Value);
            RefreshInputAuthority();
        }

        /// <summary>입력, 타격 수집과 세션 구독을 해제한다.</summary>
        public override void OnNetworkDespawn()
        {
            _slot.OnValueChanged -= OnSlotChanged;
            _vitals.OnValueChanged -= OnVitalsChanged;
            _lifeState.OnValueChanged -= OnLifeStateChanged;
            _hasCommand = false;
            _driver.SetInputAuthority(false);
            UnbindServer();
            _pawn.ApplyLifeState(LifeState.Absent);
        }

        /// <summary>소유권 획득을 입력 권한에 반영한다.</summary>
        public override void OnGainedOwnership() => RefreshInputAuthority();

        /// <summary>소유권 상실을 입력 권한에 반영한다.</summary>
        public override void OnLostOwnership() => RefreshInputAuthority();

        private void RefreshInputAuthority() => _driver.SetInputAuthority(IsSpawned && IsOwner);

        private void FixedUpdate()
        {
            if (!IsSpawned)
                return;

            if (IsOwner && _pawn.CanAct)
                SubmitCommandRpc(_driver.Tick(Time.fixedDeltaTime));
            else
                _pawn.ApplyPose(_pose.Value, Time.fixedDeltaTime);
        }

        /// <summary>서버에서 아직 처리하지 않은 소유자 명령을 꺼낸다.</summary>
        public bool TryConsumeCommand(out CharacterCommand command)
        {
            command = _pending;
            bool hasCommand = _hasCommand;
            _hasCommand = false;
            return hasCommand;
        }

        /// <summary>스폰 시 사용할 슬롯을 지정한다.</summary>
        public void PrepareSlot(byte slot) => _assignedSlot = slot;

        private void OnSlotChanged(byte oldValue, byte newValue) => _pawn.SetSlot(new SlotId(newValue));

        private void OnVitalsChanged(PlayerVital oldValue, PlayerVital newValue) => _vitalChanged.OnNext(newValue);

        private void OnLifeStateChanged(LifeState oldValue, LifeState newValue)
        {
            if (newValue != LifeState.Alive)
                _hasCommand = false;

            _pawn.ApplyLifeState(newValue);
            _driver.RefreshInputState();
        }

        /// <summary>서버의 피격 이벤트와 무기 타격 수집을 연결한다.</summary>
        public void BindServer(RunEvents events)
        {
            if (!IsSpawned || !IsServer)
                return;

            UnbindServer();
            _damageSub = events.DamageApplied.Subscribe(OnDamage);
            _pawn.SetHitAuthority(true);
        }

        /// <summary>서버 구독을 해제하고 미처리 타격을 버린다.</summary>
        public void UnbindServer()
        {
            _damageSub?.Dispose();
            _damageSub = null;
            _pawn.SetHitAuthority(false);
        }

        /// <summary>호스트 틱에서 처리할 타격을 수집한다.</summary>
        public void DrainHits(List<HitTarget> hits)
        {
            if (IsSpawned && IsServer)
                _pawn.DrainHits(hits);
        }

        /// <summary>서버 슬롯의 생명 상태, 자세와 자원을 복제한다.</summary>
        public void ServerPublish(in PlayerSlotState state)
        {
            if (!IsServer || !IsSpawned)
                return;

            _lifeState.Value = state.LifeState;
            _pose.Value = new PlayerPose
            {
                Position = state.Position,
                Yaw = state.Yaw,
                MoveDirection = state.AnimationParams.MoveDirection,
                SpeedRatio = state.AnimationParams.SpeedRatio,
                IsAiming = state.Aiming,
            };
            _vitals.Value = new PlayerVital
            {
                Health = state.Combat.Hp,
                MaxHealth = state.Combat.MaxHp,
                Stamina = state.Stamina,
                MaxStamina = state.MaxStamina,
                Hunger = state.Hunger,
                MaxHunger = state.MaxHunger,
            };
        }

        /// <summary>생존 중 승인된 공격을 모든 피어에 알린다.</summary>
        public void ServerPlayAttack()
        {
            if (IsServer && IsSpawned && _lifeState.Value == LifeState.Alive)
                AttackApproveRpc();
        }

        private void OnDamage(DamageApplied damage)
        {
            if (!IsServer || !IsSpawned || damage.TargetKind != (int)HitTargetKind.Player || damage.TargetId != _slot.Value)
                return;

            _lifeState.Value = damage.LifeStateAfter;
            if (damage.Amount > 0f && damage.LifeStateAfter == LifeState.Alive)
                PlayHitRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitCommandRpc(CharacterCommand command)
        {
            if (_lifeState.Value != LifeState.Alive)
                return;

            command.SlotId = Slot;
            _pending = command;
            _hasCommand = true;
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void AttackApproveRpc() => _pawn.PlayAttack();

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void PlayHitRpc() => _pawn.PlayHit();
    }
}
