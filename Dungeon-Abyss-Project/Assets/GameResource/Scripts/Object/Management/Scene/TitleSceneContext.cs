using Cysharp.Threading.Tasks;

namespace DdaIT.Object.Management
{
    public sealed class TitleSceneContext : SceneContext
    {
        public override UniTask PreloadAsync()
            => UniTask.WhenAll(
                UIManager.PreloadAsync<TitlePanel>(),
                UIManager.PreloadAsync<OptionPopup>());

        public override async UniTask EnterAsync()
            => await UIManager.OpenAsync<TitlePanel>();
    }
}
