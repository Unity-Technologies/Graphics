using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

static class ConversionIndexers
{
	private const int Version = 8;

	[CustomObjectIndexer(typeof(Object), version = Version)]
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
					if (MaterialReferenceBuilder.GetIsReadonlyMaterial(materialResult))
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
