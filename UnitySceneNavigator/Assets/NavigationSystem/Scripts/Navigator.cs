using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tonari.Unity.SceneNavigator
{
    public class Navigator
    {
        private Dictionary<string, INavigatableScene> _scenesByName;
        private Dictionary<string, INavigatableScene> _currentSubScenesByName;

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
            this._currentSubScenesByName = new Dictionary<string, INavigatableScene>();

            this._navigateHistoryStack = new Stack<NavigationStackElement>();

            this._loadingDisplaySelector = loadingDisplaySelector;
            this._loadingDisplaysByOption = new Dictionary<int, ILoadingDisplay>();

            this._taskCompletionSourcesByResultRequirementId = new Dictionary<Guid, UniTaskCompletionSource<object>>();

            this._canvasOrderArranger = canvasOrderArranger ?? new DefaultCanvasOrderArranger();

            this._canvasCustomizer = canvasCustomizer;
            
            this._afterTransition = afterTransition;
        }

        //public virtual async UniTask NavigateAsync(ISceneArgs args, IProgress<float> progress = null)
        //{
        //    // 結果を待ってるシーンがあるならダメ
        //    if (this._taskCompletionSourcesByResultRequirementId.Count > 0)
        //    {
        //        throw new NavigationFailureException("結果を待っているシーンがあります", args);
        //    }

        //    for(var i = 0; i < args.SubScenes.Count; ++i)
        //    {
        //        if (args.SubScenes[i].SceneName)
        //        {

        //        }
        //    }

        //    await NavigateCoreAsync(args, NavigationOption.Push, progress);
        //}

        public virtual async UniTask<TResult> NavigateNextAsync<TResult>(ISceneArgs args, IProgress<float> progress = null)
        {
            using (var internalProgresses = new NavigationInternalProgressGroup(progress, 1))
            {
                var resultRequirementId = Guid.NewGuid();
                var taskCompletionSource = new UniTaskCompletionSource<object>();
                if (this._taskCompletionSourcesByResultRequirementId.ContainsKey(resultRequirementId))
                {
                    throw new NavigationFailureException("シーンをテーブルに追加できませんでした", args);
                }
                this._taskCompletionSourcesByResultRequirementId[resultRequirementId] = taskCompletionSource;

                var activateResult = await NavigateCoreAsync(args, NavigationOption.Popup, internalProgresses[0]);

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
        }

        public virtual async UniTask NavigateBackAsync(object result = null, IProgress<float> progress = null)
        {
            using (var internalProgresses = new NavigationInternalProgressGroup(progress, 1))
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

                await NavigateCoreAsync(previousScene.ParentSceneArgs, option, internalProgresses[0]);

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
        }

        private async UniTask<NavigationResult> NavigateCoreAsync(ISceneArgs args, NavigationOption option = NavigationOption.None, IProgress<float> progress = null)
        {
            if (this._currentSubScenesByName.ContainsKey(args.SceneName))
            {
                throw new NavigationFailureException($"{args.SceneName}は既にサブシーンとしてロードされています。サブシーンを通常シーンに変更することはできません。", args);
            }

            using (var internalProgresses = new NavigationInternalProgressGroup(progress, 6))
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

                var cancellationToken = this._currentScene?.SceneLifeCancellationToken ?? new CancellationTokenSource().Token;

                // まずサブシーンを処理する
                var subSceneTransitions = await NavigateSubScenesCoreAsync(args, cancellationToken, internalProgresses[0]);
                internalProgresses[0].Report(1f);

                NavigationResult activationResult;
                if (this._scenesByName.ContainsKey(args.SceneName))
                {
                    // 既にInitialize済みのSceneであればActivateするだけでOK
                    activationResult = this.Activate(args, option);
                }
                else
                {
                    activationResult = await this.LoadAsync(args, cancellationToken, option, internalProgresses[1]);
                }
                internalProgresses[1].Report(1f);

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
                await activationResult.NextScene.ResetAsync(args, activationResult.TransitionMode, internalProgresses[2]);
                internalProgresses[2].Report(1f);

                if (activationResult.NextScene.SceneLifeCancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), activationResult.NextScene.SceneLifeCancellationToken);
                }

                // 新規シーンなら初期化する
                if (activationResult.TransitionMode.HasFlag(TransitionMode.New))
                {
                    activationResult.NextScene.Initialize();
                }

                // 新規シーンに入る
                await activationResult.NextScene.EnterAsync(activationResult.TransitionMode, internalProgresses[3]);
                internalProgresses[3].Report(1f);
                if (activationResult.NextScene.SceneLifeCancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), activationResult.NextScene.SceneLifeCancellationToken);
                }

                // 古いシーンから出る
                if (activationResult.PreviousScene != null)
                {
                    await activationResult.PreviousScene.LeaveAsync(activationResult.TransitionMode, internalProgresses[4]);
                    internalProgresses[4].Report(1f);
                }

                // 新規シーンに入ったら外部の遷移処理を呼ぶ
                async UniTask enterNext()
                {
                    for (var i = 0; i < activationResult.NextScene.RootCanvases.Count; ++i)
                    {
                        activationResult.NextScene.RootCanvases[i].enabled = false;
                    }
                    activationResult.NextScene.RootObject.SetActive(true);

                    await this._afterTransition.OnEnteredAsync(activationResult, activationResult.NextScene.SceneLifeCancellationToken, new Progress<float>());
                    if (activationResult.NextScene.SceneLifeCancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), activationResult.NextScene.SceneLifeCancellationToken);
                    }

                    for (var i = 0; i < activationResult.NextScene.RootCanvases.Count; ++i)
                    {
                        activationResult.NextScene.RootCanvases[i].enabled = true;
                    }
                }

                // 古いシーンの遷移処理を呼ぶ
                async UniTask leavePrevious()
                {
                    if (activationResult.PreviousScene != null)
                    {
                        await this._afterTransition.OnLeftAsync(activationResult, activationResult.NextScene.SceneLifeCancellationToken, new Progress<float>());
                        if (activationResult.NextScene.SceneLifeCancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), activationResult.NextScene.SceneLifeCancellationToken);
                        }

                        // 上に乗せるフラグが無ければ非アクティブ化
                        if (!option.HasFlag(NavigationOption.Override))
                        {
                            activationResult.PreviousScene.RootObject.SetActive(false);
                        }

                        // Popするならアンロードも行う
                        if (option.HasFlag(NavigationOption.Pop))
                        {
                            // シーンのファイナライズ処理
                            activationResult.PreviousScene.Collapse();

                            // 古いシーンをスタックから抜いてアンロード
                            var popObject = this._navigateHistoryStack.Pop();
                            await UnloadAsync(activationResult.PreviousScene.SceneArgs, activationResult.NextScene.SceneLifeCancellationToken, internalProgresses[5]);
                            if (activationResult.NextScene.SceneLifeCancellationToken.IsCancellationRequested)
                            {
                                throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), activationResult.NextScene.SceneLifeCancellationToken);
                            }
                        }
                    }

                    internalProgresses[5].Report(1f);
                }

                // 遷移処理は同時に進行
                await UniTask.WhenAll(
                    enterNext(),
                    leavePrevious(), 
                    subSceneTransitions.OnEnteredAsync(activationResult.NextScene.SceneLifeCancellationToken), 
                    subSceneTransitions.OnLeftAsync(activationResult.NextScene.SceneLifeCancellationToken));
                if (activationResult.NextScene.SceneLifeCancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), activationResult.NextScene.SceneLifeCancellationToken);
                }

                if (loadingDisplay != null)
                {
                    loadingDisplay.Hide();
                }
                
                return activationResult;
            }
        }

        private async UniTask<SubSceneTransitions> NavigateSubScenesCoreAsync(ISceneArgs args, CancellationToken token, IProgress<float> progress = null)
        {
            var result = new SubSceneTransitions();

            if (!args.SubScenes.Any())
            {
                return result;
            }

            var internalProgresses = new NavigationInternalProgressGroup(progress, 2);

            var removes = this._currentSubScenesByName
                .Where(x => args.SubScenes.All(y => y.SceneName != x.Key))
                .Select(x => x.Value)
                .ToArray();
            using (var internalDisactivateProgresses = new NavigationInternalProgressGroup(internalProgresses[0], removes.Length))
            {
                for (var i = 0; i < removes.Length; ++i)
                {
                    var disactivationResult = new NavigationResult()
                    {
                        PreviousScene = removes[i],
                        TransitionMode = TransitionMode.Back,
                    };

                    await removes[i].LeaveAsync(disactivationResult.TransitionMode, internalDisactivateProgresses[i]);
                    internalDisactivateProgresses[i].Report(1f);

                    // 古いシーンの遷移処理を呼ぶ
                    result.OnLeftTasks.Add(async (CancellationToken innerToken, IProgress<float> innerProgress) =>
                    {
                        await this._afterTransition.OnLeftAsync(disactivationResult, innerToken, innerProgress);

                        // シーンのファイナライズ処理
                        disactivationResult.PreviousScene.Collapse();

                        await UnloadAsync(disactivationResult.PreviousScene.SceneArgs, token);
                    });
                }
            }

            using (var internalActivateProgresses = new NavigationInternalProgressGroup(internalProgresses[1], args.SubScenes.Count))
            {
                for (var i = 0; i < args.SubScenes.Count; ++i)
                {
                    using (var internalActivateProgresses2 = new NavigationInternalProgressGroup(internalActivateProgresses[i], 3))
                    {
                        var subSceneArgs = args.SubScenes[i];
                        if (this._scenesByName.ContainsKey(subSceneArgs.SceneName))
                        {
                            throw new NavigationFailureException($"{subSceneArgs.SceneName}は既に通常シーンとしてロードされています。通常シーンをサブシーンに変更することはできません。", args);
                        }

                        NavigationResult activationResult;
                        if (this._currentSubScenesByName.ContainsKey(args.SubScenes[i].SceneName))
                        {
                            // 既にInitialize済みのSceneであればActivateするだけでOK
                            activationResult = this.Activate(subSceneArgs, NavigationOption.Sub);
                        }
                        else
                        {
                            activationResult = await this.LoadAsync(subSceneArgs, token, NavigationOption.Sub, internalActivateProgresses2[0]);
                        }

                        internalActivateProgresses2[i].Report(1f);

                        // 新しいシーンをリセットする
                        await activationResult.NextScene.ResetAsync(args, activationResult.TransitionMode, internalActivateProgresses2[1]);
                        internalActivateProgresses2[1].Report(1f);

                        if (activationResult.NextScene.SceneLifeCancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), activationResult.NextScene.SceneLifeCancellationToken);
                        }

                        // 新規シーンなら初期化する
                        if (activationResult.TransitionMode.HasFlag(TransitionMode.New))
                        {
                            activationResult.NextScene.Initialize();
                        }

                        // 新規シーンに入る
                        await activationResult.NextScene.EnterAsync(activationResult.TransitionMode, internalActivateProgresses2[2]);
                        internalActivateProgresses2[2].Report(1f);

                        // 新規シーンに入ったら外部の遷移処理を呼ぶ
                        result.OnEnteredTasks.Add(async (CancellationToken innerToken, IProgress<float> innerProgress) =>
                        {
                            for (var j = 0; j < activationResult.NextScene.RootCanvases.Count; ++j)
                            {
                                activationResult.NextScene.RootCanvases[j].enabled = false;
                            }
                            activationResult.NextScene.RootObject.SetActive(true);

                            await this._afterTransition.OnEnteredAsync(activationResult, activationResult.NextScene.SceneLifeCancellationToken, new Progress<float>());
                            if (activationResult.NextScene.SceneLifeCancellationToken.IsCancellationRequested)
                            {
                                throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), activationResult.NextScene.SceneLifeCancellationToken);
                            }

                            for (var j = 0; j < activationResult.NextScene.RootCanvases.Count; ++j)
                            {
                                activationResult.NextScene.RootCanvases[j].enabled = true;
                            }
                        });
                    }
                }
            }

            return result;
        }

        private async UniTask<NavigationResult> LoadAsync(ISceneArgs args, CancellationToken token, NavigationOption option = NavigationOption.None, IProgress<float> progress = null)
        {
            var asyncOperation = SceneManager.LoadSceneAsync(args.SceneName, LoadSceneMode.Additive);

            progress?.Report(0f);
            while (!asyncOperation.isDone)
            {
                progress?.Report(asyncOperation.progress);
                await UniTask.Delay(TimeSpan.FromSeconds(Time.fixedDeltaTime));

                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), token);
                }
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
                this._canvasCustomizer.Customize(result.NextScene.RootCanvases);
            }

            return result;
        }

        private async UniTask UnloadAsync(ISceneArgs args, CancellationToken token, IProgress<float> progress = null)
        {
            var asyncOperation = SceneManager.UnloadSceneAsync(args.SceneName);

            progress?.Report(0f);
            while (!asyncOperation.isDone)
            {
                progress?.Report(asyncOperation.progress);
                await UniTask.Delay(TimeSpan.FromSeconds(Time.fixedDeltaTime));

                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), token);
                }
            }
            progress?.Report(1f);

            if (!this._scenesByName.Remove(args.SceneName))
            {
                throw new NavigationFailureException("シーンをテーブルから削除できませんでした", args);
            }
        }

        private NavigationResult Activate(ISceneArgs args, NavigationOption option = NavigationOption.None)
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
                    this._canvasOrderArranger.ArrangeOrder(this._currentScene.RootCanvases, option);
                }
                else
                {
                    for (var i = 0; i < nextScene.RootCanvases.Count; ++i)
                    {
                        nextScene.RootCanvases[i].sortingOrder = this._canvasOrderArranger.InitialOrder;
                    }
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

#if UNITY_EDITOR
        public async UniTask ActivateInitialSceneOnLaunchAsync()
        {
            var currentScene = SceneManager.GetActiveScene();
            var args = DefaultSceneArgsFactory.CreateDefaultSceneArgs(currentScene.name);
            var tokenSource = new CancellationTokenSource();

            await UniTask.DelayFrame(1);
            for(var i = 0; i < SceneManager.sceneCount; ++i)
            {
                // 今のシーン
                if (currentScene == SceneManager.GetSceneAt(i))
                {
                    if (args.SceneStyle != SceneStyle.None)
                    {
                        new NavigationFailureException("最初のアクティブシーンにサブシーンまたはポップアップシーンを指定することはできません", args);
                    }

                    continue;
                }

                await SceneManager.UnloadSceneAsync(i);
            }

            var subSceneTransition = await this.NavigateSubScenesCoreAsync(args, tokenSource.Token, new Progress<float>());
            if (tokenSource.Token.IsCancellationRequested)
            {
                throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), tokenSource.Token);
            }
            await subSceneTransition.OnEnteredAsync(tokenSource.Token);

            var activationResult = this.Activate(args, NavigationOption.Push);
            // ここでダメな場合は既にActivateAsyncでエラーを吐いてるハズ
            if (activationResult == null || activationResult.NextScene == null)
            {
                return;
            }

            this._scenesByName[args.SceneName] = activationResult.NextScene;

            // ロード時にCanvasの調整をする
            if (this._canvasCustomizer != null)
            {
                this._canvasCustomizer.Customize(activationResult.NextScene.RootCanvases);
            }

            // シーンをスタックに積む
            this._navigateHistoryStack.Push(new NavigationStackElement() { SceneName = args.SceneName, TransitionMode = activationResult.TransitionMode });

            var dummyProgress = new Progress<float>();

            // シーンをリセットする
            await activationResult.NextScene.ResetAsync(args, activationResult.TransitionMode, dummyProgress);
            if (activationResult.NextScene.SceneLifeCancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), activationResult.NextScene.SceneLifeCancellationToken);
            }

            // シーンを初期化する
            activationResult.NextScene.Initialize();

            // シーンに入る
            await activationResult.NextScene.EnterAsync(activationResult.TransitionMode, dummyProgress);
            if (activationResult.NextScene.SceneLifeCancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), activationResult.NextScene.SceneLifeCancellationToken);
            }

            // 新規シーンに入ったら外部の遷移処理を呼ぶ
            for (var i = 0; i < activationResult.NextScene.RootCanvases.Count; ++i)
            {
                activationResult.NextScene.RootCanvases[i].enabled = false;
            }
            activationResult.NextScene.RootObject.SetActive(true);

            await this._afterTransition.OnEnteredAsync(activationResult, activationResult.NextScene.SceneLifeCancellationToken, new Progress<float>());
            if (activationResult.NextScene.SceneLifeCancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("遷移処理がキャンセルされました", new NavigationFailureException("遷移処理がキャンセルされました", args), activationResult.NextScene.SceneLifeCancellationToken);
            }

            for (var i = 0; i < activationResult.NextScene.RootCanvases.Count; ++i)
            {
                activationResult.NextScene.RootCanvases[i].enabled = true;
            }
        }
#endif

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

            public static NavigationLock Acquire(ISceneArgs args)
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

        private class SubSceneTransitions
        {
            public List<Func<CancellationToken, IProgress<float>, UniTask>> OnEnteredTasks { get; }
            public List<Func<CancellationToken, IProgress<float>, UniTask>> OnLeftTasks { get; }

            public SubSceneTransitions()
            {
                this.OnEnteredTasks = new List<Func<CancellationToken, IProgress<float>, UniTask>>();
                this.OnLeftTasks = new List<Func<CancellationToken, IProgress<float>, UniTask>>();
            }

            public UniTask OnEnteredAsync(CancellationToken token)
            {
                return UniTask.WhenAll(this.OnEnteredTasks.Select(x => x(token, new Progress<float>())));
            }

            public UniTask OnLeftAsync(CancellationToken token)
            {
                return UniTask.WhenAll(this.OnLeftTasks.Select(x => x(token, new Progress<float>())));
            }

            internal void OnEnteredAsync(object token)
            {
                throw new NotImplementedException();
            }
        }
    }
}