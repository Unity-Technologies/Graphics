using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class ShaderGraphIndexerExtension
    {
        static void ShaderGraphIndexer(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (ShaderGraphTemplateHelper.TryGetTemplateStatic(context.id, out var template, out var dataBag))
            {
                GraphViewIndexerExtension.IndexCommonData<ShaderGraphImporter>(context, indexer, template);
                GraphViewIndexerExtension.IndexCustomData<ShaderGraphImporter>(context, indexer, dataBag.GetCustomData($"{template.ToolKey}."));
            }
        }

        [CustomObjectIndexer(typeof(ShaderGraphImporter), version = 1)]
        internal static void ShaderGraphImporterIndexer(CustomObjectIndexerTarget context, ObjectIndexer indexer)
            => ShaderGraphIndexer(context, indexer);

        [CustomObjectIndexer(typeof(Shader), version = 1)]
        internal static void ShaderGraphShaderIndexer(CustomObjectIndexerTarget context, ObjectIndexer indexer)
            => ShaderGraphIndexer(context, indexer);
    }
}
