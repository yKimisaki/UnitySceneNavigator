using Tonari.Unity.SceneNavigator;
using UniRx;
using UniRx.Async;
using UnityEngine.UI;

namespace Tonari.Unity.NavigationSystemSample
{
    public enum UIContentCategory
    {
        Content1,
        Content2,
        Content3,
        Content4,
        Content5,
    }

    public class UIBaseSubSceneArgs : SubSceneArgs<UIBaseSubScene>
    {
        public UIContentCategory Category { get; }

        public UIBaseSubSceneArgs(UIContentCategory category)
        {
            this.Category = category;
        }
    }

    public class UIBaseSubScene : SceneBase
    {
        public Button Content1Button;
        public Button Content2Button;
        public Button Content3Button;
        public Button Content4Button;
        public Button Content5Button;

        private UIBaseSubSceneModel _model;

        public override void Initialize()
        {
            this._model = new UIBaseSubSceneModel(this.Navigator);

            this.Content1Button.OnClick(this.SceneShared, this._model.NavigateToContent1Async).AddTo(this);
        }
    }

    public class UIBaseSubSceneModel
    {
        private Navigator _navigator;

        public UIBaseSubSceneModel(Navigator navigator)
        {
            this._navigator = navigator;
        }

        public async UniTask NavigateToContent1Async()
        {
            await this._navigator.NavigateNextAsync<Unit>(new UIContent1SceneArgs());
        }
    }
}
