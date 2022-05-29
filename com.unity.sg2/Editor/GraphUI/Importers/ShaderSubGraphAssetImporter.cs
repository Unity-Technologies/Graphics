using UnityEditor.AssetImporters;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [ExcludeFromPreset]
    [ScriptedImporter(1, Extension, -903)]
    class ShaderSubGraphAssetImporter : ScriptedImporter
    {
        public const string Extension = ShaderGraphStencil.SubGraphExtension;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            ShaderGraphAssetUtils.HandleImport(ctx);
        }
    }
}
