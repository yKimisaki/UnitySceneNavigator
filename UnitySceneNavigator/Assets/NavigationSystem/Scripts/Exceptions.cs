using System;

namespace Tonari.Unity.SceneNavigator
{
    public class NavigationFailureException : Exception
    {
        public NavigationFailureException(string message, SceneArgs args) : base(message + ": " + args?.SceneName) { }
    }
}
