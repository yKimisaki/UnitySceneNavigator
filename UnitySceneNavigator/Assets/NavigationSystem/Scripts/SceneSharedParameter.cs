using System;
using System.Collections.Generic;
using System.Threading;

namespace Tonari.Unity.SceneNavigator
{
    public sealed class SceneSharedParameter
    {
        public bool CanInput { get; set; }
        public ICollection<IDisposable> Subscriptions { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public SceneSharedParameter(ICollection<IDisposable> subscriptions, CancellationTokenSource cancellationTokenSource)
        {
            this.Subscriptions = subscriptions;
            this.CancellationTokenSource = cancellationTokenSource;
        }
    }
}
