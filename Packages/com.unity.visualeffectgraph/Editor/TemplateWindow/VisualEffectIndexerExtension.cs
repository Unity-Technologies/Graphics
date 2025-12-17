using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    static class VisualEffectIndexerExtension
    {
        [CustomObjectIndexer(typeof(VisualEffectAsset), version = 0)]
        internal static void VisualEffectImporterIndexer(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (VFXTemplateHelperInternal.TryGetTemplateStatic(context.id, out var template))
            {
                GraphViewIndexerExtension.IndexCommonData<VisualEffectImporter>(context, indexer, template);
            }
        }
    }
}
