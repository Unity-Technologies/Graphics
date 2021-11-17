using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal class MaterialPropertyInfo
    {
        internal string DefaultExpression;
        internal string declaration;

        internal void Declare(ShaderBuilder builder, string propertyName)
        {
            builder.Indentation();
            builder.Add($"{declaration} = {DefaultExpression}");
            builder.AddLine("");
        }

        internal static List<MaterialPropertyInfo> Extract(BlockProperty property)
        {
            var results = new List<MaterialPropertyInfo>();

            var propType = property.Type;
            // If this is a struct, walk all fields and see if any are material properties
            if (propType.IsStruct)
            {
                foreach (var field in propType.StructFields)
                    ExtractProperty(field.Attributes, property, field, results);
            }
            // Otherwise try and extract material properties from the property itself
            else
                ExtractProperty(property.Attributes, property, StructField.Invalid, results);
       
            return results;
        }

        static bool ExtractProperty(IEnumerable<ShaderAttribute> attributes, BlockVariable property, StructField subField, List<MaterialPropertyInfo> results)
        {
            // The [MaterialProperty] attribute has highest precedence, then fallback to [PropertyType]
            if (ExtractMaterialPropertyAttribute(attributes, property, subField, results))
                return true;
            if (ExtractPropertyTypeAttribute(attributes, property, subField, results))
                return true;
            return false;
        }

        static bool ExtractMaterialPropertyAttribute(IEnumerable<ShaderAttribute> attributes, BlockVariable property, StructField subField, List<MaterialPropertyInfo> results)
        {
            var matPropertyAtt = MaterialPropertyAttribute.Find(attributes);
            if (matPropertyAtt == null)
                return false;

            var propInfo = new MaterialPropertyInfo();
            propInfo.DefaultExpression = GetDefaultExpression(property.DefaultExpression, property.Attributes, subField);
            propInfo.declaration = matPropertyAtt.BuildDeclarationString(property.ReferenceName, property.DisplayName);
            results.Add(propInfo);
            return true;
        }

        static bool ExtractPropertyTypeAttribute(IEnumerable<ShaderAttribute> attributes, BlockVariable property, StructField subField, List<MaterialPropertyInfo> results)
        {
            var propertyTypeAtt = PropertyTypeAttribute.Find(attributes);
            if (propertyTypeAtt == null)
                return false;

            var propInfo = new MaterialPropertyInfo();
            propInfo.DefaultExpression = GetDefaultExpression(property.DefaultExpression, property.Attributes, subField);
            propInfo.declaration = $"{property.ReferenceName}(\"{property.DisplayName}\", {propertyTypeAtt.PropertyType})";
            results.Add(propInfo);
            return true;
        }

        static string GetDefaultExpression(string defaultExpression, IEnumerable<ShaderAttribute> instanceAttributes, StructField subField)
        {
            MaterialPropertyDefaultAttribute attribute;
            if(subField.IsValid)
            {
                attribute = MaterialPropertyDefaultAttribute.Find(instanceAttributes, subField.Name);
                // If we didn't find a default on the instance, check the field definition
                if (attribute == null)
                    attribute = MaterialPropertyDefaultAttribute.Find(subField.Attributes);
            }
            else
                attribute = MaterialPropertyDefaultAttribute.Find(instanceAttributes);

            if (attribute != null)
                return attribute.PropertyDefaultExpression;
            return defaultExpression;
        }
    }

    internal static class MaterialPropertyDeclaration
    {
        internal static void Declare(ShaderBuilder builder, BlockProperty variable)
        {
            // Each variable can result in multiple properties due to structs
            var props = MaterialPropertyInfo.Extract(variable);
            foreach (var matProp in props)
            {
                matProp.Declare(builder, variable.ReferenceName);
            }
        }
    }
}
