using System;
using System.Collections.Generic;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Tonari.Unity.SceneNavigator
{
    public interface INavigatableScene
    {
        ISceneArgs SceneArgs { get; set; }
        SceneStyle SceneStyle { get; }

        ISceneArgs ParentSceneArgs { get; }
        void SetParentSceneArgs(ISceneArgs args);

        Guid? ResultRequirementId { get; set; }

        GameObject RootObject { get; }

        IReadOnlyList<Canvas> RootCanvases { get; }

        CancellationToken SceneLifeCancellationToken { get; }

        void SetNavigator(Navigator navigator);

        UniTask ResetAsync(ISceneArgs args, TransitionMode mode);

        void Initialize();

        UniTask EnterAsync(TransitionMode mode);
        UniTask LeaveAsync(TransitionMode mode);

        void Collapse();
    }
}