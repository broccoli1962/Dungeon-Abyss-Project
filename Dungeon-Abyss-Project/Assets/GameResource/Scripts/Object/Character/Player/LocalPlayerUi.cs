using System;
using DdaIT.Object.Management;
using DdaIT.Object.UI;
using DdaIT.Util.Input;
using R3;
using UnityEngine;

namespace DdaIT.Object.Character.Player
{
    public sealed class LocalPlayerUi : IDisposable
    {
        private readonly CompositeDisposable _subscriptions = new();
        private LocalPlayerDriver _driver;
        private UiPointerBlockerService _pointer;
        private InventoryPopup _popup;
        private IDisposable _closeSubscription;
        private bool _ownsCursor;
        private CursorLockMode _previousLockMode;
        private bool _previousCursorVisible;

        /// <summary>이 씬의 로컬 입력과 인벤토리 UI를 연결한다.</summary>
        public void Bind(LocalPlayerDriver driver, UiPointerBlockerService pointer)
        {
            Dispose();
            _driver = driver;
            _pointer = pointer;
            driver.BindUiPointerService(pointer);
            driver.InventoryRequested.Subscribe(_ => ToggleInventory()).AddTo(_subscriptions);
            driver.InputEnabledChanged.Subscribe(enabled =>
            {
                if (!enabled)
                    CloseInventory();
            }).AddTo(_subscriptions);
        }

        private void ToggleInventory()
        {
            if (_driver == null || !_driver.InputEnabled || GameStateUtil.IsQuitting)
                return;

            if (_popup != null)
            {
                CloseInventory();
                return;
            }

            _popup = UIManager.Open<InventoryPopup>();
            if (_popup == null)
                return;

            _closeSubscription = _popup.Closed.Subscribe(_ => ReleasePopup());
            _popup.Initialize(_pointer);
            _previousLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _ownsCursor = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>인벤토리에 닫기를 요청한다. 닫힘 이벤트에서 커서를 복구한다.</summary>
        public void CloseInventory()
        {
            if (_popup != null && !GameStateUtil.IsQuitting)
                _popup.RequestClose();
        }

        private void ReleasePopup()
        {
            _closeSubscription?.Dispose();
            _closeSubscription = null;
            _popup = null;
            RestoreCursor();
        }

        private void RestoreCursor()
        {
            if (!_ownsCursor)
                return;

            _ownsCursor = false;
            if (!GameStateUtil.IsQuitting)
            {
                Cursor.lockState = _previousLockMode;
                Cursor.visible = _previousCursorVisible;
            }
        }

        /// <summary>씬 종료나 재연결 시 팝업, 커서, 포인터와 구독을 정리한다.</summary>
        public void Dispose()
        {
            _subscriptions.Clear();
            CloseInventory();
            ReleasePopup();
            if (_driver != null)
                _driver.BindUiPointerService(null);
            _driver = null;
            _pointer = null;
        }
    }
}
