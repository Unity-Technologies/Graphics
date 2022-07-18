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
        /// The list of keywords
        /// </summary>
        List<string> keywords { get; }
        /// <summary>
        /// The header of the preferences
        /// </summary>
        GUIContent header { get; }
        /// <summary>
        /// The main method to render the user interface
        /// </summary>
        void PreferenceGUI();
    }
}
