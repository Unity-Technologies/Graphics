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
                ShowObjectField(settings.FindProperty("staticAnalysisAsset"), typeof(EditorShaderStaticAnalysisAsset), new GUIContent("Static Analysis Asset"));
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

    class Styles
    {
        public static GUIContent number = new GUIContent("My Number");
        public static GUIContent someString = new GUIContent("Some string");
    }

    const string k_PerformanceTestsPath = "ProjectSettings/PerformanceTestsSettings.asset";
    public PerformanceTestsProvider(string path, SettingsScope scope = SettingsScope.User)
        : base(path, scope) {}

    public static bool IsSettingsAvailable()
    {
        return File.Exists(k_PerformanceTestsPath);
    }

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        // This function is called when the user clicks on the MyCustom element in the Settings window.
        m_CustomSettings = PerformanceTestSettings.GetSerializedSettings();
    }

    public override void OnGUI(string searchContext)
    {
        // Use IMGUI to display UI:
        // EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("m_Number"), Styles.number);
        // EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("m_SomeString"), Styles.someString);
        Debug.Log("UI ?");
    }

    // Register the SettingsProvider
    [SettingsProvider]
    public static SettingsProvider CreatePerformanceTestsProvider()
    {
        if (IsSettingsAvailable())
        {
            var provider = new PerformanceTestsProvider("Project/PerformanceTestsProvider", SettingsScope.Project);

            // Automatically extract all keywords from the Styles.
            provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
            return provider;
        }

        // Settings Asset doesn't exist yet; no need to display anything in the Settings window.
        return null;
    }
}
