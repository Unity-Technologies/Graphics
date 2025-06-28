using System;
using System.IO;
using UnityEditor.Search;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal
{
    static class ConversionIndexers
    {
        private const int k_Version = 9;

        [CustomObjectIndexer(typeof(Component), version = k_Version)]
        internal static void ComponentConversionIndexer(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            ConversionIndexer(context, indexer);
        }

        [CustomObjectIndexer(typeof(ScriptableObject), version = k_Version)]
        internal static void ScriptableObjectConversionIndexer(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            ConversionIndexer(context, indexer);
        }

        internal static void ConversionIndexer(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            var path = AssetDatabase.GetAssetPath(context.target);
            if (path.StartsWith("Packages"))
                return;
            
            //Custom finding of all default Material properties on every single object type including custom types
            if (MaterialReferenceBuilder.MaterialReferenceLookup.TryGetValue(context.targetType, out var methods))
            {
                if (!string.IsNullOrEmpty(path) &&
                    !path.EndsWith(".asset", StringComparison.InvariantCultureIgnoreCase) &&
                    !path.EndsWith(".prefab", StringComparison.InvariantCultureIgnoreCase) &&
                    !path.EndsWith(".unity", StringComparison.InvariantCultureIgnoreCase))
                    return;

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
