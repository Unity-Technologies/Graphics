using System.IO;
using UnityEditor.VFX.UI;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    internal static class VFXControllerTestHelper
    {
        internal const string testAssetName = "Assets/TmpTests/VFXGraph1.vfx";
        internal const string testSubgraphAssetName = "Assets/TmpTests/VFXGraphSub.vfx";
        internal const string testSubgraphSubAssetName = "Assets/TmpTests/VFXGraphSub_Subgraph.vfx";
        internal const string testAssetMainSubgraph = "Assets/TmpTests/VFXGraphSubGraph_Main.vfx";
        internal const string testSubgraphSubOperatorAssetName = "Assets/TmpTests/VFXGraphSub_Subgraph.vfxoperator";
        internal const string testSubgraphBlockAssetName = "Assets/TmpTests/VFXGraphSub_Subgraph.vfxblock";

        internal static VFXViewController CreateTestAsset()
        {
            var directoryPath = Path.GetDirectoryName(testAssetName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (File.Exists(testAssetName))
            {
                AssetDatabase.DeleteAsset(testAssetName);
            }

            VisualEffectAsset asset = VisualEffectAssetEditorUtility.CreateNewAsset(testAssetName);
            VisualEffectResource resource = asset.GetResource(); // force resource creation

            var viewController = VFXViewController.GetController(resource);
            viewController.useCount++;

            return viewController;
        }

        internal static void DestroyTestAsset(VFXViewController viewController, int startUndoGroupId, string randomAssetPath)
        {
            viewController.useCount--;
            Undo.RevertAllDownToGroup(startUndoGroupId);
            AssetDatabase.DeleteAsset(testAssetName);
            AssetDatabase.DeleteAsset(testAssetMainSubgraph);
            AssetDatabase.DeleteAsset(testSubgraphAssetName);
            AssetDatabase.DeleteAsset(testSubgraphSubAssetName);
            AssetDatabase.DeleteAsset(testSubgraphSubOperatorAssetName);
            AssetDatabase.DeleteAsset(testSubgraphBlockAssetName);
            if (!string.IsNullOrEmpty(randomAssetPath))
            {
                AssetDatabase.DeleteAsset(randomAssetPath);
            }
            VFXTestCommon.DeleteAllTemporaryGraph();
        }
    }
}
