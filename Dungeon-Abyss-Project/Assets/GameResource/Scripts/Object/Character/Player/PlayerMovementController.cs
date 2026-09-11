using DdaIT.Util.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DdaIT.Object.Character.Player
{
    public class PlayerMovementController : MovementController
    {
        [SerializeField] private PlayerState _playerState = PlayerState.Idle;

        [Header("Move Setting")]
        [SerializeField] private float _walkSpeed = 2f;
        [SerializeField] private float _sprintSpeed = 4f;
        [SerializeField] private float _rotSpeed = 4f;

        private Camera _camera;
        private Vector2 _mousePosition;

        public Vector3 MoveDirection { get; private set; }
        public Vector3 InputVector { get; private set; }
        public bool IsAiming { get; private set; }
        public bool IsSprinting { get; private set; }
        public float SpeedRatio { get; private set; }

        public void SetCamera(Camera camera) => _camera = camera;

        public void ApplyLocal(in LocalInputSnapShot input, float deltaTime)
        {
            InputVector = new Vector3(input.Move.x, 0f, input.Move.y);
            IsAiming = input.Aim;
            IsSprinting = input.Sprint;

            UpdateState();
            HandleRotation(input.LookScreen, deltaTime);
            HandleMovement(deltaTime);
        }

        public void ApplyReplicated(Vector3 position, float yaw, bool aiming, float speedRatio)
        {
            IsAiming = aiming;
            SpeedRatio = speedRatio;
            IsSprinting = speedRatio >= 0.99f;
            InputVector = speedRatio > 0.001f ? SimulationRotation * Vector3.forward : Vector3.zero;
            MoveDirection = speedRatio > 0.001f ? SimulationRotation * Vector3.forward : Vector3.zero;
            _rigidbody.MovePosition(position);
            _rigidbody.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        }

        private void UpdateState()
        {
            if (InputVector == Vector3.zero)
            {
                _playerState = PlayerState.Idle;
                SpeedRatio = 0f;
                return;
            }

            if (IsAiming)
            {
                _playerState = PlayerState.Walk;
                SpeedRatio = 0.5f;
            }
            else
            {
                _playerState = IsSprinting ? PlayerState.Run : PlayerState.Walk;
                SpeedRatio = IsSprinting ? 1f : 0.5f;
            }
        }

        #region Movement Logic
        private void HandleRotation(Vector2 lookScreen, float deltaTime)
        {
            if (IsAiming)
            {
                RotateMouse(lookScreen, deltaTime);
            }
            else if (MoveDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(MoveDirection);
                _rigidbody.MoveRotation(Quaternion.Slerp(SimulationRotation, targetRot, _rotSpeed * deltaTime));
            }
        }

        private void HandleMovement(float deltaTime)
        {
            if (_playerState == PlayerState.Idle)
            {
                MoveDirection = Vector3.zero;
                return;
            }

            //캐릭터 방향 계산
            Vector3 worldDirection;
            if (_camera != null)
            {
                var camForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
                var camRight = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
                worldDirection = (camForward * InputVector.z + camRight * InputVector.x).normalized;
            }
            else
            {
                worldDirection = new Vector3(InputVector.x, 0, InputVector.z).normalized;
            }

            MoveDirection = worldDirection;
            float speed = _playerState == PlayerState.Run ? _sprintSpeed : _walkSpeed;
            _rigidbody.MovePosition(_rigidbody.position + MoveDirection * speed * deltaTime);
        }

        private void RotateMouse(Vector2 lookScreen, float deltaTime)
        {
            //캐릭터 방향 계산
            if (_camera == null) return;

            Vector3 origin = SimulationPosition;

            Ray ray = _camera.ScreenPointToRay(lookScreen);

            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, origin.y, 0));
            if (!groundPlane.Raycast(ray, out float enter))
                return;

            Vector3 hitPoint = ray.GetPoint(enter);
            
            Vector3 direction = GetDirection(hitPoint);
            if(direction.magnitude < 0.0001f)
                return;

            var targetRot = Quaternion.LookRotation(direction);
            _rigidbody.MoveRotation(Quaternion.Slerp(SimulationRotation, targetRot, _rotSpeed * deltaTime));
        }

        #endregion

        #region Input Callbacks
        private void OnMove(InputAction.CallbackContext context)
        {
            InputVector = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            _mousePosition = context.ReadValue<Vector2>();
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            IsSprinting = context.ReadValueAsButton();
        }

        private void OnAim(InputAction.CallbackContext context)
        {
            IsAiming = context.ReadValueAsButton();
        }
        #endregion
    }
}