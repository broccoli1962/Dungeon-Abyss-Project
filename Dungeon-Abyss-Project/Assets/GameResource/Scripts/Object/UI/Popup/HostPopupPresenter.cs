using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DdaIT.Object.Management;
using DdaIT.Object.UI;
using R3;
using Unity.Netcode;

namespace DdaIT.Object.UI
{
    public class HostPopupPresenter : UIPresenter<HostPopup>
    {
        private readonly CompositeDisposable _disposables = new();

        public override void OnOpen()
        {
            base.OnOpen();

            View.HostButton.OnClickAsObservable()
                .Subscribe(_ => OnHostClicked().Forget())
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

        private async UniTask OnHostClicked()
        {
            // TODO: Lobby 생성 + Relay + NGO Host 시작
            NetworkManager.Singleton.StartHost();
            await AppFlow.LoadSceneAsync(SceneId.GameScene);
        }
    }
}
