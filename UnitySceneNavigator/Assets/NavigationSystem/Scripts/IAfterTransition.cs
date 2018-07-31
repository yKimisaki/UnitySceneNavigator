using UniRx.Async;

namespace Tonari.Unity.SceneNavigator
{
    public interface IAfterTransition
    {
        UniTask OnNavigatedAsync(INavigationContext context);
    }
}