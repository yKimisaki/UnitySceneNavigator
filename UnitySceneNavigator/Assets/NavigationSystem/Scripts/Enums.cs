using System;

namespace Tonari.Unity.SceneNavigator
{
    public enum SceneStyle
    {
        None = 0,

        Sub = 1,
        Popup = 2,
    }

    [Flags]
    public enum NavigationOption
    {
        None = 0,

        Push = 1 << 1,
        Pop = 1 << 2,

        Sub = 1 << 10,

        Override = 1 << 31,

        Popup = Push | Override,
    }

    [Flags]
    public enum TransitionMode
    {
        None = 0,

        New = 1 << 1,
        KeepCurrent = 1 << 2,
        Back = 1 << 3,
    }
}
