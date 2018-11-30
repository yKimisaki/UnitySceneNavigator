using System;
using System.Collections.Generic;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Tonari.Unity.SceneNavigator
{
    public interface INavigatableScene
    {
        SceneArgs SceneArgs { get; set; }

        SceneArgs ParentSceneArgs { get; }
        void SetParentSceneArgs(SceneArgs args);

        Guid? ResultRequirementId { get; set; }

        GameObject RootObject { get; }

        IReadOnlyList<Canvas> RootCanvases { get; }

        CancellationToken SceneLifeCancellationToken { get; }

        void SetNavigator(Navigator navigator);

        UniTask ResetAsync(SceneArgs args, TransitionMode mode);

        void Initialize();

        UniTask EnterAsync(TransitionMode mode);
        UniTask LeaveAsync(TransitionMode mode);

        void Collapse();
    }
}