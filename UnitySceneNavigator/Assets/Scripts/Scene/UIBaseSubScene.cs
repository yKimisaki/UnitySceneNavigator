using Tonari.Unity.SceneNavigator;
using UniRx.Async;

namespace Tonari.Unity.NavigationSystemSample
{
    public class UIBaseSubSceneArgs : SubSceneArgs<UIBaseSubScene> { }

    public class UIBaseSubScene : SceneBase
    {
        public override void Initialize()
        {
        }

        public override UniTask ResetAsync(ISceneArgs args, TransitionMode mode)
        {
            return base.ResetAsync(args, mode);
        }
    }
}
