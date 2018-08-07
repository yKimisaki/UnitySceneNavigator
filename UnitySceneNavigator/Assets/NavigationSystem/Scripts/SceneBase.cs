using System;
using System.Threading;
using UniRx;
using UniRx.Async;
using UnityEngine;

namespace Tonari.Unity.SceneNavigator
{
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
        
        public virtual UniTask ResetAsync(SceneArgs args, TransitionMode mode) => UniTask.CompletedTask;

        public abstract void Initialize();

        public virtual UniTask EnterAsync(TransitionMode mode) => UniTask.CompletedTask;
        public virtual UniTask LeaveAsync(TransitionMode mode) => UniTask.CompletedTask;

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
}