using System;
using System.Threading;
using UniRx;
using UniRx.Async;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tonari.Unity.SceneNavigator
{
    public static class UIExtension
    {
        public static IDisposable OnClick(this Button button, SceneSharedParameter sharedParameter, Func<UniTask> call)
        {
            UnityAction wappedCall = async () =>
            {
                try
                {
                    if (sharedParameter.CanInput)
                    {
                        return;
                    }

                    sharedParameter.CanInput = true;

                    await call();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sharedParameter.CanInput = false;
                }
            };

            button.onClick.AddListener(wappedCall);

            return Disposable.Create(() => button.onClick.RemoveListener(wappedCall))
                .AddTo(sharedParameter.Subscriptions);
        }
    }
}
