
namespace Tonari.Unity.SceneNavigator
{
    public interface ISceneArgs
    {
        string SceneName { get; }
    }

    public abstract class SceneArgs<T> : ISceneArgs where T : SceneBase
    {
        string ISceneArgs.SceneName => nameof(T);
    }

    public interface ISubSceneArgs
    {
        string SceneName { get; }
    }

    public abstract class SubSceneArgs<T> : ISubSceneArgs where T : SceneBase
    {
        string ISubSceneArgs.SceneName => nameof(T);
    }
}
