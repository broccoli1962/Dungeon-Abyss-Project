using System;
using DdaIT.Object.Management;
using DdaIT.Util.Input;
using UnityEngine;


namespace DdaIT.Object.UI
{
    public class InventoryPopupPresenter : UIPresenter<InventoryPopup>
    {
        private UiPointerBlockerService _uiPointerService;
        private bool _closeRequested;

        public void Initialize(UiPointerBlockerService service)
        {
            if (_uiPointerService == service)
                return;

            _uiPointerService?.Unregister(View);
            _uiPointerService = service;

            if (View.IsOpen)
                _uiPointerService?.Register(View);
        }

        public override void OnOpen()
        {
            base.OnOpen();

            _closeRequested = false;
            Refresh();
        }

        public override void OnClose()
        {
            _uiPointerService?.Unregister(View);
            _uiPointerService = null;

            base.OnClose();
        }

        public void Refresh()
        {
            if (View.Slots == null)
                return;

            foreach (var slot in View.Slots)
            {
                if (slot != null)
                {
                    slot.RenderInventoryItem(null, 0);
                }
            }
        }

        public void NotifyChanged()
        {
            _uiPointerService?.NotifyChanged();
        }

        public bool TryClose()
        {
            if (!View.IsOpen || _closeRequested)
                return false;

            _closeRequested = true;
            return true;
        }

        public void RequestClose()
        {
            if (!TryClose())
                return;
            
            UIManager.Close(View);
        }
    }
}
