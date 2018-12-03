using System;
using System.Collections.Generic;
using System.Threading;
using UniRx;
using UniRx.Async;
using UnityEngine;

namespace Tonari.Unity.SceneNavigator
{
    public abstract class SceneBase : MonoBehaviour, INavigatableScene
    {
        ISceneArgs INavigatableScene.SceneArgs { get; set; }

        public ISceneArgs ParentSceneArgs { get; private set; }
        void INavigatableScene.SetParentSceneArgs(ISceneArgs args)
        {
            this.ParentSceneArgs = args;
        }

        Guid? INavigatableScene.ResultRequirementId { get; set; }

        GameObject INavigatableScene.RootObject => this.gameObject;

        [SerializeField]
        public Canvas[] RootCanvases;
        IReadOnlyList<Canvas> INavigatableScene.RootCanvases { get { return this.RootCanvases; } }

        protected Navigator Navigator { get; private set; }
        void INavigatableScene.SetNavigator(Navigator navigator)
        {
            this.Navigator = navigator;
        }
        
        public virtual UniTask ResetAsync(ISceneArgs args, TransitionMode mode, IProgress<float> progress) => UniTask.CompletedTask;

        public abstract void Initialize();

        public virtual UniTask EnterAsync(TransitionMode mode, IProgress<float> progress) => UniTask.CompletedTask;
        public virtual UniTask LeaveAsync(TransitionMode mode, IProgress<float> progress) => UniTask.CompletedTask;

        protected SceneSharedParameter SceneShared { get; }
        CancellationToken INavigatableScene.SceneLifeCancellationToken { get { return this.SceneShared.CancellationTokenSource.Token; } }

        private CompositeDisposable _subscriptions;
        private CancellationTokenSource _cancellationTokenSource;

        protected SceneBase()
        {
            this._subscriptions = new CompositeDisposable();
            this._cancellationTokenSource = new CancellationTokenSource();

            this.SceneShared = new SceneSharedParameter(this._subscriptions, this._cancellationTokenSource);
        }

        void INavigatableScene.Collapse()
        {
            this._subscriptions.Dispose();
            this._cancellationTokenSource.Cancel();
        }
    }
}