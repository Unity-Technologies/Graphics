using System.IO;
using System.Linq;
using UnityEditor.VFX.Block.Test;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    internal static class VFXSerializationTestHelper
    {
        internal readonly static string kTestAssetDir = "Assets/Tests";
        internal readonly static string kTestAssetName = "TestAsset";
        internal readonly static string kTestAssetPath = kTestAssetDir + "/" + kTestAssetName + ".vfx";

        internal static void CreateDirectory(out bool createdTestAssetDirectory)
        {
            createdTestAssetDirectory = !Directory.Exists(kTestAssetDir);
            if (!AssetDatabase.IsValidFolder(kTestAssetDir))
                AssetDatabase.CreateFolder("Assets", "Tests");
        }

        internal static void CreateSeedAsset(out bool createdTestAsset, out bool createdTestAssetDirectory)
        {
            createdTestAsset = false;
            CreateDirectory(out createdTestAssetDirectory);
            if (!File.Exists(kTestAssetPath))
            {
                File.Copy(FileUtil.PathToAbsolutePath("Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Serialization/RoundTrip/TestAsset.vfx"), kTestAssetPath);
                createdTestAsset = true;
                AssetDatabase.ImportAsset(kTestAssetPath);
            }

            string[] guids = AssetDatabase.FindAssets(kTestAssetName, new string[] { kTestAssetDir });

            // If the asset does not exist, create it
            if (guids.Length == 0)
            {
                VisualEffectAsset asset = CreateAssetAtPath(kTestAssetPath);
                InitAsset(asset);
            }
        }

        private static VisualEffectAsset CreateAssetAtPath(string path)
        {
            return VisualEffectAssetEditorUtility.CreateNewAsset(path);
        }

        private static void InitAsset(VisualEffectAsset asset)
        {
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetGraph();
            graph.RemoveAllChildren();

            var init0 = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var update0 = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var output0 = ScriptableObject.CreateInstance<VFXPointOutput>();

            graph.AddChild(init0);
            graph.AddChild(update0);
            graph.AddChild(output0);

            init0.LinkTo(update0);
            update0.LinkTo(output0);

            var init1 = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var output1 = ScriptableObject.CreateInstance<VFXPointOutput>();

            init1.LinkTo(output1);

            graph.AddChild(init1);
            graph.AddChild(output1);

            // Add some block
            var block0 = ScriptableObject.CreateInstance<InitBlockTest>();
            var block1 = ScriptableObject.CreateInstance<UpdateBlockTest>();
            var block2 = ScriptableObject.CreateInstance<OutputBlockTest>();

            // Add some operator
            VFXOperator add = ScriptableObject.CreateInstance<Operator.Add>();

            init0.AddChild(block0);
            update0.AddChild(block1);
            output0.AddChild(block2);

            graph.AddChild(add);
        }

        internal static void CleanupDirectory(bool createdTestAssetDirectory)
        {
            if (createdTestAssetDirectory && Directory.Exists(kTestAssetDir) && !Directory.EnumerateFileSystemEntries(kTestAssetDir).Any())
                AssetDatabase.DeleteAsset(kTestAssetDir);
        }

        internal static void CleanupSeedAsset(bool createdTestAsset, bool createdTestAssetDirectory)
        {
            if (createdTestAsset)
                AssetDatabase.DeleteAsset(kTestAssetPath);
            CleanupDirectory(createdTestAssetDirectory);
        }
    }
}
