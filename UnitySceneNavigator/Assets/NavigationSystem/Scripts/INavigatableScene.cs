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

        ISceneArgs ParentSceneArgs { get; }
        void SetParentSceneArgs(ISceneArgs args);

        Guid? ResultRequirementId { get; set; }

        GameObject RootObject { get; }

        IReadOnlyList<Canvas> RootCanvases { get; }

        CancellationToken SceneLifeCancellationToken { get; }

        void SetNavigator(Navigator navigator);

        UniTask ResetAsync(ISceneArgs args, TransitionMode mode, IProgress<float> progress);

        void Initialize();

        UniTask EnterAsync(TransitionMode mode, IProgress<float> progress);
        UniTask LeaveAsync(TransitionMode mode, IProgress<float> progress);

        void Collapse();
    }
}