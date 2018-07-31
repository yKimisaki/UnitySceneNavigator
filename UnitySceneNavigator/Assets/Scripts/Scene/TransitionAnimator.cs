using System;
using Tonari.Unity.SceneNavigator;
using UniRx.Async;
using UnityEngine;

namespace Tonari.Unity.NavigationSystemSample
{
    public class TransitionAnimator : IAfterTransition
    {
        private RuntimeAnimatorController _animator;

        public UniTask OnNavigatedAsync(INavigationContext context)
        {
            if (this._animator == null)
            {
                this._animator = Resources.Load<RuntimeAnimatorController>("Animator/NavigationAnimator");
            }

            var nextSceneAnimator = context.NextScene.RootObject.GetComponent<Animator>();
            if (nextSceneAnimator == null)
            {
                nextSceneAnimator = context.NextScene.RootObject.AddComponent<Animator>();
            }
            nextSceneAnimator.runtimeAnimatorController = this._animator;

            var prevSceneAnimator = default(Animator);
            if (context.PreviousScene != null)
            {
                prevSceneAnimator = context.PreviousScene.RootObject.GetComponent<Animator>();
                if (prevSceneAnimator == null)
                {
                    prevSceneAnimator = context.PreviousScene.RootObject.AddComponent<Animator>();
                }
                prevSceneAnimator.runtimeAnimatorController = this._animator;
            }

            if (context.TransitionMode.HasFlag(TransitionMode.KeepCurrent))
            {
                if (context.TransitionMode.HasFlag(TransitionMode.New))
                {
                    nextSceneAnimator.Play("TransitionOpen");
                }
                else if (context.TransitionMode.HasFlag(TransitionMode.Back))
                {
                    if (prevSceneAnimator != null)
                    {
                        prevSceneAnimator.Play("TransitionClose");
                    }
                }
            }

            return UniTask.Delay(TimeSpan.FromSeconds(0.3));
        }
    }
}