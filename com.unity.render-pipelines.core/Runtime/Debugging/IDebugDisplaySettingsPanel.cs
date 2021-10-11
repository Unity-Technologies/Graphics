using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Debug UI panel
    /// </summary>
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

    /// <summary>
    /// Debug UI panel disposable
    /// </summary>
    public interface IDebugDisplaySettingsPanelDisposable : IDebugDisplaySettingsPanel, IDisposable
    {
    }
}
