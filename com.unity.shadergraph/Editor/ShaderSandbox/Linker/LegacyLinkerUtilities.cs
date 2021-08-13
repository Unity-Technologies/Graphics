using System.Collections.Generic;
using ShaderSandbox;
using UnityEditor.ShaderGraph.Internal;

using UnityEditor.ShaderSandbox;
using BlockProperty = UnityEditor.ShaderSandbox.BlockVariable;

namespace ShaderSandbox
{
    internal class MaterialPropertyInfo
    {
        internal string DefaultExpression;
        internal string declaration;

        internal void Declare(ShaderBuilder builder, string propertyName)
        {
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
                {
                    var matPropertyAtt = MaterialPropertyAttribute.Find(field.Attributes);
                    if (matPropertyAtt != null)
                    {
                        var propInfo = new MaterialPropertyInfo();
                        propInfo.declaration = matPropertyAtt.BuildDeclarationString(property.ReferenceName, property.DisplayName);
                        propInfo.DefaultExpression = GetDefaultExpression(property.DefaultExpression, property.Attributes, field.Name);
                        results.Add(propInfo);
                    }
                }
            }
            else
            {
                var matPropertyAtt = MaterialPropertyAttribute.Find(property.Attributes);
                var propertyTypeAtt = PropertyTypeAttribute.Find(property.Attributes);
                 // If the property field itself is marked as a material property
                if (matPropertyAtt != null)
                {
                    var propInfo = new MaterialPropertyInfo();
                    propInfo.DefaultExpression = GetDefaultExpression(property.DefaultExpression, property.Attributes);
                    propInfo.declaration = matPropertyAtt.BuildDeclarationString(property.ReferenceName, property.DisplayName);
                    results.Add(propInfo);
                }
                // If this has the property type attribute (to handle vectors, etc...)
                else if (propertyTypeAtt != null)
                {
                    var defaultExpression = GetDefaultExpression(property.DefaultExpression, property.Attributes);
                    var propInfo = new MaterialPropertyInfo();
                    propInfo.DefaultExpression = defaultExpression;
                    propInfo.declaration = $"{property.ReferenceName}(\"{property.DisplayName}\", {propertyTypeAtt.PropertyType})";
                    results.Add(propInfo);
                }
            }
       
            return results;
        }

        static string GetDefaultExpression(string defaultExpression, IEnumerable<ShaderAttribute> attributes, string varName = null)
        {
            DefaultValueAttribute attribute;
            if(varName == null)
                attribute = DefaultValueAttribute.Find(attributes);
            else
                attribute = DefaultValueAttribute.Find(attributes, varName);

            if (attribute != null)
                return attribute.DefaultValue;
            return defaultExpression;
        }
    }

    internal class PassPropertyInfo
    {
        internal string declaredProperty;
        internal BlockVariableLinkInstance variable;
        internal HLSLDeclaration declarationType;
        internal bool isSubField = false;

        internal static void Extract(BlockVariable property, List<PassPropertyInfo> results)
        {
            results.AddRange(Extract(property.Type, property.ReferenceName, property.Attributes));
        }

        internal static List<PassPropertyInfo> Extract(BlockVariable property)
        {
            return Extract(property.Type, property.ReferenceName, property.Attributes);
        }

        internal static List<PassPropertyInfo> Extract(ShaderType type, string referenceName, IEnumerable<ShaderAttribute> attributes)
        {
            List<PassPropertyInfo> results = new List<PassPropertyInfo>();

            var propertyInstance = BlockVariableLinkInstance.Construct(type, referenceName, referenceName, null, null);

            var propType = type;
            if (propType.IsStruct)
            {
                foreach (var field in propType.StructFields)
                {
                    var propVariableAtt = PropertyVariableAttribute.Find(field.Attributes);
                    if (propVariableAtt != null)
                    {
                        var info = new PassPropertyInfo();
                        info.declaredProperty = propVariableAtt.BuildDeclarationString(referenceName);
                        info.declarationType = attributes.GetDeclaration();
                        info.variable = BlockVariableLinkInstance.Construct(field.Type, field.Name, field.Name, propertyInstance, null);
                        info.isSubField = true;
                        results.Add(info);
                    }
                }
            }
            else
            {
                var info = new PassPropertyInfo();
                info.declaredProperty = referenceName;
                info.declarationType = attributes.GetDeclaration();
                info.variable = propertyInstance;
                results.Add(info);
            }
            return results;
        }

        internal void Copy(ShaderFunction.Builder builder, BlockVariableLinkInstance owningVariable)
        {
            if(isSubField)
            {
                variable.Owner = owningVariable;
                var stringBuilder = new ShaderBuilder();
                variable.Declare(stringBuilder);
                builder.AddLine($"{stringBuilder} = {declaredProperty};");
            }
            else
            {
                var stringBuilder = new ShaderBuilder();
                owningVariable.Declare(stringBuilder);
                builder.AddLine($"{stringBuilder} = {declaredProperty};");
            }
        }

        internal void Declare(ShaderBuilder perMaterialBuilder, ShaderBuilder globalBuilder, string propertyName)
        {
            if (declarationType == HLSLDeclaration.DoNotDeclare)
                return;

            var builder = perMaterialBuilder;
            if (declarationType == HLSLDeclaration.Global)
                builder = globalBuilder;

            builder.AddLine($"{variable.Type.Name} {declaredProperty};");
        }
    }

    static class LegacyCustomizationPoints
    {
        internal const string VertexDescriptionCPName = "VertexDescription";
        internal const string SurfaceDescriptionCPName = "SurfaceDescription";

        internal const string VertexDescriptionFunctionName = "VertexDescriptionFunction";
        internal const string SurfaceDescriptionFunctionName = "SurfaceDescriptionFunction";

        internal const string VertexEntryPointInputName = "VertexDescriptionInputs";
        internal const string VertexEntryPointOutputName = "VertexDescription";

        internal const string SurfaceEntryPointInputName = "SurfaceDescriptionInputs";
        internal const string SurfaceEntryPointOutputName = "SurfaceDescription";
    }

    internal class LegacyEntryPoints
    {
        internal BlockDescriptor vertexDescBlockDesc;
        internal BlockDescriptor fragmentDescBlockDesc;
    }

    internal static class TypeUtilities
    {
        internal static ShaderType.StructBuilder BuildStructBuilder(string typeName, IEnumerable<BlockVariableLinkInstance> variables)
        {
            var builder = new ShaderType.StructBuilder(typeName);
            foreach (var variable in variables)
                builder.AddField(variable.Type, variable.ReferenceName);

            return builder;
        }

        internal static ShaderType BuildType(ShaderContainer container, string typeName, IEnumerable<BlockVariable> variables)
        {
            var builder = new ShaderType.StructBuilder(typeName);
            foreach (var variable in variables)
                builder.AddField(variable.Type, variable.ReferenceName);
            return builder.Build(container);
        }
    }
}
