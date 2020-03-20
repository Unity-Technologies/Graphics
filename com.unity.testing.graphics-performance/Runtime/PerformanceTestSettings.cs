using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PerformanceTestsSettings : ScriptableObject
{
    public const string k_TestAssetName = "PerformanceTestsSettings";
    public static string k_PerformanceTestsPath => $"Assets/Resources/{k_TestAssetName}.asset";

    [SerializeField]
    public TestSceneAsset                   testDescriptionAsset = null;

    public static PerformanceTestsSettings instance { get => GetOrCreateSettings(); }

    internal static PerformanceTestsSettings GetOrCreateSettings()
    {
        var settings = Resources.Load<PerformanceTestsSettings>(k_TestAssetName);

#if UNITY_EDITOR
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PerformanceTestsSettings>();
            if (!Directory.Exists("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateAsset(settings, k_PerformanceTestsPath);
            AssetDatabase.SaveAssets();
        }
#endif
        return settings;
    }

#if UNITY_EDITOR
    public static SerializedObject GetSerializedSettings()
    {
        return new SerializedObject(GetOrCreateSettings());
    }
#endif
}
