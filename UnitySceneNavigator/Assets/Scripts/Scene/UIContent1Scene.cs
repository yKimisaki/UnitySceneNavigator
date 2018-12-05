using System.Collections.Generic;
using Tonari.Unity.SceneNavigator;

namespace Tonari.Unity.NavigationSystemSample
{
    public class UIContent1SceneArgs : SceneArgs<UIContent1Scene>
    {
        public override IReadOnlyList<ISceneArgs> SubScenes => new[] { new UIBaseSubSceneArgs(UIContentCategory.Content1), };
    }

    public class UIContent1Scene : SceneBase
    {
        public override void Initialize()
        {
        }
    }
}
