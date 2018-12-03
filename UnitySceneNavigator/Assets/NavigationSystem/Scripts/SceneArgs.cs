
using System;
using System.Collections.Generic;

namespace Tonari.Unity.SceneNavigator
{
    public interface ISceneArgs
    {
        IReadOnlyList<ISceneArgs> SubScenes { get; }

        string SceneName { get; }

        SceneStyle SceneStyle { get; }
    }

    public abstract class SceneArgs<T> : ISceneArgs where T : INavigatableScene
    {
        public virtual IReadOnlyList<ISceneArgs> SubScenes => Array.Empty<ISceneArgs>();

        string ISceneArgs.SceneName => nameof(T);

        SceneStyle ISceneArgs.SceneStyle => SceneStyle.None;
    }

    public abstract class SubSceneArgs<T> : ISceneArgs where T : INavigatableScene
    {
        // サブシーンにサブシーンは指定できない
        IReadOnlyList<ISceneArgs> ISceneArgs.SubScenes => Array.Empty<ISceneArgs>();

        string ISceneArgs.SceneName => nameof(T);

        SceneStyle ISceneArgs.SceneStyle => SceneStyle.Sub;
    }

    public abstract class PopupSceneArgs<T> : ISceneArgs where T : INavigatableScene
    {
        // ポップアップにサブシーンは指定できない
        IReadOnlyList<ISceneArgs> ISceneArgs.SubScenes => Array.Empty<ISceneArgs>();

        string ISceneArgs.SceneName => nameof(T);

        SceneStyle ISceneArgs.SceneStyle => SceneStyle.Popup;
    }
}