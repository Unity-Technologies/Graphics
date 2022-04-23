using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class WindowAssetModificationWatcher : AssetModificationProcessor
    {
        static bool AssetAtPathIsGraphAsset(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GraphAsset>(path) != null;
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            // Avoid calling FindObjectsOfTypeAll if the deleted asset is not a graph asset.
            if (AssetAtPathIsGraphAsset(assetPath))
            {
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                var windows = Resources.FindObjectsOfTypeAll<GraphViewEditorWindow>();
                foreach (var window in windows)
                {
                    if (WindowAssetPostprocessingWatcher.IsWindowDisplayingGraphAsset(window, guid))
                    {
                        // Unload graph *before* it is deleted.
                        window.GraphTool.Dispatch(new UnloadGraphCommand());
                    }
                }
            }
            return AssetDeleteResult.DidNotDelete;
        }
    }
}
