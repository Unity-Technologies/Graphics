using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    class ImportedGraphModificationWatcher : AssetModificationProcessor
    {
        static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            var graphAsset = AssetDatabase.LoadAssetAtPath<ImportedGraphAsset>(sourcePath);
            if (graphAsset != null && graphAsset.Dirty)
            {
                Debug.LogWarning("Renaming the graph will cause it to be reimported. To avoid data loss, save the graph before renaming it.");
                return AssetMoveResult.FailedMove;
            }

            return AssetMoveResult.DidNotMove;
        }
    }
}
