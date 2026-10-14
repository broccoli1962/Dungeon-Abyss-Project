using DdaIT.Object.UI;
using TMPro;
using UnityEngine;

namespace DdaIT
{
    public class ClientPopup : UIPopup<ClientPopupPresenter>
    {
        [SerializeField] private TMP_InputField _lobbyCodeInput;
        [SerializeField] private CommonButton _joinButton;
        [SerializeField] private CommonButton _closeButton;

        public TMP_InputField LobbyCodeInput => _lobbyCodeInput;
        public CommonButton JoinButton => _joinButton;
        public CommonButton CloseButton => _closeButton;
    }
}
