using System;
using Tonari.Unity.SceneNavigator;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tonari.Unity.NavigationSystemSample
{
    public class InitialScene : MonoBehaviour
    {
        public async UniTask Start()
        {
            try
            {
                // UI用カメラの作成
                var cameraObject = new GameObject("UICamera");
                var camera = cameraObject.AddComponent<Camera>();
                DontDestroyOnLoad(cameraObject);
                camera.orthographic = true;
                camera.orthographicSize = 5;
                camera.cullingMask = 1 << 5;

                // EventSystemの作成
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(eventSystem);

                // CanvasCustomizerの作成
                var canvasCustomizer = new CanvasCustomizer(camera);

                // 遷移アニメーションの作成
                var animation = new TransitionAnimator();

                // Navigatorの作成
                var navigator = new Navigator(null, canvasCustomizer, null, animation);

                // 全部終わったら最初のシーンに移動
                await navigator.NavigateAsync(new UITestSceneArgs());

                // 自分を殺す
                Destroy(this.gameObject);
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                    UnityEngine.Debug.LogException(e);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}