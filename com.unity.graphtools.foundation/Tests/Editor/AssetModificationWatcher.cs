using System;
using System.IO;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class AssetModificationWatcher : AssetModificationProcessor
    {
        public static int Version;

        static bool AssetAtPathIsGraphAsset(string path)
        {
            if (Path.GetExtension(path) != ".asset")
                return false;

            return typeof(IGraphAssetModel).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path));
        }

        static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths.Any(AssetAtPathIsGraphAsset))
                Version++;

            return paths;
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            if (AssetAtPathIsGraphAsset(assetPath))
                Version++;
            return AssetDeleteResult.DidNotDelete;
        }

        static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            if (AssetAtPathIsGraphAsset(sourcePath))
                Version++;
            return AssetMoveResult.DidNotMove;
        }
    }
}
