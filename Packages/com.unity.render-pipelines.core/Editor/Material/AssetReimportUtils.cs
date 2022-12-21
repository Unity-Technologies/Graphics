using System;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Set of utilities for Material reimporting.
    /// </summary>
    public static class AssetReimportUtils
    {
        /// <summary>
        /// Re-imports a given type of asset, and sends an analytic with the elapsed time
        /// </summary>
        /// <param name="duration">The elapsed time</param>
        /// <param name="numberOfAssetsReimported">The number of assets that have been re-imported</param>
        /// <param name="importNeedDelegate">A delegate if you want to skip some asset to be re-imported</param>
        /// <typeparam name="TAsset">The asset type that will be re-imported</typeparam>
        public static void ReimportAll<TAsset>(out double duration, out uint numberOfAssetsReimported, Func<string, bool> importNeedDelegate = null)
        {
            numberOfAssetsReimported = 0;
            duration = 0.0;
            using (TimedScope.FromRef(ref duration))
            {
                string[] distinctGuids = AssetDatabase
                    .FindAssets($"t:{typeof(TAsset).Name}", null);

                try
                {
                    AssetDatabase.StartAssetEditing();

                    for (int i = 0, total = distinctGuids.Length; i < total; ++i)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(distinctGuids[i]);
                        EditorUtility.DisplayProgressBar($"{typeof(TAsset).Name} Upgrader re-import", $"({i} of {total}) {path}", (float)i / (float)total);
                        if (importNeedDelegate?.Invoke(path) ?? true)
                        {
                            AssetDatabase.ImportAsset(path);
                            numberOfAssetsReimported++;
                        }
                    }
                }
                finally
                {
                    // Ensure the AssetDatabase knows we're finished editing
                    AssetDatabase.StopAssetEditing();
                }
            }

            EditorUtility.ClearProgressBar();
        }
    }
}
