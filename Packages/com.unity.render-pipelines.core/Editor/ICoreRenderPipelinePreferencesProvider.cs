using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

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
        GUIContent header
        {
            get
            {
                var type = GetType();
                var displayTypeInfoAttribute = type.GetCustomAttribute<DisplayInfoAttribute>();
                return EditorGUIUtility.TrTextContent(displayTypeInfoAttribute != null ? displayTypeInfoAttribute.name : type.Name);
            }
        }

        /// <summary>
        /// Renders the Preferences UI for this provider
        /// </summary>
        void PreferenceGUI();
    }
}
