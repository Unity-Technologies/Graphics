using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class ShaderGraphIndexerExtension
    {
        [CustomObjectIndexer(typeof(Shader), version = 0)]
        internal static void ShaderGraphImporterIndexer(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (ShaderGraphTemplateHelper.TryGetTemplateStatic(context.id, out var template))
            {
                GraphViewIndexerExtension.IndexCommonData<ShaderGraphImporter>(context, indexer, template);
            }
        }
    }
}
