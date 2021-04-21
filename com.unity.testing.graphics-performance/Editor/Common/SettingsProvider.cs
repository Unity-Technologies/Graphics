using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;

static class PerformanceSettingsProviderGUI
{
    [SettingsProvider]
    public static SettingsProvider CreatePerformanceTestsProvider()
    {
        var provider = new SettingsProvider("Project/Performance Tests", SettingsScope.Project)
        {
            // By default the last token of the path is used as display name if no label is provided.
            label = "Performance Tests",
            // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
            guiHandler = (searchContext) =>
            {
                var settings = PerformanceTestSettings.GetSerializedSettings();

                EditorGUI.BeginChangeCheck();
                ShowObjectField(settings.FindProperty("testDescriptionAsset"), typeof(TestSceneAsset), new GUIContent("Test Description Asset"));
#if SHADERANALYSIS_SUPPORT
                ShowObjectField(settings.FindProperty("staticAnalysisAsset"), typeof(EditorShaderStaticAnalysisAsset), new GUIContent("Static Analysis Asset"));
#endif
                if (EditorGUI.EndChangeCheck())
                    settings.ApplyModifiedProperties();
            },

            // Populate the search keywords to enable smart search filtering and label highlighting:
            keywords = new HashSet<string>(new[] { "Performance" })
        };

        return provider;
    }

    static void ShowObjectField(SerializedProperty resourcePathProperty, Type objectType, GUIContent content)
    {
        Object res = Resources.Load(resourcePathProperty.stringValue, objectType);
        res = EditorGUILayout.ObjectField(content, res, objectType, false);

        // Find the resource path of the object:
        resourcePathProperty.stringValue = null;
        if (res != null)
        {
            var path = AssetDatabase.GetAssetPath(res);
            if (path.Contains("Resources"))
            {
                var resourcePath = path.Substring(path.LastIndexOf("Resources/") + "Resources/".Length);
                resourcePath = Path.ChangeExtension(resourcePath, null);
                resourcePathProperty.stringValue = resourcePath;
            }
            else
            {
                Debug.LogError("You must choose an asset within a Resources folder");
            }
        }
    }
}

// Create PerformanceTestsProvider by deriving from SettingsProvider:
class PerformanceTestsProvider : SettingsProvider
{
    private SerializedObject m_CustomSettings;

    const string k_PerformanceTestsPath = "ProjectSettings/PerformanceTestsSettings.asset";
    public PerformanceTestsProvider(string path, SettingsScope scope = SettingsScope.User)
        : base(path, scope) {}

    public static bool IsSettingsAvailable() => File.Exists(k_PerformanceTestsPath);

    // Register the SettingsProvider
    [SettingsProvider]
    public static SettingsProvider CreatePerformanceTestsProvider()
    {
        if (IsSettingsAvailable())
            return new PerformanceTestsProvider("Project/PerformanceTestsProvider", SettingsScope.Project);

        // Settings Asset doesn't exist yet; no need to display anything in the Settings window.
        return null;
    }
}
