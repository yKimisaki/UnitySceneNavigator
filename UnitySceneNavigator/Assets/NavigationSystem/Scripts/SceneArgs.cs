
using System;
using System.Collections.Generic;

namespace Tonari.Unity.SceneNavigator
{
    public interface ISceneArgs
    {
        string SceneName { get; }
    }

    public abstract class SceneArgs<T> : ISceneArgs where T : SceneBase
    {
        public virtual IReadOnlyList<ISceneArgs> SubScenes => Array.Empty<ISceneArgs>();

        string ISceneArgs.SceneName => nameof(T);
    }

    public abstract class SubSceneArgs<T> : ISceneArgs where T : SubSceneBase
    {
        string ISceneArgs.SceneName => nameof(T);
    }

    public abstract class PopupSceneArgs<T> : ISceneArgs where T : PopupSceneBase
    {
        string ISceneArgs.SceneName => nameof(T);
    }
}
