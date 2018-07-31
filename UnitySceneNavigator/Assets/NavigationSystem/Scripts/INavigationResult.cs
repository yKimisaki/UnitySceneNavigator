
namespace Tonari.Unity.SceneNavigator
{
    public interface INavigationResult
    {
        INavigatableScene NextScene { get; }
        INavigatableScene PreviousScene { get; }
        TransitionMode TransitionMode { get; }
    }
}