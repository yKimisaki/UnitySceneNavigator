using Tonari.Unity.SceneNavigator;
using UniRx.Async;

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
        public override void Initialize()
        {
        }
    }
}
