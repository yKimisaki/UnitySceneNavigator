using System;
using Tonari.Unity.SceneNavigator;
using UniRx.Async;
using UnityEngine;

namespace Tonari.Unity.NavigationSystemSample
{
    public class TransitionAnimator
    {
        private RuntimeAnimatorController _animator;

        public UniTask OnNavigatedAsync(INavigationResult result)
        {
            if (this._animator == null)
            {
                this._animator = Resources.Load<RuntimeAnimatorController>("Animator/NavigationAnimator");
            }

            var nextSceneAnimator = result.NextScene.RootObject.GetComponent<Animator>();
            if (nextSceneAnimator == null)
            {
                nextSceneAnimator = result.NextScene.RootObject.AddComponent<Animator>();
            }
            nextSceneAnimator.runtimeAnimatorController = this._animator;

            var prevSceneAnimator = default(Animator);
            if (result.PreviousScene != null)
            {
                prevSceneAnimator = result.PreviousScene.RootObject.GetComponent<Animator>();
                if (prevSceneAnimator == null)
                {
                    prevSceneAnimator = result.PreviousScene.RootObject.AddComponent<Animator>();
                }
                prevSceneAnimator.runtimeAnimatorController = this._animator;
            }

            if (result.TransitionMode.HasFlag(TransitionMode.KeepCurrent))
            {
                if (result.TransitionMode.HasFlag(TransitionMode.New))
                {
                    nextSceneAnimator.Play("TransitionOpen");
                }
                else if (result.TransitionMode.HasFlag(TransitionMode.Back))
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