using System.Collections.Generic;
using Tonari.Unity.SceneNavigator;

namespace Tonari.Unity.NavigationSystemSample
{
    public class UIContent5SceneArgs : SceneArgs<UIContent5Scene>
    {
        public override IReadOnlyList<ISceneArgs> SubScenes => new[] { new UIBaseSubSceneArgs(UIContentCategory.Content5), };
    }

    public class UIContent5Scene : SceneBase
    {
        public override void Initialize()
        {
        }
    }
}
