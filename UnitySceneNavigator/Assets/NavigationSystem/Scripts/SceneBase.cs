using System;
using System.Threading;
using UniRx;
using UniRx.Async;
using UnityEngine;

namespace Tonari.Unity.SceneNavigator
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public abstract class SceneBase : MonoBehaviour, INavigatableScene
    {
        SceneArgs INavigatableScene.SceneArgs { get; set; }

        public SceneArgs ParentSceneArgs { get; private set; }
        void INavigatableScene.SetParentSceneArgs(SceneArgs args)
        {
            this.ParentSceneArgs = args;
        }

        Guid? INavigatableScene.ResultRequirementId { get; set; }

        GameObject INavigatableScene.RootObject => this.gameObject;
        public virtual Canvas RootCanvas { get; private set; }
        void INavigatableScene.SetRootCanvas(Canvas canvas)
        {
            this.RootCanvas = canvas;
        }

        protected Navigator Navigator { get; private set; }
        void INavigatableScene.SetNavigator(Navigator navigator)
        {
            this.Navigator = navigator;
        }

        public virtual async UniTask ResetAsync(SceneArgs args, TransitionMode mode) { }

        public abstract void Initialize();

        public virtual async UniTask EnterAsync(TransitionMode mode) { }
        public virtual async UniTask LeaveAsync(TransitionMode mode) { }

        protected SceneSharedParameter SceneShared { get; }

        private CompositeDisposable _subscriptions;
        private CancellationTokenSource _cancellationTokenSource;

        public SceneBase()
        {
            this._subscriptions = new CompositeDisposable();
            this._cancellationTokenSource = new CancellationTokenSource();

            this.SceneShared = new SceneSharedParameter(this._subscriptions, this._cancellationTokenSource);
        }

        void INavigatableScene.OnCollapse()
        {
            this._subscriptions.Dispose();
            this._cancellationTokenSource.Cancel();
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}