using UnityEngine;

namespace Tonari.Unity.SceneNavigator
{
    public interface ILoadingDisplay
    {
        GameObject CreateInstance();

        void Show();
        void Hide();
    }
}
