using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

using Object = UnityEngine.Object;

public static class MaterialReferenceBuilder
{
	public static readonly Dictionary<Type, List<MethodInfo>> MaterialReferenceLookup;

	static MaterialReferenceBuilder()
	{
		MaterialReferenceLookup = GetMaterialReferenceLookup();
	}

	public static List<Type> GetComponentTypes()
	{
		return MaterialReferenceLookup.Keys.Where(key => typeof(Component).IsAssignableFrom(key)).ToList();
	}

	private static Dictionary<Type, List<MethodInfo>> GetMaterialReferenceLookup()
	{
		var result = new Dictionary<Type, List<MethodInfo>>();

		var allObjectsWithMaterialProperties = TypeCache.GetTypesDerivedFrom<Object>()
			.Where(type => type.GetProperties().Any(HasMaterialProperty));

		foreach (var property in allObjectsWithMaterialProperties)
		{
			if (!result.ContainsKey(property))
			{
				result.Add(property, new List<MethodInfo>());
			}

			var materialProps = GetMaterialPropertiesWithoutLeaking(property);
			foreach (var prop in materialProps)
			{
				result[property].Add(prop.GetGetMethod());
			}
		}

		return result;
	}

	private static bool HasMaterialProperty(PropertyInfo prop)
	{
		return prop.PropertyType == typeof(Material) || prop.PropertyType == typeof(Material[]);
	}

	public static List<Material> GetMaterialsFromObject(Object obj)
	{
		var result = new List<Material>();

		var allMaterialProperties = new List<PropertyInfo>();
		if (obj is GameObject go)
		{
			var componentList = new List<Component>();

			foreach (var key in GetComponentTypes())
			{
				var components = go.GetComponentsInChildren(key);
				foreach (var component in components)
				{
					result.AddRange(GetMaterials(component));
				}
			}
		}
		else
		{
			result.AddRange(GetMaterials(obj));
		}

		return result.Distinct().ToList();
	}

	private static List<Material> GetMaterials(Object obj)
	{
		var result = new List<Material>();

		var allMaterialProperties = obj.GetType().GetMaterialPropertiesWithoutLeaking();
		foreach (var property in allMaterialProperties)
		{
			var value = property.GetGetMethod().Invoke(obj, null);

			if (value is Material materialResult)
			{
				result.Add(materialResult);
			}
			else if (value is Material[] materialList)
			{
				result.AddRange(materialList);
			}
		}

		return result;
	}

	public static IEnumerable<PropertyInfo> GetMaterialPropertiesWithoutLeaking(this Type property)
	{
		var materialProps = property.GetProperties().Where(HasMaterialProperty).ToList();

		// if there is a sharedMaterial property or sharedMaterials property, remove the property that will leak materials
		var sharedMaterialProps =
			materialProps.Where(prop => prop.Name.ToLowerInvariant().Contains("shared")).ToList();

		var propsToRemove = sharedMaterialProps
			.Select(prop => prop.Name.ToLowerInvariant().Replace("shared", string.Empty))
			.ToList();
		materialProps.RemoveAll(prop => propsToRemove.Contains(prop.Name.ToLowerInvariant()));

        // also remove any property which has no setter
        materialProps.RemoveAll(prop => prop.SetMethod == null);

        return materialProps;
	}

	public static bool GetIsReadonlyMaterial(Material material)
	{
		var assetPath = AssetDatabase.GetAssetPath(material);

		return string.IsNullOrEmpty(assetPath) || assetPath.Equals(@"Resources/unity_builtin_extra", StringComparison.OrdinalIgnoreCase);
	}
}
