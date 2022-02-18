using UnityEditor.Search;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal
{
    static class ConversionIndexers
    {
        private const int k_Version = 8;

        [CustomObjectIndexer(typeof(Object), version = k_Version)]
        internal static void ConversionIndexer(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            //Custom finding of all default Material properties on every single object type including custom types
            if (MaterialReferenceBuilder.MaterialReferenceLookup.TryGetValue(context.targetType, out var methods))
            {
                foreach (var method in methods)
                {
                    if (method == null) continue;

                    var result = method.GetMaterialFromMethod(context.target, (methodName, objectName) =>
                        $"The method {methodName} was not found on {objectName}. This property will not be indexed.");

                    if (result is Material materialResult)
                    {
                        if (materialResult != null && MaterialReferenceBuilder.GetIsReadonlyMaterial(materialResult))
                        {
                            indexer.AddProperty("urp", "convert-readonly", context.documentIndex);
                        }
                    }
                    else if (result is Material[] materialArrayResult)
                    {
                        foreach (var material in materialArrayResult)
                        {
                            if (material != null && MaterialReferenceBuilder.GetIsReadonlyMaterial(material))
                            {
                                indexer.AddProperty("urp", "convert-readonly", context.documentIndex);
                            }
                        }
                    }
                }
            }
        }
    }
}
