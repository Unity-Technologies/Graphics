using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Interface for determining what kind of debug settings are currently active.
    /// </summary>
    public interface IDebugDisplaySettingsQuery
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
        /// Checks whether lighting is active for these settings.
        /// </summary>
        bool IsLightingActive { get; }

        /// <summary>
        /// Attempts to get the color used to clear the screen for this debug setting.
        /// </summary>
        /// <param name="color">A reference to the screen clear color to use.</param>
        /// <returns>"true" if we updated the color, "false" if we didn't change anything.</returns>
        bool TryGetScreenClearColor(ref Color color);
    }
}
