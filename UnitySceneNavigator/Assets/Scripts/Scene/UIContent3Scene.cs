using System.Collections.Generic;
using Tonari.Unity.SceneNavigator;

namespace Tonari.Unity.NavigationSystemSample
{
    public class UIContent3SceneArgs : SceneArgs<UIContent3Scene>
    {
        public override IReadOnlyList<ISceneArgs> SubScenes => new[] { new UIBaseSubSceneArgs(UIContentCategory.Content3), };
    }

    public class UIContent3Scene : SceneBase
    {
        public override void Initialize()
        {
        }
    }
}
