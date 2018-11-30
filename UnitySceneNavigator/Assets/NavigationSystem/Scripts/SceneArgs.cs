
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

    public abstract class SubSceneArgs<T> : ISceneArgs where T : SubSceneBase
    {
        string ISceneArgs.SceneName => nameof(T);
    }
}
