using System;
using System.Threading;
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
        }

        public void SetListening(bool listen)
        {
            if (IsListening == listen) return;

            IsListening = listen;
            if (listen)
            {
                _actions.Player.Enable();
                _actions.Player.Move.performed += OnMove;
                _actions.Player.Move.canceled += OnMove;
                _actions.Player.Look.performed += OnLook;
                _actions.Player.Look.canceled += OnLook;
                _actions.Player.Sprint.performed += OnSprint;
                _actions.Player.Sprint.canceled += OnSprint;
                _actions.Player.Aim.performed += OnAim;
                _actions.Player.Aim.canceled += OnAim;
                _actions.Player.Attack.started += OnAttack;
                _actions.Player.Interact.started += OnInteract;
            }
            else
            {
                _actions.Player.Move.performed -= OnMove;
                _actions.Player.Move.canceled -= OnMove;
                _actions.Player.Look.performed -= OnLook;
                _actions.Player.Look.canceled -= OnLook;
                _actions.Player.Sprint.performed -= OnSprint;
                _actions.Player.Sprint.canceled -= OnSprint;
                _actions.Player.Aim.performed -= OnAim;
                _actions.Player.Aim.canceled -= OnAim;
                _actions.Player.Attack.started -= OnAttack;
            }
        }

        public LocalInputSnapShot Consume()
        {
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

        private void OnDisable() => SetListening(false);

        private void OnDestroy(){
            SetListening(false);
            _actions?.Dispose();
        }

        private void OnMove(InputAction.CallbackContext context) => _move = context.ReadValue<Vector2>();
        private void OnLook(InputAction.CallbackContext context) => _look = context.ReadValue<Vector2>();
        private void OnSprint(InputAction.CallbackContext context) => _sprint = context.ReadValueAsButton();
        private void OnAim(InputAction.CallbackContext context) => _aim = context.ReadValueAsButton();
        private void OnAttack(InputAction.CallbackContext context) => _attackQueued = true;
        private void OnInteract(InputAction.CallbackContext context) => _interactQueued = true;
    }
}