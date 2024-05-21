using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using RuntimeSRPPreferences = UnityEngine.Rendering.CoreRenderPipelinePreferences;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Preferences for Decals
    /// </summary>
    public class DecalPreferences : ICoreRenderPipelinePreferencesProvider
    {
        static readonly Color k_DecalGizmoColorBase = new Color(1, 1, 1, 8f / 255);
        static Func<Color>    GetColorPrefDecalGizmoColor;

        /// <summary>
        /// Obtains the color of the decal gizmo
        /// </summary>
        public static Color   decalGizmoColor => GetColorPrefDecalGizmoColor();

        static DecalPreferences()
        {
            GetColorPrefDecalGizmoColor = RuntimeSRPPreferences.RegisterPreferenceColor("Scene/Decal", k_DecalGizmoColorBase);
        }

        static List<string> s_SearchKeywords = new() { "Decals" };

        /// <summary>
        /// The list of keywords for user search
        /// </summary>
        public List<string> keywords => s_SearchKeywords;

        /// <summary>
        /// The header of the panel
        /// </summary>
        public GUIContent header => null; // For now this is only a data preference without UI

        /// <summary>
        /// Renders the Preferences UI for this provider
        /// </summary>
        public void PreferenceGUI()
        {
            // For now this is only a data preference without UI
        }
    }
}
