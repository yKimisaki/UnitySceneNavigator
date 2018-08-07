using UniRx.Async;

namespace Tonari.Unity.SceneNavigator
{
    public interface IAfterTransition
    {
        UniTask OnEnteredAsync(INavigationContext context);
        UniTask OnLeftAsync(INavigationContext context);
    }
}