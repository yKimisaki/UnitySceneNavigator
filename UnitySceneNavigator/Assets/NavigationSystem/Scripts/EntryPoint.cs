using Tonari.Unity.NavigationSystemSample;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tonari.Unity.SceneNavigator
{
    public static class EntryPoint
    {
        public static async UniTask MainAsync()
        {
            // UI用カメラの作成
            var cameraObject = new GameObject("UICamera");
            var camera = cameraObject.AddComponent<Camera>();
            Object.DontDestroyOnLoad(cameraObject);
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.cullingMask = 1 << 5;

            // EventSystemの作成
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            Object.DontDestroyOnLoad(eventSystem);

            // CanvasCustomizerの作成
            var canvasCustomizer = new CanvasCustomizer(camera);

            // 遷移アニメーションの作成
            var transitionAnimator = new TransitionAnimator();

            // Navigatorの作成
            var navigator = new Navigator(null, canvasCustomizer, null, transitionAnimator);

            await navigator.ActivateInitialSceneOnLaunchAsync();
        }
    }
}
