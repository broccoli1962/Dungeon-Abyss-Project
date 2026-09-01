using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Backend.Object.Management
{
    /// <summary>
    /// 빌드 세팅에 등록된 씬 파일명과 1:1 대응한다.
    /// </summary>
    public enum SceneId
    {
        LoadingScene,
        TitleScene,
    }

    /// <summary>
    /// 빌드 세팅 씬 전환. Addressable 씬이 아니라 SceneManager 경로를 쓴다.
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>
        /// 지정한 씬으로 전환한다. 로딩 패널을 쓰면 전환 중 입력을 막고 기존 UI를 닫는다.
        /// 스플래시처럼 UIManager가 아직 없는 구간에서는 useLoadingPanel을 false로 둔다.
        /// </summary>
        public static async UniTask LoadAsync(SceneId scene, bool useLoadingPanel = true)
        {
            if (useLoadingPanel)
            {
                await UIManager.BlockUI();
                await UIManager.ShowLoadingAsync();
                await UIManager.CloseAllUIAsync();
            }

            await SceneManager.LoadSceneAsync(scene.ToString()).ToUniTask();
        }
    }
}
