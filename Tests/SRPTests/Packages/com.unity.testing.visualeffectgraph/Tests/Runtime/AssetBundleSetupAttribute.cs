using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.Scripting;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph.Tests
{
    public class AssetBundleSetupAttribute : GraphicsPrebuildSetupAttribute
    {
        public AssetBundleSetupAttribute() : base() { }

        public override void Setup()
        {
#if UNITY_EDITOR
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
#endif
        }
    }
}
