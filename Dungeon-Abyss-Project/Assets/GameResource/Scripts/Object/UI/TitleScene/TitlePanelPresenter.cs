using DdaIT.Object.Management;
using DdaIT.Object.UI;
using R3;
using Cysharp.Threading.Tasks;

namespace DdaIT
{
    public class TitlePanelPresenter : UIPresenter<TitlePanel>
    {
        private readonly CompositeDisposable _disposables = new();

        public override void OnOpen()
        {
            base.OnOpen();

            View.GameHostButton.OnClickAsObservable().Subscribe(OnGameStartButtonClicked).AddTo(_disposables);
            View.GameClientButton.OnClickAsObservable().Subscribe(OnGameClientButtonClicked).AddTo(_disposables);
            View.GameOptionButton.OnClickAsObservable().Subscribe(OnGameOptionButtonClicked).AddTo(_disposables);
            View.GameExitButton.OnClickAsObservable().Subscribe(OnGameExitButtonClicked).AddTo(_disposables);
        }

        public override void OnClose()
        {
            _disposables.Clear();
            base.OnClose();
        }

        private void OnGameExitButtonClicked(Unit unit)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
            #if UNITY_STANDALONE
            UnityEngine.Application.Quit();
            #endif
        }

        private void OnGameOptionButtonClicked(Unit _)
        {
            UIManager.OpenAsync<OptionPopup>().Forget();
        }

        private void OnGameStartButtonClicked(Unit _)
        {
            UIManager.OpenAsync<HostPopup>().Forget();
        }

        private void OnGameClientButtonClicked(Unit _)
        {
            UIManager.OpenAsync<ClientPopup>().Forget();
        }
    }
}