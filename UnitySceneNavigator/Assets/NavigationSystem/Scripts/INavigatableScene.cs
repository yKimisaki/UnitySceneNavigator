using System;
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
        Canvas RootCanvas { get; }
        void SetRootCanvas(Canvas canvas);

        void SetNavigator(Navigator navigator);

        UniTask ResetAsync(SceneArgs args, TransitionMode mode);

        void Initialize();

        UniTask EnterAsync(TransitionMode mode);
        UniTask LeaveAsync(TransitionMode mode);
    }
}