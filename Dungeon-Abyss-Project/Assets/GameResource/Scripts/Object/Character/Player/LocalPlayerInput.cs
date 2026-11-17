using System;
using DdaIT.Util;
using DdaIT.Util.Input;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        public bool InventoryPressed;
    }

    public sealed class LocalPlayerInput : CachedMonobehaviour
    {
        private PlayerInput_Actions _actions;

        private UiPointerBlockerService _uiPointerService;
        private IDisposable _uiPointerSub;

        private readonly Subject<bool> _listeningChanged = new();

        private bool _attackQueued;
        private bool _interactQueued;
        private bool _inventoryQueued;

        private bool _leftOwnedByUi;
        private bool _rightOwnedByUi;
        private bool _aimStartedInWorld;

        private int _pointerSuppressedFrame = -1;

        public bool IsListening { get; private set; }
        public Observable<bool> ListeningChanged => _listeningChanged;

        private bool CanProcessInput =>
            IsListening &&
            isActiveAndEnabled &&
            Application.isFocused;

        private void Awake()
        {
            _actions = new PlayerInput_Actions();

            var player = _actions.Player;

            player.Attack.started += OnAttack;
            player.Attack.canceled += OnAttackCanceled;

            player.Aim.performed += OnAim;
            player.Aim.canceled += OnAimCanceled;

            player.Interact.started += OnInteract;
            _actions.UI.Inventory.started += OnInventory;
        }

        public void SetListening(bool listen)
        {
            if (_actions == null || IsListening == listen) return;

            IsListening = listen;

            if (listen)
            {
                _actions.Player.Enable();
                _actions.UI.Inventory.Enable();
            }
            else
            {
                _actions.Player.Disable();
                _actions.UI.Inventory.Disable();
            }

            ResetState();
            _listeningChanged.OnNext(listen);
        }

        public void BindUiPointerService(UiPointerBlockerService service)
        {
            _uiPointerSub?.Dispose();
            _uiPointerSub = null;

            _uiPointerService = service;

            if (service != null)
            {
                _uiPointerSub = service.Changed.Subscribe(_ => ResetPointerState());
            }

            ResetPointerState();
        }

        public LocalInputSnapShot Consume()
        {
            if (!CanProcessInput)
            {
                ResetState();
                return default;
            }

            var player = _actions.Player;
            bool blockPointer = IsWorldPointerBlocked();

            var snap = new LocalInputSnapShot
            {
                Move = player.Move.ReadValue<Vector2>(),
                LookScreen = player.Look.ReadValue<Vector2>(),
                Sprint = player.Sprint.IsPressed(),

                Aim = !blockPointer &&
                    _aimStartedInWorld &&
                    player.Aim.IsPressed(),

                AttackPressed = !blockPointer && _attackQueued,
                InteractPressed = _interactQueued,
                InventoryPressed = _inventoryQueued,
            };

            _attackQueued = false;
            _interactQueued = false;
            _inventoryQueued = false;

            return snap;
        }

        private bool IsWorldPointerBlocked()
        {
            if (_pointerSuppressedFrame == Time.frameCount ||
                    _leftOwnedByUi ||
                    _rightOwnedByUi)
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse == null || _uiPointerService == null)
                return false;

            return _uiPointerService.IsPointerBlocked(mouse.position.ReadValue());
        }

        private void OnAttack(InputAction.CallbackContext context)
        {
            bool blocked = !CanProcessInput || IsWorldPointerBlocked();

            _leftOwnedByUi = blocked;

            if (!blocked)
                _attackQueued = true;
        }

        private void OnAttackCanceled(InputAction.CallbackContext context)
        {
            _leftOwnedByUi = false;
        }

        private void OnAim(InputAction.CallbackContext context)
        {
            bool blocked = !CanProcessInput || IsWorldPointerBlocked();

            _rightOwnedByUi = blocked;
            _aimStartedInWorld = !blocked;
        }

        private void OnAimCanceled(InputAction.CallbackContext context)
        {
            _rightOwnedByUi = false;
            _aimStartedInWorld = false;
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            if (CanProcessInput)
            {
                _interactQueued = true;
            }
        }

        private void OnInventory(InputAction.CallbackContext context)
        {
            if (CanProcessInput)
            {
                _inventoryQueued = true;
            }
        }

        private void ResetPointerState()
        {
            _attackQueued = false;
            _aimStartedInWorld = false;
            _pointerSuppressedFrame = Time.frameCount;

            _leftOwnedByUi = _actions != null && _actions.Player.Attack.IsPressed();
            _rightOwnedByUi = _actions != null && _actions.Player.Aim.IsPressed();
        }


        private void ResetState()
        {
            _interactQueued = false;
            _inventoryQueued = false;
            ResetPointerState();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                ResetState();
            }
        }

        private void OnDisable()
        {
            SetListening(false);
        }

        private void OnDestroy()
        {
            _uiPointerSub?.Dispose();
            _uiPointerSub = null;
            _uiPointerService = null;

            if (_actions != null)
            {
                SetListening(false);

                var player = _actions.Player;
                
                player.Attack.started -= OnAttack;
                player.Attack.canceled -= OnAttackCanceled;
                
                player.Aim.performed -= OnAim;
                player.Aim.canceled -= OnAimCanceled;

                player.Interact.started -= OnInteract;
                _actions.UI.Inventory.started -= OnInventory;

                _actions.Dispose();
                _actions = null;
            }
            
            _listeningChanged.Dispose();
        }
    }
}
