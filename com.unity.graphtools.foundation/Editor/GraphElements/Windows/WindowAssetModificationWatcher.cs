using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class WindowAssetModificationWatcher : AssetModificationProcessor
    {
        static bool AssetAtPathIsGraphAsset(string path)
        {
            return typeof(IGraphAssetModel).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path));
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            if (AssetAtPathIsGraphAsset(assetPath))
            {
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                var windows = Resources.FindObjectsOfTypeAll<GraphViewEditorWindow>();
                foreach (var window in windows)
                {
                    if (window.GraphTool.ToolState.CurrentGraph.GraphModelAssetGuid == guid)
                    {
                        window.GraphTool.Dispatch(new UnloadGraphAssetCommand());
                    }
                }
            }
            return AssetDeleteResult.DidNotDelete;
        }

        static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            if (AssetAtPathIsGraphAsset(sourcePath))
            {
                var guid = AssetDatabase.AssetPathToGUID(sourcePath);
                var windows = Resources.FindObjectsOfTypeAll<GraphViewEditorWindow>();
                foreach (var window in windows)
                {
                    if (window.GraphTool.ToolState.CurrentGraph.GraphModelAssetGuid == guid ||
                        window.GraphTool.ToolState.SubGraphStack.Any(og => og.GraphModelAssetGuid == guid))
                    {
                        using (var toolStateUpdater = window.GraphTool.ToolState.UpdateScope)
                        {
                            toolStateUpdater.AssetChangedOnDisk();
                        }
                    }
                }
            }

            return AssetMoveResult.DidNotMove;
        }
    }
}
