using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Core
{
    /// <summary>
    /// Editor Preferences for HDRP
    /// </summary>
    [DisplayInfo(name = "High Definition Render Pipeline", order = 200)]
    public class HDRenderPipelinePreferencesProvider : ICoreRenderPipelinePreferencesProvider
    {
        class Styles
        {
            public static readonly GUIContent matcapLabel = EditorGUIUtility.TrTextContent("MatCap Mode Default Values");
            public static readonly GUIContent matcapViewMixAlbedoLabel = EditorGUIUtility.TrTextContent("Mix Albedo", "Enable to make HDRP mix the albedo of the Material with its material capture.");
            public static readonly GUIContent matcapViewScaleLabel = EditorGUIUtility.TrTextContent("Intensity Scale", "Set the intensity of the material capture. This increases the brightness of the Scene. This is useful if the albedo darkens the Scene considerably.");
        }

        static List<string> s_SearchKeywords = new() { "MatCap Mode", "Intensity scale", "Mix Albedo" };

        /// <summary>
        /// Keyworks for the preferences
        /// </summary>
        public List<string> keywords => s_SearchKeywords;

        /// <summary>
        /// UI for the preferences.
        /// </summary>
        public void PreferenceGUI()
        {
            EditorGUILayout.LabelField(Styles.matcapLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            var matCapMode = HDRenderPipelinePreferences.matCapMode;
            matCapMode.mixAlbedo.value = EditorGUILayout.Toggle(Styles.matcapViewMixAlbedoLabel, matCapMode.mixAlbedo.value);
            if (matCapMode.mixAlbedo.value)
                matCapMode.viewScale.value = EditorGUILayout.FloatField(Styles.matcapViewScaleLabel, matCapMode.viewScale.value);
            EditorGUI.indentLevel--;
        }
    }
}
