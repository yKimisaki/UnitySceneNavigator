using System.Collections.Generic;
using Tonari.Unity.SceneNavigator;

namespace Tonari.Unity.NavigationSystemSample
{
    public class UIContent2SceneArgs : SceneArgs<UIContent2Scene>
    {
        public override IReadOnlyList<ISceneArgs> SubScenes => new[] { new UIBaseSubSceneArgs(UIContentCategory.Content2), };
    }

    public class UIContent2Scene : SceneBase
    {
        public override void Initialize()
        {
        }
    }
}
