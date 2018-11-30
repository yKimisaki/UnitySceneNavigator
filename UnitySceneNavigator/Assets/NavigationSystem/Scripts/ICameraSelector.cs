using System.Collections.Generic;
using UnityEngine;

namespace Tonari.Unity.SceneNavigator
{
    public interface ICanvasCustomizer
    {
        void Customize(IReadOnlyList<Canvas> canvases);
    }
}
