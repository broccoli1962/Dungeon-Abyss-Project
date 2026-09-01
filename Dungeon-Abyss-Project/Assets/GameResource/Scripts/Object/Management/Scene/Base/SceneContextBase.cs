using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Backend.Object.Management
{
    public abstract class SceneContextBase : MonoBehaviour
    {
        private void Start() => EnterAsync().Forget();

        private async UniTaskVoid EnterAsync() => await OnEnterAsync();

        private void OnDestroy(){
            if(GameStateUtil.IsQuitting) return;
            OnExit();
        }

        protected abstract UniTask OnEnterAsync();
        protected virtual void OnExit() { }
    }
}
