using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [ExcludeFromPreset]
    [ScriptedImporter(1, Extension, -903)]
    class ShaderSubGraphAssetImporter : ScriptedImporter
    {
        public const string Extension = ShaderGraphStencil.SubGraphExtension;
        static string[] GatherDependenciesFromSourceFile(string assetPath)
        {
            if (string.CompareOrdinal(Path.GetExtension(assetPath), "."+Extension) == 0)
            {
                return ShaderGraphAssetUtils.GatherDependenciesForShaderGraphAsset(assetPath);
            }
            return new string[0];
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (string.CompareOrdinal(Path.GetExtension(assetPath), "."+Extension) == 0)
            {
                ShaderGraphAssetUtils.HandleImport(ctx);
            }
        }
    }
}
