using DdaIT.Util;
using DdaIT.Util.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DdaIT.Object.Character.Player
{
    public struct LocalInputSnapShot
    {
        public Vector2 Move;
        public Vector2 LookScreen;
        public bool Sprint;
        public bool Aim;
        public bool AttackPressed;
        public bool InteractPressed;
    }

    public sealed class LocalPlayerInput : CachedMonobehaviour
    {
        private PlayerInput_Actions _actions;
        private bool _attackQueued;
        private bool _interactQueued;
        private Vector2 _move;
        private Vector2 _look;
        private bool _sprint;
        private bool _aim;

        public bool IsListening { get; private set; }

        private void Awake()
        {
            _actions = new PlayerInput_Actions();
            var player = _actions.Player;
            player.Move.performed += OnMove;
            player.Move.canceled += OnMove;
            player.Look.performed += OnLook;
            player.Look.canceled += OnLook;
            player.Sprint.performed += OnSprint;
            player.Sprint.canceled += OnSprint;
            player.Aim.performed += OnAim;
            player.Aim.canceled += OnAim;
            player.Attack.started += OnAttack;
            player.Interact.started += OnInteract;
        }

        public void SetListening(bool listen)
        {
            if (_actions == null || IsListening == listen) return;

            IsListening = listen;
            if (listen)
            {
                ResetState();
                _actions.Player.Enable();
            }
            else
            {
                _actions.Player.Disable();
                ResetState();
            }
        }

        public LocalInputSnapShot Consume()
        {
            if (!IsListening)
                return default;

            var snap = new LocalInputSnapShot
            {
                Move = _move,
                LookScreen = _look,
                Sprint = _sprint,
                Aim = _aim,
                AttackPressed = _attackQueued,
                InteractPressed = _interactQueued,
            };
            _attackQueued = false;
            _interactQueued = false;
            return snap;
        }

        private void ResetState(){
            _move = default;
            _look = default;
            _sprint = false;
            _aim = false;
            _attackQueued = false;
            _interactQueued = false;
        }

        private void OnDestroy()
        {
            if (_actions == null)
                return;

            SetListening(false);

            var player = _actions.Player;
            player.Move.performed -= OnMove;
            player.Move.canceled -= OnMove;
            player.Look.performed -= OnLook;
            player.Look.canceled -= OnLook;
            player.Sprint.performed -= OnSprint;
            player.Sprint.canceled -= OnSprint;
            player.Aim.performed -= OnAim;
            player.Aim.canceled -= OnAim;
            player.Attack.started -= OnAttack;
            player.Interact.started -= OnInteract;

            _actions.Dispose();
            _actions = null;
        }

        private void OnMove(InputAction.CallbackContext context) => _move = context.ReadValue<Vector2>();
        private void OnLook(InputAction.CallbackContext context) => _look = context.ReadValue<Vector2>();
        private void OnSprint(InputAction.CallbackContext context) => _sprint = context.ReadValueAsButton();
        private void OnAim(InputAction.CallbackContext context) => _aim = context.ReadValueAsButton();
        private void OnAttack(InputAction.CallbackContext context) => _attackQueued = true;
        private void OnInteract(InputAction.CallbackContext context) => _interactQueued = true;
    }
}
