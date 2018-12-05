using System;
using System.Threading;
using Tonari.Unity.SceneNavigator;
using UniRx.Async;
using UnityEngine;

namespace Tonari.Unity.NavigationSystemSample
{
    public class TransitionAnimator : IAfterTransition
    {
        private RuntimeAnimatorController _animatorController;

        public TransitionAnimator()
        {
            this._animatorController = Resources.Load<RuntimeAnimatorController>("Animator/NavigationAnimator");
        }

        public UniTask OnEnteredAsync(INavigationContext context, CancellationToken token, IProgress<float> progress)
        {
            var nextSceneAnimator = context.NextScene.RootObject.GetComponent<Animator>();
            if (nextSceneAnimator == null)
            {
                nextSceneAnimator = context.NextScene.RootObject.AddComponent<Animator>();
            }
            nextSceneAnimator.runtimeAnimatorController = this._animatorController;

            if (context.TransitionMode.HasFlag(TransitionMode.KeepCurrent | TransitionMode.New))
            {
                nextSceneAnimator.Play("TransitionOpen");
            }

            return UniTask.CompletedTask;
        }

        public UniTask OnLeftAsync(INavigationContext context, CancellationToken token, IProgress<float> progress)
        {
            var prevSceneAnimator = context.PreviousScene.RootObject.GetComponent<Animator>();
            if (prevSceneAnimator == null)
            {
                prevSceneAnimator = context.PreviousScene.RootObject.AddComponent<Animator>();
            }
            prevSceneAnimator.runtimeAnimatorController = this._animatorController;

            if (context.TransitionMode.HasFlag(TransitionMode.KeepCurrent) && context.TransitionMode.HasFlag(TransitionMode.Back))
            {
                prevSceneAnimator.Play("TransitionClose");
                return UniTask.Delay(TimeSpan.FromSeconds(0.25));
            }

            return UniTask.CompletedTask;
        }
    }
}