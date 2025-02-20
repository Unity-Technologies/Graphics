namespace UnityEngine.Rendering
{
    /// <summary>
    /// Debug UI panel interface
    /// </summary>
    public interface IDebugDisplaySettingsData : IDebugDisplaySettingsQuery
    {
        /// <summary>
        /// Creates the debug UI panel needed for these debug settings.
        /// </summary>
        /// <returns>The debug UI panel created.</returns>
        IDebugDisplaySettingsPanelDisposable CreatePanel();

        /// <summary>
        /// Resets the values of the settings data
        /// </summary>
        void Reset() { }
    }
}
