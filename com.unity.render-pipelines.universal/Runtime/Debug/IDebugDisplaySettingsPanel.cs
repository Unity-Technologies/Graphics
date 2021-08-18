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

        /// <summary>
        /// Flags to be applied to the top-level panel.
        /// </summary>
        DebugUI.Flags Flags { get; }
    }

    public interface IDebugDisplaySettingsPanelDisposable : IDebugDisplaySettingsPanel, IDisposable
    {
    }
}
