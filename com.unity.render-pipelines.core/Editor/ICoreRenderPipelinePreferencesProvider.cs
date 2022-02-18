using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Interface to extend to provide UI
    /// </summary>
    public interface ICoreRenderPipelinePreferencesProvider
    {
        /// <summary>
        /// The list of keywords for user search
        /// </summary>
        List<string> keywords { get; }

        /// <summary>
        /// The header of the panel
        /// </summary>
        GUIContent header { get; }

        /// <summary>
        /// Renders the Preferences UI for this provider
        /// </summary>
        void PreferenceGUI();
    }
}
