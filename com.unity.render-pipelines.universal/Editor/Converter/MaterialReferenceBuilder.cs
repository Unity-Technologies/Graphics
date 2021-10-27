using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal.Converters
{
    internal static class MaterialReferenceBuilder
    {
        public static readonly Dictionary<Type, List<MethodInfo>> MaterialReferenceLookup;

        static MaterialReferenceBuilder()
        {
            MaterialReferenceLookup = GetMaterialReferenceLookup();
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

        private static List<Material> GetMaterials(Object obj)
        {
            var result = new List<Material>();

            var allMaterialProperties = obj.GetType().GetMaterialPropertiesWithoutLeaking();
            foreach (var property in allMaterialProperties)
            {
                var value = property.GetGetMethod().GetMaterialFromMethod(obj, (methodName, objectName) =>
                    $"The method {methodName} was not found on {objectName}. This property will not be indexed.");

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

        /// <summary>
        /// Gets all of the types in the Material Reference lookup that are components. Used to determine whether to run the
        /// method directly or on the component
        /// </summary>
        /// <returns>List of types that are components</returns>
        public static List<Type> GetComponentTypes()
        {
            return MaterialReferenceLookup.Keys.Where(key => typeof(Component).IsAssignableFrom(key)).ToList();
        }

        /// <summary>
        /// Gets all material properties from an object or a component of an object
        /// </summary>
        /// <param name="obj">The GameObject or Scriptable Object</param>
        /// <returns>List of Materials</returns>
        public static List<Material> GetMaterialsFromObject(Object obj)
        {
            var result = new List<Material>();

            if (obj is GameObject go)
            {
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

        /// <summary>
        /// Text Mesh pro will sometimes be missing the GetFontSharedMaterials method, even though the property is supposed
        /// to have that method. This gracefully handles that case.
        /// </summary>
        /// <param name="method">The Method being invoked</param>
        /// <param name="obj">The Unity Object the method is invoked upon</param>
        /// <param name="generateErrorString">The function that takes the method name and object name and produces an error string</param>
        /// <returns>The resulting object from invoking the method on the Object</returns>
        /// <exception cref="Exception">Any exception that is not the missing method exception</exception>
        public static object GetMaterialFromMethod(this MethodInfo method,
            Object obj,
            Func<string, string, string> generateErrorString)
        {
            object result = null;
            try
            {
                result = method.Invoke(obj, null);
            }
            catch (Exception e)
            {
                // swallow the missing method exception, there's nothing we can do about it at this point
                // and we've already checked for other possible null exceptions here
                if ((e.InnerException is NullReferenceException))
                {
                    Debug.LogWarning(generateErrorString(method.Name, obj.name));
                }
                else
                {
                    throw e;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the SharedMaterial(s) properties when there are shared materials so that we don't leak material instances into the scene
        /// </summary>
        /// <param name="property">The property Type that we are getting the SharedMaterial(s) properties from</param>
        /// <returns>List of shared material properties and other material properties that won't leak material instances</returns>
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

        /// <summary>
        /// Get whether or not a Material is considered readonly (Built In Resource)
        /// </summary>
        /// <param name="material">The Material to test</param>
        /// <returns>Boolean of whether or not that Material is considered readonly</returns>
        public static bool GetIsReadonlyMaterial(Material material)
        {
            var assetPath = AssetDatabase.GetAssetPath(material);

            return string.IsNullOrEmpty(assetPath) || assetPath.Equals(@"Resources/unity_builtin_extra", StringComparison.OrdinalIgnoreCase);
        }
    }
}
