using System;
using Tonari.Unity.SceneNavigator;
using UniRx.Async;
using UnityEngine;

namespace Tonari.Unity.NavigationSystemSample
{
    public class TransitionAnimator : IAfterTransition
    {
        private RuntimeAnimatorController _animatorController;

        public async UniTask OnNavigatedAsync(INavigationContext context)
        {
            if (this._animatorController != null)
            {
                this._animatorController = Resources.Load<RuntimeAnimatorController>("Animator/NavigationAnimator");
            }

            var nextSceneAnimator = context.NextScene.RootObject.GetComponent<Animator>();
            if (nextSceneAnimator == null)
            {
                nextSceneAnimator = context.NextScene.RootObject.AddComponent<Animator>();
            }
            nextSceneAnimator.runtimeAnimatorController = this._animatorController;

            var prevSceneAnimator = default(Animator);
            if (context.PreviousScene != null)
            {
                prevSceneAnimator = context.PreviousScene.RootObject.GetComponent<Animator>();
                if (prevSceneAnimator == null)
                {
                    prevSceneAnimator = context.PreviousScene.RootObject.AddComponent<Animator>();
                }
                prevSceneAnimator.runtimeAnimatorController = this._animatorController;
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

            await UniTask.Delay(TimeSpan.FromSeconds(0.3));
        }
    }
}