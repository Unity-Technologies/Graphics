using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary> Set of helpers for AssetDatabase operations. </summary>
    public static class AssetDatabaseHelper
    {
        /// <summary>
        /// Finds all assets of type T in the project.
        /// </summary>
        /// <param name="extension">Asset type extension i.e ".mat" for materials. Specifying extension make this faster.</param>
        /// <typeparam name="T">The type of asset you are looking for</typeparam>
        /// <returns>An IEnumerable off all assets found.</returns>
        public static IEnumerable<T> FindAssets<T>(string extension = null)
            where T : Object
        {
            string query = BuildQueryToFindAssets<T>(extension);
            foreach (var guid in AssetDatabase.FindAssets(query))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (asset is T castAsset)
                    yield return castAsset;
            }
        }
        
        /// <summary>
        /// Finds all assets paths of type T in the project.
        /// </summary>
        /// <param name="extension">Asset type extension i.e ".mat" for materials. Specifying extension make this faster.</param>
        /// <typeparam name="T">The type of asset you are looking for</typeparam>
        /// <returns>An IEnumerable off all assets paths found.</returns>
        public static IEnumerable<string> FindAssetPaths<T>(string extension = null)
            where T : Object
        {
            string query = BuildQueryToFindAssets<T>(extension);
            foreach (var guid in AssetDatabase.FindAssets(query))
                yield return AssetDatabase.GUIDToAssetPath(guid);
        }

        static string BuildQueryToFindAssets<T>(string extension = null)
            where T : Object
        {
            string typeName = typeof(T).ToString();
            int i = typeName.LastIndexOf('.');
            if (i != -1)
            {
                typeName = typeName.Substring(i+1, typeName.Length - i-1);
            }

            return string.IsNullOrEmpty(extension) ? $"t:{typeName}" : $"t:{typeName} glob:\"**/*{extension}\"";
        }
    }
}
