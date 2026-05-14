using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Tests
{
    // Cleanup is skipped intentionally; the copied asset is harmless across runs.
    internal sealed class TestPrebuildSetup : IPrebuildSetup
    {
        const string k_SrcPath =
            "Packages/com.unity.render-pipelines.core/Tests/Editor/SurfaceCache/TestShaders.asset";
        const string k_DstFolder = "Assets/Resources";
        const string k_DstPath   = k_DstFolder + "/TestShaders.asset";

        public void Setup()
        {
#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder(k_DstFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (AssetDatabase.LoadAssetAtPath<TestShaderAsset>(k_DstPath) == null)
                AssetDatabase.CopyAsset(k_SrcPath, k_DstPath);
#endif
        }
    }
}
