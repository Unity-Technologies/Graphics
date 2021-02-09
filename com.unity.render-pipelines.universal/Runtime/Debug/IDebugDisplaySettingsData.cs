
namespace UnityEditor.Rendering
{
    public interface IDebugDisplaySettingsData
    {
        /// <summary>
        /// Checks whether ANY of the debug settings are currently active.
        /// </summary>
        bool AreAnySettingsActive { get; }

        /// <summary>
        /// Checks whether the current state of these settings allows post-processing.
        /// </summary>
        bool IsPostProcessingAllowed { get; }

        /// <summary>
        /// Creates the debug UI panel needed for these debug settings.
        /// </summary>
        /// <returns>The debug UI panel created.</returns>
        IDebugDisplaySettingsPanelDisposable CreatePanel();
    }
}
