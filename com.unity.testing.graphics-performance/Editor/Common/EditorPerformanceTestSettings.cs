using UnityEngine;
using UnityEditor;
using System.IO;

public class EditorPerformanceTestsSettings : ScriptableObject
{
    public const string k_TestAssetName = "EditorPerformanceTestsSettings";
    public static string k_PerformanceTestsPath => $"Assets/Editor/Resources/{k_TestAssetName}.asset";

    [SerializeField]
    public EditorShaderStaticAnalysisAsset  staticAnalysisAsset = null;

    public static EditorPerformanceTestsSettings instance { get => GetOrCreateSettings(); }

    internal static EditorPerformanceTestsSettings GetOrCreateSettings()
    {
        var settings = Resources.Load<EditorPerformanceTestsSettings>(k_TestAssetName);

        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<EditorPerformanceTestsSettings>();
            if (!Directory.Exists("Assets/Editor/Resources"))
                AssetDatabase.CreateFolder("Assets", "Editor/Resources");
            AssetDatabase.CreateAsset(settings, k_PerformanceTestsPath);
            AssetDatabase.SaveAssets();
        }
        return settings;
    }

    public static SerializedObject GetSerializedSettings()
    {
        return new SerializedObject(GetOrCreateSettings());
    }
}
