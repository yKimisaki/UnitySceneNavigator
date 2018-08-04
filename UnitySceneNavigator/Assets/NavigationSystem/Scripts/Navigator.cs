using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tonari.Unity.SceneNavigator
{
    public class Navigator
    {
        private Dictionary<string, INavigatableScene> _scenesByName;
        private Stack<NavigationStackElement> _navigateHistoryStack;
        private INavigatableScene _currentScene;

        private ILoadingDisplaySelector _loadingDisplaySelector;
        private Dictionary<int, ILoadingDisplay> _loadingDisplaysByOption;

        private Dictionary<Guid, UniTaskCompletionSource<object>> _taskCompletionSourcesByResultRequirementId;

        private ICanvasOrderArranger _canvasOrderArranger;

        private ICanvasCustomizer _canvasCustomizer;

        private IAfterTransition _afterTransition;

        public Navigator(ILoadingDisplaySelector loadingDisplaySelector, ICanvasCustomizer canvasCustomizer, ICanvasOrderArranger canvasOrderArranger, IAfterTransition afterTransition)
        {
            this._scenesByName = new Dictionary<string, INavigatableScene>();

            this._navigateHistoryStack = new Stack<NavigationStackElement>();

            this._loadingDisplaySelector = loadingDisplaySelector;
            this._loadingDisplaysByOption = new Dictionary<int, ILoadingDisplay>();

            this._taskCompletionSourcesByResultRequirementId = new Dictionary<Guid, UniTaskCompletionSource<object>>();

            this._canvasOrderArranger = canvasOrderArranger ?? new DefaultCanvasOrderArranger();

            this._canvasCustomizer = canvasCustomizer;
            
            this._afterTransition = afterTransition;
        }

        public virtual UniTask NavigateAsync(SceneArgs args, IProgress<float> progress = null)
        {
            // 結果を待ってるシーンがあるならダメ
            if (this._taskCompletionSourcesByResultRequirementId.Count > 0)
            {
                throw new NavigationFailureException("結果を待っているシーンがあります", args);
            }

            return NavigateCoreAsync(args, NavigationOption.Push, progress);
        }

        public virtual async UniTask NavigateBackAsync(object result = null, IProgress<float> progress = null)
        {
            var previousObject = this._navigateHistoryStack.Peek();
            if (previousObject == null)
            {
                throw new NavigationFailureException("シーンスタックがありません", null);
            }

            var previousScene = default(INavigatableScene);
            if (!this._scenesByName.TryGetValue(previousObject.SceneName, out previousScene))
            {
                throw new NavigationFailureException("無効なシーンが設定されています", null);
            }

            // 先に結果要求IDを貰っておく
            var resultRequirementId = previousScene.ResultRequirementId;

            // 遷移
            var option = NavigationOption.Pop;
            if (previousObject.TransitionMode.HasFlag(TransitionMode.KeepCurrent))
            {
                option |= NavigationOption.Override;
            }
            await NavigateCoreAsync(previousScene.ParentSceneArgs, option, progress);

            if (resultRequirementId.HasValue && this._taskCompletionSourcesByResultRequirementId.ContainsKey(resultRequirementId.Value))
            {
                var taskCompletionSource = this._taskCompletionSourcesByResultRequirementId[resultRequirementId.Value];
                if (taskCompletionSource == null)
                {
                    throw new NavigationFailureException("戻り値が取得できません", previousScene.SceneArgs);
                }

                if (!taskCompletionSource.TrySetResult(result))
                {
                    throw new NavigationFailureException("結果の代入に失敗しました", previousScene.SceneArgs);
                }
            }
            else
            {
                throw new NavigationFailureException("戻り値が取得できません", previousScene.SceneArgs);
            }
        }

        public virtual async UniTask<TResult> NavigateAsPopupAsync<TResult>(SceneArgs args, IProgress<float> progress = null)
        {
            var resultRequirementId = Guid.NewGuid();
            var taskCompletionSource = new UniTaskCompletionSource<object>();

            if (this._taskCompletionSourcesByResultRequirementId.ContainsKey(resultRequirementId))
            {
                throw new NavigationFailureException("シーンをテーブルに追加できませんでした", args);
            }
            this._taskCompletionSourcesByResultRequirementId[resultRequirementId] = taskCompletionSource;

            var activateResult = await NavigateCoreAsync(args, NavigationOption.Popup, progress);
            // ここでダメな場合は既にActivateAsyncでエラーを吐いてるハズ
            if (activateResult == null)
            {
                return default(TResult);
            }

            activateResult.NextScene.ResultRequirementId = resultRequirementId;

            var result = await taskCompletionSource.Task;

            if (!(result is TResult))
            {
                throw new NavigationFailureException($"戻り値の型は{typeof(TResult)}を期待しましたが、{result.GetType()}が返されました", args);
            }

            return (TResult)result;
        }

        private async UniTask<NavigationResult> NavigateCoreAsync(SceneArgs args, NavigationOption option = NavigationOption.None, IProgress<float> progress = null)
        {
            using (NavigationLock.Acquire(args))
            {
                var loadingDisplay = default(ILoadingDisplay);

                if (this._loadingDisplaySelector != null)
                {
                    if (this._loadingDisplaysByOption.ContainsKey((int)option))
                    {
                        loadingDisplay = this._loadingDisplaysByOption[(int)option];
                    }
                    else
                    {
                        loadingDisplay = this._loadingDisplaysByOption[(int)option] = this._loadingDisplaySelector.SelectDisplay(option);
                    }
                }

                if (loadingDisplay != null)
                {
                    loadingDisplay.Show();
                }

                NavigationResult activationResult;
                if (this._scenesByName.ContainsKey(args.SceneName))
                {
                    // 既にInitialize済みのSceneであればActivateするだけでOK
                    activationResult = Activate(args, option);
                }
                else
                {
                    activationResult = await LoadAsync(args, option, progress);
                }
                // ここでダメな場合は既にActivateAsyncでエラーを吐いてるハズ
                if (activationResult == null || activationResult.NextScene == null)
                {
                    return null;
                }

                if (option.HasFlag(NavigationOption.Push))
                {
                    // 新しいシーンをスタックに積む
                    this._navigateHistoryStack.Push(new NavigationStackElement() { SceneName = args.SceneName, TransitionMode = activationResult.TransitionMode });
                }

                // 新しいシーンをリセットする
                await activationResult.NextScene.ResetAsync(args, activationResult.TransitionMode);

                // 新規シーンなら初期化する
                if (activationResult.TransitionMode.HasFlag(TransitionMode.New))
                {
                    activationResult.NextScene.Initialize();
                }

                // 新規シーンに入る
                await activationResult.NextScene.EnterAsync(activationResult.TransitionMode);

                // 新規シーンに入ったら外部の遷移処理を呼ぶ
                activationResult.NextScene.RootCanvas.enabled = false;
                activationResult.NextScene.RootObject.SetActive(true);
                await this._afterTransition.OnAfterEnterAsync(activationResult);
                activationResult.NextScene.RootCanvas.enabled = true;

                // 古いシーンから出る
                if (activationResult.PreviousScene != null)
                {
                    await activationResult.PreviousScene.LeaveAsync(activationResult.TransitionMode);

                    // 古いシーンの遷移処理を呼ぶ
                    await this._afterTransition.OnAfterLeaveAsync(activationResult);

                    // 上に乗せるフラグが無ければ非アクティブ化
                    if (!option.HasFlag(NavigationOption.Override))
                    {
                        activationResult.PreviousScene.RootObject.SetActive(false);
                    }

                    // Popするならアンロードも行う
                    if (option.HasFlag(NavigationOption.Pop))
                    {
                        // 古いシーンをスタックから抜いてアンロード
                        var popObject = this._navigateHistoryStack.Pop();
                        await UnloadAsync(activationResult.PreviousScene.SceneArgs, progress);
                    }
                }

                if (loadingDisplay != null)
                {
                    loadingDisplay.Hide();
                }

                return activationResult;
            }
        }

        private async UniTask<NavigationResult> LoadAsync(SceneArgs args, NavigationOption option = NavigationOption.None, IProgress<float> progress = null)
        {
            var asyncOperation = SceneManager.LoadSceneAsync(args.SceneName, LoadSceneMode.Additive);

            progress?.Report(0f);
            while (!asyncOperation.isDone)
            {
                progress?.Report(asyncOperation.progress);
                await UniTask.Delay(TimeSpan.FromSeconds(Time.fixedDeltaTime));
            }
            progress?.Report(1f);

            var result = Activate(args, option);

            // ここに来たという事は新規
            result.TransitionMode |= TransitionMode.New;

            if (this._scenesByName.ContainsKey(args.SceneName))
            {
                throw new NavigationFailureException("シーンを重複して読み込もうとしています", args);
            }
            this._scenesByName[args.SceneName] = result.NextScene;

            // ロード時にCanvasの調整をする
            if (this._canvasCustomizer != null)
            {
                this._canvasCustomizer.Customize(result.NextScene.RootCanvas);
            }

            return result;
        }

        private async UniTask UnloadAsync(SceneArgs args, IProgress<float> progress = null)
        {
            var asyncOperation = SceneManager.UnloadSceneAsync(args.SceneName);

            progress?.Report(0f);
            while (!asyncOperation.isDone)
            {
                progress?.Report(asyncOperation.progress);
                await UniTask.Delay(TimeSpan.FromSeconds(Time.fixedDeltaTime));
            }
            progress?.Report(1f);

            if (!this._scenesByName.Remove(args.SceneName))
            {
                throw new NavigationFailureException("シーンをテーブルから削除できませんでした", args);
            }
        }

        private NavigationResult Activate(SceneArgs args, NavigationOption option = NavigationOption.None)
        {
            var result = new NavigationResult();

            if (this._currentScene != null)
            {
                var currentUnityScene = SceneManager.GetSceneByName(this._currentScene.SceneArgs.SceneName);
                if (!currentUnityScene.isLoaded)
                {
                    throw new NavigationFailureException("無効なシーンが設定されています", args);
                }

                result.PreviousScene = this._currentScene;
            }

            // シーンマネージャの方から次のSceneを取得
            var nextUnityScene = SceneManager.GetSceneByName(args.SceneName);
            if (!nextUnityScene.isLoaded)
            {
                throw new NavigationFailureException("シーンの読み込みに失敗しました", args);
            }
            if (nextUnityScene.rootCount != 1)
            {
                throw new NavigationFailureException("シーンのRootObjectが複数あります", args);
            }

            // SceneからINavigatableSceneを取得
            var rootObjects = nextUnityScene.GetRootGameObjects();
            if (rootObjects.Length == 0)
            {
                throw new NavigationFailureException("RootObjectが存在しません", args);
            }
            if (rootObjects.Length > 1)
            {
                throw new NavigationFailureException("RootObjectが複数あります", args);
            }

            var containsCanvases = rootObjects[0].GetComponentsInChildren<Canvas>();
            if (containsCanvases.Length == 0)
            {
                throw new NavigationFailureException("Canvasが見つかりませんでした", args);
            }

            var sceneBases = rootObjects[0].GetComponents<SceneBase>();
            if (sceneBases.Length == 0)
            {
                throw new NavigationFailureException("SceneBaseコンポーネントがRootObjectに存在しません", args);
            }
            if (sceneBases.Length > 1)
            {
                throw new NavigationFailureException("SceneBaseコンポーネントが複数あります", args);
            }

            // 進む場合、新しいシーンは非表示にしておく
            if (!option.HasFlag(NavigationOption.Pop))
            {
                rootObjects[0].SetActive(false);
            }

            // 次のシーンに諸々引数を渡す
            var nextScene = sceneBases[0] as INavigatableScene;
            nextScene.SetRootCanvas(containsCanvases[0]);
            nextScene.SceneArgs = args;
            nextScene.SetNavigator(this);
            if (this._currentScene != null)
            {
                nextScene.SetParentSceneArgs(this._currentScene.SceneArgs);
            }

            // 進む場合、ソートを整える
            if (!option.HasFlag(NavigationOption.Pop))
            {
                if (this._currentScene != null)
                {
                    nextScene.RootCanvas.sortingOrder = this._canvasOrderArranger.GetOrder(this._currentScene.RootCanvas.sortingOrder, option);
                }
                else
                {
                    nextScene.RootCanvas.sortingOrder = this._canvasOrderArranger.InitialOrder;
                }
            }

            // 次のシーンにnextSceneを設定
            this._currentScene = result.NextScene = nextScene;

            // TransitionModeの調整
            if (option.HasFlag(NavigationOption.Override))
            {
                result.TransitionMode |= TransitionMode.KeepCurrent;
            }
            if (option.HasFlag(NavigationOption.Pop))
            {
                result.TransitionMode |= TransitionMode.Back;
            }

            return result;
        }

        private class NavigationResult : INavigationContext
        {
            public INavigatableScene NextScene { get; set; }
            public INavigatableScene PreviousScene { get; set; }
            public TransitionMode TransitionMode { get; set; }
        }

        private class NavigationStackElement
        {
            public string SceneName { get; set; }
            public TransitionMode TransitionMode { get; set; }
        }

        private class NavigationLock : IDisposable
        {
            private static NavigationLock _lock;

            public static NavigationLock Acquire(SceneArgs args)
            {
                if (_lock != null)
                {
                    throw new NavigationFailureException("前回の遷移が終了する前に新しい遷移が開始されました", args);
                }

                return _lock = new NavigationLock();
            }

            private NavigationLock() { }

            public void Dispose()
            {
                _lock = null;
            }
        }
    }
}