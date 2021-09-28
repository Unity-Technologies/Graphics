namespace UnityEngine.Rendering
{
    /// <summary>
    /// Debug UI panel interface
    /// </summary>
    public interface IDebugDisplaySettingsData
    {
        /// <summary>
        /// Creates the debug UI panel needed for these debug settings.
        /// </summary>
        /// <returns>The debug UI panel created.</returns>
        IDebugDisplaySettingsPanelDisposable CreatePanel();
    }
}
