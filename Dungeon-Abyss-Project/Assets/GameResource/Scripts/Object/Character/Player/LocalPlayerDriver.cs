using DdaIT.Object.Session;
using DdaIT.Util.Input;
using R3;
using UnityEngine;

namespace DdaIT.Object.Character.Player
{
    public sealed class LocalPlayerDriver : MonoBehaviour
    {
        [SerializeField] private LocalPlayerInput _input;
        [SerializeField] private PlayerPawn _pawn;

        private bool _hasInputAuthority;
        private bool _gameplayInputEnabled = true;
        private readonly Subject<Unit> _inventoryRequested = new();

        public bool InputEnabled => _input.IsListening;
        public Observable<bool> InputEnabledChanged => _input.ListeningChanged;
        public Observable<Unit> InventoryRequested => _inventoryRequested;

        /// <summary>소유자의 입력을 한 번 소비하고 서버에 전송할 명령을 구성한다.</summary>
        public CharacterCommand Tick(float deltaTime)
        {
            LocalInputSnapShot input = default;
            if (InputEnabled && _pawn.CanAct)
            {
                input = _input.Consume();
                if (input.InventoryPressed)
                {
                    _inventoryRequested.OnNext(Unit.Default);
                    input.AttackPressed = false;
                    input.Aim = false;
                }
                _pawn.ApplyIntent(input, deltaTime);
            }
            else
            {
                _pawn.StopMovement(deltaTime);
            }

            var pose = _pawn.ReadPose();
            return new CharacterCommand
            {
                SlotId = _pawn.Slot,
                Position = pose.Position,
                Yaw = pose.Yaw,
                MoveDirection = pose.MoveDirection,
                SpeedRatio = pose.SpeedRatio,
                Sprint = input.Sprint,
                Aim = pose.IsAiming,
                AttackPressed = input.AttackPressed && pose.IsAiming,
                InteractPressed = input.InteractPressed,
            };
        }

        /// <summary>네트워크 소유자에게만 로컬 입력을 허용한다.</summary>
        public void SetInputAuthority(bool hasAuthority)
        {
            _hasInputAuthority = hasAuthority;
            enabled = hasAuthority;
            RefreshInputState();
        }

        /// <summary>컷씬 등에서 게임플레이 입력을 차단하거나 재개한다.</summary>
        public void SetGameplayInputEnabled(bool enabled)
        {
            _gameplayInputEnabled = enabled;
            RefreshInputState();
        }

        /// <summary>현재 권한과 생명 상태를 입력 리스너에 반영한다.</summary>
        public void RefreshInputState()
        {
            _input.SetListening(isActiveAndEnabled && _hasInputAuthority && _gameplayInputEnabled && _pawn.CanAct);
        }

        /// <summary>씬의 UI 포인터 차단 서비스를 입력에 연결한다.</summary>
        public void BindUiPointerService(UiPointerBlockerService service) => _input.BindUiPointerService(service);

        private void OnEnable() => RefreshInputState();

        private void OnDisable()
        {
            if (_input != null)
                _input.SetListening(false);
        }

        private void OnDestroy() => _inventoryRequested.Dispose();
    }
}
