using System.Collections.Generic;
using UnityEngine;

namespace Tonari.Unity.SceneNavigator
{
    public class DefaultCanvasOrderArranger : ICanvasOrderArranger
    {
        public int InitialOrder
        {
            get
            {
                return 100;
            }
        }

        public void ArrangeOrder(IReadOnlyList<Canvas> canvas, NavigationOption option)
        {
        }
    }
}
