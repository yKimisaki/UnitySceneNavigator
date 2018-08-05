using Tonari.Unity.SceneNavigator;
using UnityEngine.UI;

namespace Tonari.Unity.NavigationSystemSample
{
    public class UIPopupTestSceneArgs : SceneArgs
    {
        public UIPopupTestSceneArgs() : base("UIPopupTest") { }
    }

    public class UIPopupTestScene : SceneBase
    {
        public Button Button;

        public override void Initialize()
        {
            this.Button.OnClick(this.SceneShared, () => this.Navigator.NavigateBackAsync(50));
        }
    }
}
