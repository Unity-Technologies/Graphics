using System.Collections.Generic;
using System.Reflection;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
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
    /// Editor Preferences for URP
    /// </summary>
    [DisplayInfo(name = "Universal Render Pipeline", order = 300)]
    public class UniversalRenderPipelinePreferencesProvider : ICoreRenderPipelinePreferencesProvider
    {
        class Styles
        {
            public static readonly GUIContent configPackageLabel = new("Config Package", "Choose whether to embed or sync with registry version.");

            public static readonly GUIContent viewInPackageManagerLabel = new("View in Package Manager", "");

            public static readonly GUIContent urpProjectSettingsPathLabel = EditorGUIUtility.TrTextContent("Resources Folder Name", "Resources Folder will be the one where to get project elements related to URP as default scene and default settings.");
        }

        static List<string> s_SearchKeywords = new() {
            "Default Resources Folder",
            "Config Package"
        };

        /// <summary>
        /// Keywords for the preferences
        /// </summary>
        public List<string> keywords => s_SearchKeywords;

        /// <summary>
        /// UI for the preferences.
        /// </summary>
        public void PreferenceGUI()
        {
            EditorGUI.indentLevel++;
            UniversalProjectSettings.projectSettingsFolderPath = EditorGUILayout.TextField(Styles.urpProjectSettingsPathLabel, UniversalProjectSettings.projectSettingsFolderPath);
            DrawConfigPackageDropdown();
            EditorGUI.indentLevel--;
        }

        private const string k_PackageName = "com.unity.render-pipelines.universal-config";
        
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
