
namespace Tonari.Unity.SceneNavigator
{
    public interface INavigationContext
    {
        INavigatableScene NextScene { get; }
        INavigatableScene PreviousScene { get; }
        TransitionMode TransitionMode { get; }
    }
}