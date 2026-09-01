using Backend.Util.Debug;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.Management
{
    /// <summary>
    /// 앱 최초 진입 씬. 로고 페이드와 Boot 초기화를 병렬로 진행한 뒤 타이틀로 넘긴다.
    /// SceneContext가 아니라 SceneContextBase를 상속해야 Boot 대기 없이 연출이 시작된다.
    /// </summary>
    public class LoadingSceneContext : SceneContextBase
    {
        [SerializeField] private Image _logo;
        [SerializeField] private float _fadeInDuration = 1f;
        [SerializeField] private float _fadeOutDuration = 0.8f;

        /// <summary>
        /// 로고 FadeIn과 Boot 대기를 겹친 뒤, 초기화가 끝난 다음 FadeOut하고 Title로 전환한다.
        /// </summary>
        protected override async UniTask OnEnterAsync()
        {
            var bootReady = Boot.WaitUntilReadyAsync();

            await FadeAsync(0f, 1f, _fadeInDuration);

            try
            {
                await bootReady;
            }
            catch (System.Exception e)
            {
                Debugger.LogError($"[LoadingSceneContext] Boot 초기화 실패: {e}");
                return;
            }

            await FadeAsync(1f, 0f, _fadeOutDuration);

            await SceneLoader.LoadAsync(SceneId.TitleScene, useLoadingPanel: false);
        }

        /// <summary>
        /// 로고 알파를 from에서 to로 보간한다. 로고가 없으면 즉시 완료한다.
        /// </summary>
        private UniTask FadeAsync(float from, float to, float duration)
        {
            if (_logo == null)
                return UniTask.CompletedTask;

            return LMotion.Create(from, to, duration)
                .BindToColorA(_logo)
                .ToUniTask();
        }
    }
}
