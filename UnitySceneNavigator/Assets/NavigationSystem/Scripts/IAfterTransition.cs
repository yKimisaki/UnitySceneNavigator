using System;
using System.Threading;
using UniRx.Async;

namespace Tonari.Unity.SceneNavigator
{
    public interface IAfterTransition
    {
        UniTask OnEnteredAsync(INavigationContext context, CancellationToken token, IProgress<float> progress);
        UniTask OnLeftAsync(INavigationContext context, CancellationToken token, IProgress<float> progress);
    }
}