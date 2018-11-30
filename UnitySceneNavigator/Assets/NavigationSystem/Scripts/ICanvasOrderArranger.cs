using System.Collections.Generic;
using UnityEngine;

namespace Tonari.Unity.SceneNavigator
{
    public interface ICanvasOrderArranger
    {
        int InitialOrder { get; }
        void ArrangeOrder(IReadOnlyList<Canvas> canvas, NavigationOption option);
    }
}
