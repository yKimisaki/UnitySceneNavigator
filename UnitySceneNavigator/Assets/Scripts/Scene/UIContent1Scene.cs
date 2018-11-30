using System.Collections.Generic;
using Tonari.Unity.SceneNavigator;

namespace Tonari.Unity.NavigationSystemSample
{
    public class UIContentSceneArgs : SceneArgs<UIContent1Scene>
    {
        public override IReadOnlyList<ISceneArgs> SubScenes => new[] { new UIBaseSubSceneArgs(), };
    }

    public class UIContent1Scene : SceneBase
    {
        public override void Initialize()
        {
        }
    }
}
