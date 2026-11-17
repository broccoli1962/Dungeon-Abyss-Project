using System.Collections.Generic;
using DdaIT.Util.Debug;
using DdaIT.Util.Input;
using DdaIT.Util.Interface;
using R3;
using UnityEngine;

namespace DdaIT.Object.UI
{
    public class InventoryPopup : UIPopup<InventoryPopupPresenter>, IUiPointerBlocker
    {
        [SerializeField] private InventorySlotView[] _slots;
        [SerializeField] private RectTransform _inventoryArea;

        private readonly Subject<Unit> _closed = new();

        private Canvas _canvas;
        private bool _isOpen;
        private bool _isDragging;

        public IReadOnlyList<InventorySlotView> Slots => _slots;
        public Observable<Unit> Closed => _closed;
        public bool IsOpen => _isOpen;

        protected override void Awake()
        {
            base.Awake();

            if (_slots == null)
                return;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].Init(i);
                }
            }
        }

        public void Initialize(UiPointerBlockerService service)
        {
            Presenter.Initialize(service);
        }

        public bool BlockPointer(Vector2 screenPos)
        {
            if (!_isOpen || !isActiveAndEnabled)
                return false;

            if (_isDragging)
                return true;

            if (_inventoryArea == null || _canvas == null)
                return true;

            Camera camera = null;

            if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camera = _canvas.worldCamera;

                if (camera == null)
                    return true;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(_inventoryArea, screenPos, camera);
        }

        public void SetDragging(bool dragging)
        {
            if (_isDragging == dragging)
                return;

            _isDragging = dragging;
            Presenter.NotifyChanged();
        }

        public void RequestClose()
        {
            Presenter.RequestClose();
        }

        public override bool OnBackPressed()
        {
            return Presenter.TryClose();
        }

        protected override sealed void OnOpen()
        {
            _isOpen = true;
            _isDragging = false;

            _canvas = _inventoryArea?.GetComponentInParent<Canvas>();

            if (_canvas != null)
                _canvas = _canvas.rootCanvas;

            if (_inventoryArea == null || _canvas == null)
            {
                Debugger.LogError("InventoryPopup: InventoryArea or Canvas is not set");
            }

            base.OnOpen();
        }

        protected override sealed void OnClose()
        {
            CloseFunc();
        }

        private void CloseFunc()
        {
            if(!_isOpen)
                return;

            _isOpen = false;
            _isDragging = false;

            base.OnClose();

            _closed.OnNext(Unit.Default);
        }

        private void OnDisable()
        {
            CloseFunc();
        }

        private void OnDestroy()
        {
            CloseFunc();
            _closed.Dispose();
        }
    }
}