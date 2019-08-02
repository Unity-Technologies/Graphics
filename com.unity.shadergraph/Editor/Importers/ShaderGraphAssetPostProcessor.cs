using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    class ShaderGraphAssetPostProcessor : AssetPostprocessor
    {
        static void RegisterShaders(string[] paths)
        {
            foreach (var path in paths)
            {
                if (!path.EndsWith(ShaderGraphImporter.Extension, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var mainObj = AssetDatabase.LoadMainAssetAtPath(path);
                if (mainObj is Shader)
                    ShaderUtil.RegisterShader((Shader)mainObj);

                var objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                foreach (var obj in objs)
                {
                    if (obj is Shader)
                        ShaderUtil.RegisterShader((Shader)obj);
                }
            }
        }

        static void UpdateAfterAssetChange(string[] newNames)
        {
            // This will change the title of the window.
            MaterialGraphEditWindow[] windows = Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>();
            foreach (var matGraphEditWindow in windows)
            {
                for (int i = 0; i < newNames.Length; ++i)
                {
                    if (matGraphEditWindow.selectedGuid == AssetDatabase.AssetPathToGUID(newNames[i]))
                        matGraphEditWindow.assetName = Path.GetFileNameWithoutExtension(newNames[i]).Split('/').Last();
                }
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            RegisterShaders(importedAssets);

            bool anyShaders = movedAssets.Any(val => val.EndsWith(ShaderGraphImporter.Extension, StringComparison.InvariantCultureIgnoreCase));
            anyShaders |= movedAssets.Any(val => val.EndsWith(ShaderSubGraphImporter.Extension, StringComparison.InvariantCultureIgnoreCase));
            if (anyShaders)
                UpdateAfterAssetChange(movedAssets);
            
            var changedFiles = movedAssets.Union(importedAssets)
                .Where(x => x.EndsWith(ShaderSubGraphImporter.Extension, StringComparison.InvariantCultureIgnoreCase)
                || CustomFunctionNode.s_ValidExtensions.Contains(Path.GetExtension(x)))
                .Select(AssetDatabase.AssetPathToGUID)
                .Distinct()
                .ToList();

            if (changedFiles.Count > 0)
            {
                var windows = Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>();
                foreach (var window in windows)
                {
                    window.ReloadSubGraphsOnNextUpdate(changedFiles);
                }
            }
        }
    }
}
