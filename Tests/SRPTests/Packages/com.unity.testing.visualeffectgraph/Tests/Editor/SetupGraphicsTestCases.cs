using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.Scripting;
using UnityEngine.TestTools;
using UnityEngine.VFX;

// Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
// Once that's fixed, this class can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
// can be used directly instead.
public class SetupGraphicsTestCases : IPrebuildSetup
{
    public void Setup()
    {
        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup();

        // Configure project for XR tests
        Unity.Testing.XR.Editor.SetupMockHMD.SetupLoader();

        var vfxAssetsGuid = AssetDatabase.FindAssets("t:VisualEffectAsset AssetBundle");

        if (vfxAssetsGuid.Any())
        {
            foreach (var guid in vfxAssetsGuid)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(assetPath);
            }
            EditorUtility.ClearProgressBar();
        }

        var bundlePath = "Assets/StreamingAssets/" + Unity.Testing.VisualEffectGraph.AssetBundleHelper.kAssetBundleRoot;
        if (!Directory.Exists(bundlePath))
        {
            Directory.CreateDirectory(bundlePath);
        }
        BuildTarget target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
        UnityEditor.BuildPipeline.BuildAssetBundles(bundlePath, UnityEditor.BuildAssetBundleOptions.None, target);
    }
}
