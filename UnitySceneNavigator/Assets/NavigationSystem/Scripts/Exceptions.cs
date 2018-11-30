using System;

namespace Tonari.Unity.SceneNavigator
{
    public class NavigationFailureException : Exception
    {
        public NavigationFailureException(string message, ISceneArgs args) : base(message + ": " + args?.SceneName) { }
    }
}
