using System;

namespace UnityEngine.Rendering.Universal
{
    public interface IDebugDisplaySettingsPanel
    {
        /// <summary>
        /// The name used when displaying this panel.
        /// </summary>
        string PanelName { get; }

        /// <summary>
        /// Widgets used by this panel.
        /// </summary>
        DebugUI.Widget[] Widgets { get; }
    }

    public interface IDebugDisplaySettingsPanelDisposable : IDebugDisplaySettingsPanel, IDisposable
    {
    }
}
