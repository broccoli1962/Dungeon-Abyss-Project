using Backend.Util.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Backend.Object.Character.Player
{
    public class PlayerMovementController : MovementController
    {
        [SerializeField] private PlayerState _playerState = PlayerState.Idle;
        private PlayerInput_Actions _playerInput;
        private PlayerStatus _playerStatus;
        private Camera _camera;
        private Vector2 _mousePosition;

        public Vector3 MoveDirection { get; private set; }
        public Vector3 InputVector { get; private set; }
        public bool IsAiming { get; private set; }
        public bool IsSprinting { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            
            _playerInput = new PlayerInput_Actions();
            _playerStatus = GetComponent<PlayerStatus>();

            if (Camera.main != null)
            {
                _camera = Camera.main;
            }
        }

        private void OnEnable()
        {
            _playerInput.Player.Enable();
            
            _playerInput.Player.Move.performed += OnMove;
            _playerInput.Player.Move.canceled += OnMove;

            _playerInput.Player.Sprint.performed += OnSprint;
            _playerInput.Player.Sprint.canceled += OnSprint;

            _playerInput.Player.Look.performed += OnLook;
            _playerInput.Player.Look.canceled += OnLook;

            _playerInput.Player.Aim.performed += OnAim;
            _playerInput.Player.Aim.canceled += OnAim;
        }

        private void OnDisable()
        {
            _playerInput.Player.Move.performed -= OnMove;
            _playerInput.Player.Move.canceled -= OnMove;

            _playerInput.Player.Sprint.performed -= OnSprint;
            _playerInput.Player.Sprint.canceled -= OnSprint;

            _playerInput.Player.Look.performed -= OnLook;
            _playerInput.Player.Look.canceled -= OnLook;

            _playerInput.Player.Aim.performed -= OnAim;
            _playerInput.Player.Aim.canceled -= OnAim;

            _playerInput.Player.Disable();
        }

        private void FixedUpdate()
        {
            UpdateState();
            HandleRotation();
            HandleMovement();
        }

        private void UpdateState()
        {
            if(InputVector == Vector3.zero)
            {
                _playerState = PlayerState.Idle;
                return;
            }

            if (IsAiming)
            {
                _playerState = PlayerState.Walk;
            }
            else 
            {
                _playerState = IsSprinting ? PlayerState.Run : PlayerState.Walk;
            }
        }

        #region Movement Logic
        private void HandleRotation()
        {
            if (IsAiming)
            {
                RotateMouse();
            }else if (MoveDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(MoveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _playerStatus.RotSpeed * Time.deltaTime);
            }
        }

        private void HandleMovement()
        {
            if(_playerState == PlayerState.Idle)
            {
                MoveDirection = Vector3.zero;
                return;
            }

            //ī�޶� ���� ȸ��
            Vector3 worldDirection;
            if (_camera != null)
            {
                var camForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
                var camRight = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
                worldDirection = (camForward * InputVector.y + camRight * InputVector.x).normalized;
            }
            else
            {
                worldDirection = new Vector3(InputVector.x, 0, InputVector.y).normalized;
            }

            MoveDirection = worldDirection;
        
            float currentSpeed = (_playerState == PlayerState.Run) ? _playerStatus.SprintSpeed : _playerStatus.WalkSpeed;
            Vector3 movement = MoveDirection * currentSpeed * Time.deltaTime;
            _rigidbody.MovePosition(_rigidbody.position + movement);
        }

        private void RotateMouse()
        {
            //���콺 ��ġ�� ȸ��
            Ray ray = _camera.ScreenPointToRay(_mousePosition);

            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

            if(groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                Vector3 lookTarget = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
                Vector3 direction = (lookTarget - transform.position).normalized;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _playerStatus.RotSpeed * Time.deltaTime);
                }
            }
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