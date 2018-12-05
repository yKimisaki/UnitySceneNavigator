using System.Collections.Generic;
using Tonari.Unity.SceneNavigator;

namespace Tonari.Unity.NavigationSystemSample
{
    public class UIContent4SceneArgs : SceneArgs<UIContent4Scene>
    {
        public override IReadOnlyList<ISceneArgs> SubScenes => new[] { new UIBaseSubSceneArgs(UIContentCategory.Content4), };
    }

    public class UIContent4Scene : SceneBase
    {
        public override void Initialize()
        {
        }
    }
}
