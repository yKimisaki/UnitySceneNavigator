using UniRx.Async;

namespace Tonari.Unity.SceneNavigator
{
    public interface IAfterTransition
    {
        UniTask OnAfterEnterAsync(INavigationContext context);
        UniTask OnAfterLeaveAsync(INavigationContext context);
    }
}