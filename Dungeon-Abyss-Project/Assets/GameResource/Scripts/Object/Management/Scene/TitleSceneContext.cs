using Backend.Object.Management;
using Cysharp.Threading.Tasks;

namespace DdaIT.Scene
{
    public sealed class TitleSceneContext : SceneContext
    {
        public override UniTask PreloadAsync()
            => UIManager.PreloadAsync<TitlePanel>();

        public override async UniTask EnterAsync()
            => await UIManager.OpenAsync<TitlePanel>();
    }
}
