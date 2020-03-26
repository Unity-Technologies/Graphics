using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.VFX;

// Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
// Once that's fixed, this class can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
// can be used directly instead.
public class SetupGraphicsTestCases : IPrebuildSetup
{
    public static void RebuildVisualEffectAsset(VisualEffectAsset vfx)
    {
    }

    private static string GetAssetBundleBasePath()
    {
        var basePath = System.IO.Directory.GetCurrentDirectory();

        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].ToLower() == "-logfile" && i != args.Length - 1)
            {
                var testResultID = "test-results"; //Find a nice path for yamato output
                var logPath = args[i + 1];
                if (logPath.Contains(testResultID))
                {
                    basePath = logPath.Substring(0, logPath.IndexOf(testResultID) + testResultID.Length);
                }
                break;
            }
        }
        return System.IO.Path.Combine(basePath, "VFX_Bundle_Test");
    }

    public void Setup()
    {
        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup();

        var vfxAssetsGuid = AssetDatabase.FindAssets("t:VisualEffectAsset AssetBundle");

        if (vfxAssetsGuid.Any())
        {
            foreach (var guid in vfxAssetsGuid)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
                if (vfxAsset != null)
                {
                    var vfxName = Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid));
                    EditorUtility.DisplayProgressBar("Recompiling Asset Bundle VFX : " + vfxName, "Asset Bundle", 0);
                    RebuildVisualEffectAsset(vfxAsset);
                }
            }
            EditorUtility.ClearProgressBar();
        }

        var bundlePath = GetAssetBundleBasePath();
        if (!Directory.Exists(bundlePath))
        {
            Directory.CreateDirectory(bundlePath);
        }
        BuildTarget target = UnityEditor.BuildTarget.NoTarget;

#if UNITY_STANDALONE_OSX
        target = UnityEditor.BuildTarget.StandaloneOSX;
#elif UNITY_STANDALONE_LINUX
        target = UnityEditor.BuildTarget.StandaloneLinux64;
#elif UNITY_STANDALONE_WIN
        target = UnityEditor.BuildTarget.StandaloneWindows64;
#else
        Debug.LogError("Unable to choose the correct target while building AssetBundle");
#endif

        UnityEditor.BuildPipeline.BuildAssetBundles(bundlePath, UnityEditor.BuildAssetBundleOptions.None, target);
        if (!Directory.Exists("Assets/StreamingAssets"))
            Directory.CreateDirectory("Assets/StreamingAssets");
        File.WriteAllText("Assets/StreamingAssets/AssetBundlePath.txt", bundlePath);
    }
}
