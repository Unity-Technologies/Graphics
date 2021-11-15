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
            MaterialPropertyDefaultAttribute attribute;
            if(varName == null)
                attribute = MaterialPropertyDefaultAttribute.Find(attributes);
            else
                attribute = MaterialPropertyDefaultAttribute.Find(attributes, varName);

            if (attribute != null)
                return attribute.PropertyDefaultExpression;
            return defaultExpression;
        }
    }

    internal class UniformDeclarationContext
    {
        internal ShaderBuilder PerMaterialBuilder;
        internal ShaderBuilder GlobalBuilder;
    }

    internal class PassPropertyInfo
    {
        internal string uniformDeclaration;
        internal VariableLinkInstance variable;
        internal HLSLDeclaration declarationType;
        internal bool isSubField = false;
        internal string assignmentExpression;

        internal static List<PassPropertyInfo> Extract(BlockVariable property)
        {
            return Extract(property.Type, property.ReferenceName, property.Attributes);
        }

        internal static List<PassPropertyInfo> Extract(VariableLinkInstance property)
        {
            return Extract(property.Type, property.Name, property.Attributes);
        }

        internal static List<PassPropertyInfo> Extract(ShaderType type, string referenceName, IEnumerable<ShaderAttribute> attributes)
        {
            List<PassPropertyInfo> results = new List<PassPropertyInfo>();

            var propertyInstance = new VariableLinkInstance
            {
                Type = type,
                Name = referenceName,
                Container = type.Container,
            };

            var propType = type;
            if (propType.IsStruct)
            {
                foreach (var field in propType.StructFields)
                {
                    ExtractField(field.Type, field.Name, field.Attributes, referenceName, attributes, propertyInstance, results);
                }
            }
            else
            {
                bool extracted = ExtractField(propType, referenceName, attributes, referenceName, attributes, null, results);
                if(!extracted)
                {
                    var info = new PassPropertyInfo();
                    info.uniformDeclaration = $"{propType.Name} {referenceName}";
                    info.declarationType = attributes.GetDeclaration();
                    if (info.declarationType == HLSLDeclaration.HybridPerInstance)
                        info.assignmentExpression = $"# = UNITY_ACCESS_HYBRID_INSTANCED_PROP({referenceName}, {propType.Name})";
                    else
                        info.assignmentExpression = $"# = {referenceName}";
                    info.variable = propertyInstance;
                    results.Add(info);
                }
            }
            return results;
        }

        static bool ExtractField(ShaderType variableType, string variableName, IEnumerable<ShaderAttribute> variableAttributes,
            string propReferenceName, IEnumerable<ShaderAttribute> propInstanceAttributes, VariableLinkInstance owner, List<PassPropertyInfo> results)
        {
            PassPropertyInfo info = null;
            var propVariableAtt = PropertyVariableAttribute.Find(variableAttributes);
            if (propVariableAtt != null)
            {
                if(info == null)
                    info = new PassPropertyInfo();

                info.uniformDeclaration = propVariableAtt.BuildDeclarationString(variableType, propReferenceName);
                var rhsVariableName = propVariableAtt.BuildVariableNameString(propReferenceName);
                var declarationType =  propInstanceAttributes.GetDeclaration();
                if (declarationType == HLSLDeclaration.HybridPerInstance)
                    info.assignmentExpression = $"# = UNITY_ACCESS_HYBRID_INSTANCED_PROP({rhsVariableName}, {variableType.Name})";
                else
                    info.assignmentExpression = $"# = {rhsVariableName}";
            }

            var uniformDeclAttribute = UniformDeclarationAttribute.Find(variableAttributes);
            if (uniformDeclAttribute != null)
            {
                if (info == null)
                    info = new PassPropertyInfo();

                info.uniformDeclaration = uniformDeclAttribute.BuildDeclarationString(variableType, propReferenceName);
                var rhsVariableName = uniformDeclAttribute.BuildVariableNameString(propReferenceName);

                var declarationType =  propInstanceAttributes.GetDeclaration();
                if (declarationType == HLSLDeclaration.HybridPerInstance)
                    info.assignmentExpression = $"# = UNITY_ACCESS_HYBRID_INSTANCED_PROP({rhsVariableName}, {variableType.Name})";
                else
                    info.assignmentExpression = $"# = {rhsVariableName}";
            }

            var defaultValueAtt = DefaultValueAttribute.Find(propInstanceAttributes, variableName);
            if (defaultValueAtt != null)
            {
                if (info == null)
                    info = new PassPropertyInfo();

                info.assignmentExpression = defaultValueAtt.DefaultValue;
            }

            if(info != null)
            {
                info.declarationType = propInstanceAttributes.GetDeclaration();
                info.variable = new VariableLinkInstance { Container = variableType.Container, Type = variableType, Name = variableName, Parent = owner };
                info.isSubField = owner != null;
                results.Add(info);
            }

            return info != null;
        }

        internal void Copy(ShaderFunction.Builder builder, VariableLinkInstance owningVariable)
        {
            string variableDeclaration = null;
            if (isSubField)
            {
                variable.Parent = owningVariable;
                variableDeclaration = variable.GetDeclarationString();
            }
            else
            {
                variableDeclaration = owningVariable.GetDeclarationString();
            }
            if (!string.IsNullOrEmpty(this.assignmentExpression))
            {
                var defaultExpression = this.assignmentExpression.Replace("#", variableDeclaration);
                builder.AddLine($"{defaultExpression};");
            }
        }

        internal void Declare(UniformDeclarationContext context)
        {
            if (declarationType == HLSLDeclaration.DoNotDeclare)
                return;

            var builder = context.PerMaterialBuilder;
            if (declarationType == HLSLDeclaration.Global)
                builder = context.GlobalBuilder;

            if(declarationType == HLSLDeclaration.HybridPerInstance)
            {
                builder.AppendLine("#ifdef UNITY_HYBRID_V1_INSTANCING_ENABLED");
                builder.AddLine($"{uniformDeclaration}_dummy;");
                builder.AppendLine("#else // V2");
                builder.AddLine($"{uniformDeclaration};");
                builder.AppendLine("#endif");
            }
            else
                builder.AddLine($"{uniformDeclaration};");
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

    internal class VaryingVariable
    {
        internal ShaderType Type;
        internal string Name;
    }

    internal class LegacyEntryPoints
    {
        internal BlockInstance vertexDescBlockInstance;
        internal BlockInstance fragmentDescBlockInstance;
        internal List<VaryingVariable> customInterpolants = new List<VaryingVariable>();
    }
}
