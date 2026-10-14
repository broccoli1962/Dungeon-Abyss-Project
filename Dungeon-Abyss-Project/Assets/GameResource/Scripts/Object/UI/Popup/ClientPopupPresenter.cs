using Cysharp.Threading.Tasks;
using DdaIT.Object.Management;
using DdaIT.Object.UI;
using R3;

namespace DdaIT
{
    public class ClientPopupPresenter : UIPresenter<ClientPopup>
    {
        private readonly CompositeDisposable _disposables = new();

        public override void OnOpen()
        {
            base.OnOpen();

            View.JoinButton.OnClickAsObservable()
                .Subscribe(_ => OnJoinClicked().Forget())
                .AddTo(_disposables);

            View.CloseButton.OnClickAsObservable()
                .Subscribe(_ => UIManager.Close(View))
                .AddTo(_disposables);
        }

        public override void OnClose()
        {
            _disposables.Clear();
            base.OnClose();
        }

        private async UniTask OnJoinClicked()
        {
            string code = View.LobbyCodeInput != null ? View.LobbyCodeInput.text : string.Empty;
            // TODO: Lobby 코드로 참가 + Relay + NGO Client 시작
            _ = code;

            await AppFlow.LoadSceneAsync(SceneId.GameScene);
        }
    }
}
