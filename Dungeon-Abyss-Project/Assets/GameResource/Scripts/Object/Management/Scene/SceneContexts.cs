using System;
using System.Collections.Generic;
using DdaIT.Scene;

namespace Backend.Object.Management
{
    /// <summary>
    /// <see cref="SceneId"/> 별 <see cref="SceneContext"/> 팩토리 레지스트리.
    /// 인스턴스를 캐시하지 않고 진입할 때마다 새로 만들어, 씬을 왕복해도 이전 방문의 필드가 남지 않는다.
    /// 등록되지 않은 씬은 Create 가 null 을 반환하며, 이 경우 AppFlow 는 커튼만 걷고 넘어간다.
    /// </summary>
    internal static class SceneContexts
    {
        private static readonly Dictionary<SceneId, Func<SceneContext>> Factories = new()
        {
            [SceneId.TitleScene] = () => new TitleSceneContext(),
        };

        public static SceneContext Create(SceneId id)
            => Factories.TryGetValue(id, out var factory) ? factory() : null;
    }
}
