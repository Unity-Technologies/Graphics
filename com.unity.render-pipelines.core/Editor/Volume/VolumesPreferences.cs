using System;
using System.Collections.Generic;
using UnityEngine;
using RuntimeSRPPreferences = UnityEngine.Rendering.CoreRenderPipelinePreferences;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Preferences for Volumes
    /// </summary>
    public class VolumesPreferences : ICoreRenderPipelinePreferencesProvider
    {
        static class Keys
        {
            internal const string volumeGizmosVisibility = "General.VolumeGizmosVisibility";
        }

        public enum VolumeGizmoVisibility
        {
            Wireframe = 0,
            Solid = 1,
            Everything
        }

        class Styles
        {
            public static readonly GUIContent volumeGizmosVisibilityLabel = EditorGUIUtility.TrTextContent("Gizmo Visibility", "Specifies how Gizmos for Volumes are being rendered");
        }

        static VolumeGizmoVisibility s_VolumeGizmosVisibilityOption = VolumeGizmoVisibility.Solid;

        static VolumesPreferences()
        {
            GetColorPrefVolumeGizmoColor = RuntimeSRPPreferences.RegisterPreferenceColor("Scene/Volume Gizmo", s_VolumeGizmoColorDefault);
            s_VolumeGizmosVisibilityOption = (VolumeGizmoVisibility)EditorPrefs.GetInt(Keys.volumeGizmosVisibility);
        }

        public static VolumeGizmoVisibility volumeGizmosVisibilityOption
        {
            get => s_VolumeGizmosVisibilityOption;
            set
            {
                s_VolumeGizmosVisibilityOption = value;
                EditorPrefs.SetInt(Keys.volumeGizmosVisibility, (int)s_VolumeGizmosVisibilityOption);
            }
        }

        static Color s_VolumeGizmoColorDefault = new Color(0.2f, 0.8f, 0.1f, 0.125f);
        private static Func<Color> GetColorPrefVolumeGizmoColor;

        public static Color volumeGizmoColor => GetColorPrefVolumeGizmoColor();

        static List<string> s_SearchKeywords = new() { "Gizmo", "Wireframe", "Visibility" };
        public List<string> keywords => s_SearchKeywords;

        public GUIContent header { get; } = EditorGUIUtility.TrTextContent("Volumes");

        public int height => 17;

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
