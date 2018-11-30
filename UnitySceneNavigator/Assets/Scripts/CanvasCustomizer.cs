using System.Collections.Generic;
using Tonari.Unity.SceneNavigator;
using UnityEngine;
using UnityEngine.UI;

namespace Tonari.Unity.NavigationSystemSample
{
    public class CanvasCustomizer : ICanvasCustomizer
    {
        private Camera _camera;

        public CanvasCustomizer(Camera camera)
        {
            this._camera = camera;
        }

        public void Customize(IReadOnlyList<Canvas> canvases)
        {
            for (var i = 0; i < canvases.Count; ++i)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = this._camera;

                var graphicRaycaster = canvases[i].GetComponent<GraphicRaycaster>();
                graphicRaycaster.ignoreReversedGraphics = true;
                graphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            }
        }
    }
}
