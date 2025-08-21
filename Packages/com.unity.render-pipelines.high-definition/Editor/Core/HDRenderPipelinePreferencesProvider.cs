using System.Collections.Generic;
using System.Reflection;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Core
{
    static class PackageManagerHelper
    {
        public static void OpenPackageManagerToPackage(string packageName)
        {
            var packageManagerUI = typeof(Editor).Assembly
                .GetType("UnityEditor.PackageManager.UI.Window");

            if (packageManagerUI != null)
            {
                var openMethod = packageManagerUI.GetMethod("Open", BindingFlags.Public | BindingFlags.Static);
                openMethod?.Invoke(null, new object[] { packageName });
            }
            else
            {
                Debug.LogWarning("Could not find PackageManager UI type. Package Manager may have changed.");
            }
        }
    }

    /// <summary>
    /// Editor Preferences for HDRP
    /// </summary>
    [DisplayInfo(name = "High Definition Render Pipeline", order = 200)]
    public class HDRenderPipelinePreferencesProvider : ICoreRenderPipelinePreferencesProvider
    {
        class Styles
        {
            public static readonly GUIContent configPackageLabel = new("Config Package", "Choose whether to embed or sync with registry version.");

            public static readonly GUIContent viewInPackageManagerLabel = new("View in Package Manager", "");

            public static readonly GUIContent hdrpProjectSettingsPathLabel = EditorGUIUtility.TrTextContent("Resources Folder Name", "Resources Folder will be the one where to get project elements related to HDRP as default scene and default settings.");

            public static readonly GUIContent matcapViewMixAlbedoLabel = EditorGUIUtility.TrTextContent("MatCap Mode Mix Albedo", "Enable to make HDRP mix the albedo of the Material with its material capture.");
            public static readonly GUIContent matcapViewScaleLabel = EditorGUIUtility.TrTextContent("MatCap Mode Intensity Scale", "Set the intensity of the material capture. This increases the brightness of the Scene. This is useful if the albedo darkens the Scene considerably.");
        }

        static List<string> s_SearchKeywords = new() {
            "MatCap Mode",
            "Intensity scale",
            "Mix Albedo",
            "Default Resources Folder",
            "Config Package"
        };

        /// <summary>
        /// Keyworks for the preferences
        /// </summary>
        public List<string> keywords => s_SearchKeywords;

        /// <summary>
        /// UI for the preferences.
        /// </summary>
        public void PreferenceGUI()
        {
            EditorGUI.indentLevel++;
            HDProjectSettings.projectSettingsFolderPath = EditorGUILayout.TextField(Styles.hdrpProjectSettingsPathLabel, HDProjectSettings.projectSettingsFolderPath);
            DrawConfigPackageDropdown();
            DrawMatCapDefaults();
            EditorGUI.indentLevel--;
        }

        void DrawMatCapDefaults()
        {
            var matCapMode = HDRenderPipelinePreferences.matCapMode;
            matCapMode.mixAlbedo.value = EditorGUILayout.Toggle(Styles.matcapViewMixAlbedoLabel, matCapMode.mixAlbedo.value);
            if (matCapMode.mixAlbedo.value)
                matCapMode.viewScale.value = EditorGUILayout.FloatField(Styles.matcapViewScaleLabel, matCapMode.viewScale.value);
        }

        private const string k_PackageName = "com.unity.render-pipelines.high-definition-config";
        
        void DrawConfigPackageDropdown()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.configPackageLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
            if (GUILayout.Button(Styles.viewInPackageManagerLabel, GUILayout.Width(200)))
            {
                PackageManagerHelper.OpenPackageManagerToPackage(k_PackageName);
            }
            GUILayout.EndHorizontal();
        }
    }
}
