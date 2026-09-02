using Cysharp.Threading.Tasks;

namespace Backend.Object.Management
{
    /// <summary>
    /// 씬 하나를 책임지는 진입점. 씬에 배치하지 않으며 <see cref="AppFlow"/> 가 단계별로 호출한다.
    /// - PreloadAsync: 씬 활성화 전. 씬 로드와 병렬로 실행된다.
    /// - EnterAsync:   씬 활성화 후. 여기서 UI/오브젝트를 조립한다.
    /// - ExitAsync:    다음 씬 로드 전. 이전 씬이 아직 살아있는 상태에서 정리한다.
    /// </summary>
    public abstract class SceneContext
    {
        public virtual UniTask PreloadAsync() => UniTask.CompletedTask;
        public abstract UniTask EnterAsync();
        public virtual UniTask ExitAsync() => UniTask.CompletedTask;
    }
}
