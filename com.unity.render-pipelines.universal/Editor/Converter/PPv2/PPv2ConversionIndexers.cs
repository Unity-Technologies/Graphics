#if PPV2_EXISTS
using UnityEditor.Search;
using UnityEngine;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.Universal
{
    static class PPv2ConversionIndexers
    {
        // Note: Iterating this version number does not force an index rebuild.
        //       If modifying this code, you will need to either delete the Library directory,
        //       or open "Window > Search > Index Manager" and right-click each indexer to do a Force Rebuild.
        private const int Version = 10;

        [CustomObjectIndexer(typeof(Object), version = Version)]
        internal static void ConversionIndexer(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            // Note: Volumes and Layers on the same object would still produce only a
            //       single result in the "urp:convert-ppv2component" search, thus we only
            //       explicitly add one or the other here, instead of both.
            if (context.targetType == typeof(BIRPRendering.PostProcessVolume))
            {
                indexer.AddProperty("urp", "convert-ppv2component", context.documentIndex);
            }
            else if (context.targetType == typeof(BIRPRendering.PostProcessLayer))
            {
                indexer.AddProperty("urp", "convert-ppv2component", context.documentIndex);
            }

            if (context.targetType == typeof(BIRPRendering.PostProcessProfile))
            {
                indexer.AddProperty("urp", "convert-ppv2scriptableobject", context.documentIndex);
            }
        }
    }
}
#endif
