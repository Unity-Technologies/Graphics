using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    public class SettingsDisableScope : IDisposable
    {
        bool enable;

        public SettingsDisableScope(bool enable)
        {
            this.enable = enable;
            if (!enable)
                GUI.enabled = false;
        }

        public void Dispose()
        {
            if (!enable)
                GUI.enabled = true;
        }
    }
}
