using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        string ISceneArgs.SceneName => typeof(T).Name;

        SceneStyle ISceneArgs.SceneStyle => SceneStyle.None;
    }

    public abstract class SubSceneArgs<T> : ISceneArgs where T : INavigatableScene
    {
        // サブシーンにサブシーンは指定できない
        IReadOnlyList<ISceneArgs> ISceneArgs.SubScenes => Array.Empty<ISceneArgs>();

        string ISceneArgs.SceneName => typeof(T).Name;

        SceneStyle ISceneArgs.SceneStyle => SceneStyle.Sub;
    }

    public abstract class PopupSceneArgs<T> : ISceneArgs where T : INavigatableScene
    {
        // ポップアップにサブシーンは指定できない
        IReadOnlyList<ISceneArgs> ISceneArgs.SubScenes => Array.Empty<ISceneArgs>();

        string ISceneArgs.SceneName => typeof(T).Name;

        SceneStyle ISceneArgs.SceneStyle => SceneStyle.Popup;
    }

#if UNITY_EDITOR
    public sealed class DefaultSceneArgsFactory 
    {
        public static ISceneArgs CreateDefaultSceneArgs(string sceneName)
        {
            var sceneArgsType = Assembly.GetExecutingAssembly()
                .GetTypes()
                .FirstOrDefault(x => x.Name == sceneName + "Args");
            // Type.GetType("Tonari.Unity.NavigationSystemSample." + sceneName + "Args");
            if (sceneArgsType == null)
            {
                throw new InvalidOperationException($"起動用のシーン引数: {sceneName}Argsが見つかりませんでした");
            }

            return (ISceneArgs)Activator.CreateInstance(sceneArgsType);
        }
    }
#endif
}