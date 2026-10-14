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

        //수치 변경 이벤트
        private readonly Subject<PlayerVital> _vitalChanged = new();
        public PlayerVital Vitals => _vitals.Value;
        public Observable<PlayerVital> VitalChanged => _vitalChanged;

        private CharacterCommand _pending;
        private bool _hasCommand;
        private byte _assignedSlot;

        public SlotId Slot => new SlotId(_slot.Value);

        #region 네트워크 상태 관리
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _slot.Value = _assignedSlot;
                _pose.Value = new PlayerPose
                {
                    Position = transform.position,
                    Yaw = transform.eulerAngles.y,
                };
            }

            _slot.OnValueChanged += OnSlotChanged;
            _vitals.OnValueChanged += OnVitalsChanged;
            ApplySlot(_slot.Value);
        }

        public override void OnNetworkDespawn()
        {
            _slot.OnValueChanged -= OnSlotChanged;
            _vitals.OnValueChanged -= OnVitalsChanged;
        }
        #endregion

        #region 틱
        private void FixedUpdate()
        {
            if (!IsSpawned || _actor == null) return;

            if (IsOwner)
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
            if (!IsServer) return;

            _pose.Value = pose;
        }

        public void ServerPlayAttack()
        {
            if (!IsServer) return;

            AttackRpc();
        }

        public void ServerSetVitals(in PlayerVital vitals)
        {
            if (!IsServer) return;

            _vitals.Value = vitals;
        }
        #endregion

        #region RPC
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitCommandRpc(CharacterCommand command)
        {
            command.SlotId = Slot;
            _pending = command;
            _hasCommand = true;
        }

        [Rpc(SendTo.NotOwner)]
        private void AttackRpc()
        {
            _actor.PlayRemoteAttack();
        }
        #endregion
    }
}
