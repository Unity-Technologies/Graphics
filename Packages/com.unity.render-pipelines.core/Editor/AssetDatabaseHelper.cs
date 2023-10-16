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
        /// <param name="extension">Asset type extension i.e ".mat" for materials</param>
        /// <typeparam name="T">The type of material you are looking for</typeparam>
        /// <returns>A IEnumerable object</returns>
        public static IEnumerable<T> FindAssets<T>(string extension = null)
        {
            string typeName = typeof(T).ToString();
            int i = typeName.LastIndexOf('.');
            if (i != -1)
            {
                typeName = typeName.Substring(i+1, typeName.Length - i-1);
            }

            string query = !string.IsNullOrEmpty(extension) ? $"t:{typeName} glob:\"**/*{extension}\"" : $"t:{typeName}";

            foreach (var guid in AssetDatabase.FindAssets(query))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (asset is T castAsset)
                    yield return castAsset;
            }
        }
    }
}
