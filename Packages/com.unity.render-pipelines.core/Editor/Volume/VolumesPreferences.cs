using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using RuntimeSRPPreferences = UnityEngine.Rendering.CoreRenderPipelinePreferences;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Preferences for Volumes
    /// </summary>
    [DisplayInfo(name = "Volumes", order = 50)]
    public class VolumesPreferences : ICoreRenderPipelinePreferencesProvider
    {
        static class Keys
        {
            internal const string volumeGizmosVisibility = "General.VolumeGizmosVisibility";
        }

        /// <summary>
        /// Specifies how the gizmos for volumes are being drawn
        /// </summary>
        [Flags]
        public enum VolumeGizmoVisibility
        {
            /// <summary> Wireframe </summary>
            Wireframe = 1,
            /// <summary> Solid </summary>
            Solid = 2,
            /// <summary> Solid with Wireframe </summary>
            Everything = Wireframe | Solid
        }

        class Styles
        {
            public static readonly GUIContent volumeGizmosVisibilityLabel = EditorGUIUtility.TrTextContent("Gizmo Visibility", "Specifies how Gizmos for Volumes are being rendered");
        }

        static VolumeGizmoVisibility s_VolumeGizmosVisibilityOption = VolumeGizmoVisibility.Solid;

        static VolumesPreferences()
        {
            GetColorPrefVolumeGizmoColor = RuntimeSRPPreferences.RegisterPreferenceColor("Scene/Volume Gizmo", s_VolumeGizmoColorDefault);
            if (EditorPrefs.HasKey(Keys.volumeGizmosVisibility))
                s_VolumeGizmosVisibilityOption = (VolumeGizmoVisibility)EditorPrefs.GetInt(Keys.volumeGizmosVisibility);
        }

        /// <summary>
        /// Returns the current <see cref="VolumeGizmoVisibility"/>
        /// </summary>
        public static VolumeGizmoVisibility volumeGizmosVisibilityOption
        {
            get => s_VolumeGizmosVisibilityOption;
            set
            {
                s_VolumeGizmosVisibilityOption = value;
                EditorPrefs.SetInt(Keys.volumeGizmosVisibility, (int)s_VolumeGizmosVisibilityOption);
            }
        }

        /// <summary>
        /// Returns if the Volume Gizmos should render the wireframe edges
        /// </summary>
        public static bool drawWireFrame => (volumeGizmosVisibilityOption & VolumeGizmoVisibility.Wireframe) == VolumeGizmoVisibility.Wireframe;

        /// <summary>
        /// Returns if the Volume Gizmos should render the solid faces
        /// </summary>
        public static bool drawSolid => (volumeGizmosVisibilityOption & VolumeGizmoVisibility.Solid) == VolumeGizmoVisibility.Solid;

        static Color s_VolumeGizmoColorDefault = new Color(0.2f, 0.8f, 0.1f, 0.125f);
        private static Func<Color> GetColorPrefVolumeGizmoColor;

        /// <summary>
        /// Returns the user defined color for rendering volume gizmos
        /// </summary>
        public static Color volumeGizmoColor => GetColorPrefVolumeGizmoColor();

        static List<string> s_SearchKeywords = new() { "Gizmo", "Wireframe", "Visibility" };
        /// <summary>
        /// The list of keywords for user search
        /// </summary>
        public List<string> keywords => s_SearchKeywords;

        /// <summary>
        /// Renders the Preferences UI for this provider
        /// </summary>
        public void PreferenceGUI()
        {
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUILayout.EnumPopup(Styles.volumeGizmosVisibilityLabel, volumeGizmosVisibilityOption);
            if (EditorGUI.EndChangeCheck())
            {
                volumeGizmosVisibilityOption = (VolumeGizmoVisibility)newValue;
            }
        }
    }
}
