using System;
using UniRx.Async;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tonari.Unity.SceneNavigator
{
    public static class UIExtension
    {
        public static void OnClick(this Button button, SceneSharedParameter sharedParameter, Func<UniTask> call)
        {
            UnityAction wappedCall = async () =>
            {
                try
                {
                    if (sharedParameter.InputLock)
                    {
                        return;
                    }

                    sharedParameter.InputLock = true;

                    await call();

                    sharedParameter.InputLock = false;
                }
                catch
                {
                    throw;
                }
            };

            button.onClick.AddListener(wappedCall);
        }
    }
}
