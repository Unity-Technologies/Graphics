using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PerformanceTestSettings))]
class PerformanceTestSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("You can edit this asset in Project Settings > Performance Tests", MessageType.Info);
        if (GUILayout.Button("Open"))
            SettingsService.OpenProjectSettings("Project/Performance Tests");
    }
}

#endif

public class PerformanceTestSettings : ScriptableObject
{
    public const string k_TestAssetName = "PerformanceTestsSettings";
    public static string k_PerformanceTestsPath => $"Assets/Resources/{k_TestAssetName}.asset";

    public string   testDescriptionAsset = null;
    public string   staticAnalysisAsset = null;

    public static PerformanceTestSettings instance { get => GetOrCreateSettings(); }

    internal static PerformanceTestSettings GetOrCreateSettings()
    {
        var settings = Resources.Load<PerformanceTestSettings>(k_TestAssetName);

#if UNITY_EDITOR
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PerformanceTestSettings>();
            if (!Directory.Exists("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateAsset(settings, k_PerformanceTestsPath);
            AssetDatabase.SaveAssets();
        }
#endif
        return settings;
    }

    public static TestSceneAsset GetTestSceneDescriptionAsset() => Resources.Load<TestSceneAsset>(instance.testDescriptionAsset);

#if UNITY_EDITOR
    // We use an object for this method because we don't have access to the type.
    public static Object GetStaticAnalysisAsset() => Resources.Load<Object>(instance.staticAnalysisAsset);

    public static SerializedObject GetSerializedSettings()
    {
        return new SerializedObject(GetOrCreateSettings());
    }
#endif
}
