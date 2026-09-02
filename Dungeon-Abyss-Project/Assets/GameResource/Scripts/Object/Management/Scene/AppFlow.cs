using System;
using Backend.Object.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Backend.Object.Management
{
    /// <summary>
    /// 빌드 세팅에 등록된 씬 파일명과 1:1 대응한다.
    /// </summary>
    public enum SceneId
    {
        TitleScene,
    }

    /// <summary>
    /// 부트스트랩과 씬 전환을 하나의 선형 흐름으로 묶는다.
    /// - Run: 앱 시작 1회. BootCurtain → SplashPanel → GameManager 코어 초기화 → 첫 씬 Context 진입.
    /// - LoadSceneAsync: 씬 전환. LoadingPanel 커튼을 직접 들고 페이드/진행률을 구동하며,
    ///   allowSceneActivation 게이트로 씬 활성화 전 대상 씬의 PreloadAsync 를 병렬로 끝낸다.
    /// 커튼(Splash/Loading) 연출의 주인은 AppFlow 다. UIManager 는 타입을 모르는 UI 스위치로만 쓰인다.
    /// </summary>
    public static class AppFlow
    {
        private const float SplashMinDuration = 1.2f;
        private const int BootCurtainSortingOrder = 30000;

        private const float SceneLoadWeight = 0.6f;
        private const float PreloadWeight = 0.3f;

        private static bool _isLoading;
        private static SceneContext _current;
        private static UniTaskCompletionSource _readySource = new UniTaskCompletionSource();

        /// <summary>부트스트랩 초기화(코어 + 첫 씬 진입)가 완료되었는지 여부.</summary>
        public static bool IsReady { get; private set; }

        /// <summary>부트스트랩이 끝날 때까지 대기한다. 이미 완료된 경우 즉시 반환한다.</summary>
        public static UniTask WaitUntilReadyAsync()
            => IsReady ? UniTask.CompletedTask : _readySource.Task;

        /// <summary>
        /// 도메인 리로드 비활성화/씬 재진입 등에서 잔존하는 정적 상태를 안전하게 초기화한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsReady = false;
            _isLoading = false;
            _current = null;
            _readySource = new UniTaskCompletionSource();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Run() => RunAsync().Forget();

        private static async UniTaskVoid RunAsync()
        {
            var bootCurtain = CreateBootCurtain();
            try
            {
                var splash = await UIManager.OpenAsync<SplashPanel>();
                UnityEngine.Object.Destroy(bootCurtain);

                var minShow = UniTask.Delay(TimeSpan.FromSeconds(SplashMinDuration));
                await splash.FadeLogoInAsync();

                // 코어 초기화와 커튼 프리워밍을 겹친다.
                await UniTask.WhenAll(
                    GameManager.InitializeCore(),
                    UIManager.PreloadAsync<LoadingPanel>());

                // 첫 씬은 이미 로드돼 있으므로 씬 로드 단계를 건너뛰고 Context 만 돌린다.
                var currentId = CurrentSceneId();
                _current = currentId.HasValue ? SceneContexts.Create(currentId.Value) : null;
                if (_current != null)
                {
                    await _current.PreloadAsync();
                    await _current.EnterAsync();
                }

                IsReady = true;
                _readySource.TrySetResult();

                await minShow;
                await splash.FadeOutAsync();
                UIManager.Close(splash);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AppFlow] 부트 실패: {e}");
            }
        }

        public static async UniTask LoadSceneAsync(SceneId next)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[AppFlow] 전환 중 재호출 무시: {next}");
                return;
            }

            _isLoading = true;
            try
            {
                await UIManager.BlockUI();

                // 커튼 인스턴스를 AppFlow 가 직접 보유한다.
                var loading = await UIManager.OpenAsync<LoadingPanel>();
                await loading.FadeInAsync();

                // 이전 씬이 아직 살아있는 동안 정리한다.
                if (_current != null)
                    await _current.ExitAsync();
                await UIManager.CloseAllUIAsync();

                var nextContext = SceneContexts.Create(next);

                var op = SceneManager.LoadSceneAsync(next.ToString());
                op.allowSceneActivation = false;

                var progress = new LoadProgress();
                await UniTask.WhenAll(
                    WaitSceneLoadedAsync(op, loading, progress),
                    PreloadNextAsync(nextContext, loading, progress));

                op.allowSceneActivation = true;
                await op.ToUniTask();

                _current = nextContext;
                if (_current != null)
                    await _current.EnterAsync();

                loading.SetProgress(1f);
                await loading.FadeOutAsync();
                UIManager.Close(loading);
                UIManager.UnblockUI();
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>씬 로드 0.6 + 대상 씬 프리로드 0.3 의 합산 진행률. 나머지 0.1 은 EnterAsync 완료 시 한 번에 채운다.</summary>
        private sealed class LoadProgress
        {
            public float Scene;
            public float Preload;
            public float Total => Scene + Preload;
        }

        /// <summary>
        /// allowSceneActivation = false 인 동안 op.progress 는 0.9 에서 멈춘다.
        /// 여기서 폴링해 0.9 도달을 감지하지 않으면 이후의 activation 대기가 데드락된다.
        /// </summary>
        private static async UniTask WaitSceneLoadedAsync(AsyncOperation op, LoadingPanel loading, LoadProgress progress)
        {
            while (op.progress < 0.9f)
            {
                progress.Scene = op.progress / 0.9f * SceneLoadWeight;
                loading.SetProgress(progress.Total);
                await UniTask.Yield();
            }

            progress.Scene = SceneLoadWeight;
            loading.SetProgress(progress.Total);
        }

        private static async UniTask PreloadNextAsync(SceneContext context, LoadingPanel loading, LoadProgress progress)
        {
            if (context != null)
                await context.PreloadAsync();

            progress.Preload = PreloadWeight;
            loading.SetProgress(progress.Total);
        }

        private static SceneId? CurrentSceneId()
            => Enum.TryParse<SceneId>(SceneManager.GetActiveScene().name, out var id) ? id : (SceneId?)null;

        /// <summary>
        /// 코드로 생성하는 불투명 검정 커튼. SplashPanel 이 Addressable 비동기 로드로 몇 프레임 늦게 뜨는 동안
        /// TitleScene 이 노출되는 것을 막는 유일한 장치. 스플래시가 화면을 덮은 직후 곧바로 Destroy 된다.
        /// </summary>
        private static GameObject CreateBootCurtain()
        {
            var root = new GameObject("[AppFlow] BootCurtain");
            UnityEngine.Object.DontDestroyOnLoad(root);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = BootCurtainSortingOrder;

            var imageGo = new GameObject("Image");
            imageGo.transform.SetParent(root.transform, false);
            var img = imageGo.AddComponent<Image>();
            img.color = Color.black;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return root;
        }
    }
}
