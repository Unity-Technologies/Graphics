using UnityEngine;
using UnityEditor.ShaderApiReflection;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing;
using System.IO;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    internal class ShaderReflectionAssetPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (!ProviderLibrary.IsInstanceInitialized)
                return;

            List<GUID> modifiedFiles = new();
            foreach (string path in importedAssets)
            {
                if (Path.GetExtension(path).ToLower() != ".hlsl")
                    continue;

                var assetID = AssetDatabase.GUIDFromAssetPath(path);
                if (ProviderLibrary.Instance.AnalyzeFile(assetID))
                {
                    modifiedFiles.Add(assetID);
                }
            }

            if (modifiedFiles.Count > 0)
                NotifyAll(modifiedFiles);
        }

        internal static void NotifyAll(IEnumerable<GUID> modifiedFiles)
        {
            var editors = Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>();
            foreach(var editor in editors)
            {
                editor.NotifyDependencyUpdated(modifiedFiles);
            }
        }
    }
}
