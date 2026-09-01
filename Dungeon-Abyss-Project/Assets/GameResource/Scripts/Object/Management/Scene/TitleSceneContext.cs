using Backend.Object.Management;
using Cysharp.Threading.Tasks;

namespace DdaIT.Scene
{
    public class TitleSceneContext : SceneContext
    {
        protected override UniTask OnBootReadyAsync()
        {
            UIManager.OpenAsync<TitlePanel>();
            return UniTask.CompletedTask;
        }
    }
}
