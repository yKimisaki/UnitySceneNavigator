using System;
using System.Collections.Generic;

namespace Tonari.Unity.SceneNavigator
{
    public class NavigationInternalProgressGroup : IDisposable
    {
        private IProgress<float> _outerProgress;
        private NavigationInternalProgress[] _progresses;

        public NavigationInternalProgressGroup(IProgress<float> outerProgress, int count)
        {
            this._outerProgress = outerProgress;

            this._progresses = new NavigationInternalProgress[count];
            var margin = 1f / count;
            for (var i = 0; i < count; ++i)
            {
                var initialValue = (float)i / count;
                this._progresses[i] = new NavigationInternalProgress(this._outerProgress, initialValue, margin);
            }
        }

        public void Dispose()
        {
            this._outerProgress.Report(1f);
        }

        public IProgress<float> this[int i]
        {
            get
            {
                return this._progresses[i];
            }
        }

        private class NavigationInternalProgress : IProgress<float>
        {
            private IProgress<float> _outerProgress;

            private float _initialValue;
            private float _margin;

            public NavigationInternalProgress(IProgress<float> outerProgress, float initialValue, float margin)
            {
                this._outerProgress = outerProgress;

                this._initialValue = initialValue;
                this._margin = margin;
            }

            public void Report(float value)
            {
                this._outerProgress.Report(value * this._margin + this._initialValue);
            }
        }
    }
}
