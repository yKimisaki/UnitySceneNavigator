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
        public Button PopupButton;
        public Button CloseButton;

        public override void Initialize()
        {
            this.PopupButton.OnClick(this.SceneShared, () => this.Navigator.NavigateAsPopupAsync<int>(new UIPopupTestSceneArgs()));
            this.CloseButton.OnClick(this.SceneShared, () => this.Navigator.NavigateBackAsync(50));
        }
    }
}
