using DdaIT.Object.UI;
using UnityEngine;

namespace DdaIT
{
    public class HostPopup : UIPopup<HostPopupPresenter>
    {
        [SerializeField] private CommonButton _hostButton;
        [SerializeField] private CommonButton _closeButton;

        public CommonButton HostButton => _hostButton;
        public CommonButton CloseButton => _closeButton;
    }
}
