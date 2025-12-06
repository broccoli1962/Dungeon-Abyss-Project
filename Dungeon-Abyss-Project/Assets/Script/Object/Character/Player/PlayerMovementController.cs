using Backend.Util.Debug;
using Backend.Util.Input;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Backend.Object.Character.Player
{
    public class PlayerMovementController : MovementController
    {
        private PlayerInput_Actions _playerInput;
        private PlayerStatus _playerStatus;
        
        public bool IsAiming;
        private bool _isSprinting;

        private Vector3 _inputVector;
        private Vector3 _moveDirection;
        private Vector2 _mousePosition;

        [SerializeField] private State _playerState = State.Idle;

        private Camera _camera;

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
            if(_inputVector == Vector3.zero)
            {
                _playerState = State.Idle;
                return;
            }

            if (IsAiming)
            {
                _playerState = State.Walk;
            }
            else 
            {
                _playerState = _isSprinting ? State.Run : State.Walk;
            }
        }

        #region Movement Logic
        private void HandleRotation()
        {
            if (IsAiming)
            {
                RotateMouse();
            }else if (_moveDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(_moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _playerStatus.RotSpeed * Time.deltaTime);
            }
        }

        private void HandleMovement()
        {
            if(_playerState == State.Idle)
            {
                _moveDirection = Vector3.zero;
                return;
            }

            _playerState = State.Walk;

            //카메라 기준 회전
            Vector3 worldDirection;
            if (_camera != null)
            {
                var camForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
                var camRight = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
                worldDirection = (camForward * _inputVector.y + camRight * _inputVector.x).normalized;
            }
            else
            {
                worldDirection = new Vector3(_inputVector.x, 0, _inputVector.y).normalized;
            }

            _moveDirection = worldDirection;
        
            float currentSpeed = (_playerState == State.Run) ? _playerStatus.SprintSpeed : _playerStatus.WalkSpeed;
            Vector3 movement = _moveDirection * currentSpeed * Time.deltaTime;
            _rigidbody.MovePosition(_rigidbody.position + movement);
        }

        private void RotateMouse()
        {
            //마우스 위치로 회전
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
            _inputVector = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            _mousePosition = context.ReadValue<Vector2>();
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            _isSprinting = context.ReadValueAsButton();
        }

        private void OnAim(InputAction.CallbackContext context)
        {
            IsAiming = context.ReadValueAsButton();
        }
        #endregion
    }
}