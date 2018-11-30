using System.Collections.Generic;
using Tonari.Unity.SceneNavigator;

namespace Tonari.Unity.NavigationSystemSample
{
    public class UIContent1Scene : SceneBase
    {
        public override IReadOnlyList<ISubSceneArgs> SubScenes => new[] { new UIBaseSubSceneArgs(), };

        public override void Initialize()
        {
        }
    }
}
