using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal class UniformDeclarationContext
    {
        internal ShaderBuilder PerMaterialBuilder;
        internal ShaderBuilder GlobalBuilder;
    }

    internal class UniformInfo
    {
        internal string uniformDeclaration;
        internal HLSLDeclaration declarationType;
        internal bool isSubField = false;
        // The expression to access the uniform. Used for assignment
        internal string assignmentExpression;
        // If this uniform is a sub field, this is the access chains to get to the sub-field, not including the root.
        // This doesn't include the root as we may need to access this off a renamed field.
        internal string baseAccessChain;

        internal static List<UniformInfo> Extract(BlockVariable property)
        {
            return Extract(property.Type, property.Name, property.Attributes);
        }

        internal static List<UniformInfo> Extract(VariableLinkInstance property)
        {
            return Extract(property.Type, property.Name, property.Attributes);
        }

        internal static List<UniformInfo> Extract(ShaderType type, string name, IEnumerable<ShaderAttribute> attributes)
        {
            List<UniformInfo> results = new List<UniformInfo>();

            if (type.IsStruct)
            {
                foreach (var field in type.StructFields)
                    ExtractField(field.Type, field.Name, field.Attributes, name, attributes, true, results);
            }
            else
            {
                bool extracted = ExtractField(type, name, attributes, name, attributes, false, results);
                if(!extracted)
                {
                    var info = new UniformInfo();
                    info.uniformDeclaration = $"{type.Name} {name}";
                    info.declarationType = attributes.GetDeclaration();
                    info.assignmentExpression = BuildAssignmentExpression(type, name, info.declarationType);
                    results.Add(info);
                }
            }
            return results;
        }

        static bool ExtractField(ShaderType variableType, string variableName, IEnumerable<ShaderAttribute> variableAttributes,
            string propReferenceName, IEnumerable<ShaderAttribute> propInstanceAttributes, bool isSubField, List<UniformInfo> results)
        {
            // Check for the declaration type, first with the instance variable then the field
            HLSLDeclaration declarationType;
            if (!propInstanceAttributes.GetDeclaration(out declarationType))
                variableAttributes.GetDeclaration(out declarationType);

            UniformInfo info = null;
            ExtractPropertyVariableAttribute(variableAttributes, variableType, propReferenceName, declarationType, ref info);
            ExtractUniformDeclarationAttribute(variableAttributes, variableType, propReferenceName, declarationType, ref info);
            // If we didn't build a uniform, try extracting a default value for this field.
            // This is so a struct where one field is a uniform but another has a default value will work correctly.
            if(info == null)
                ExtractDefaultValueAttribute(propInstanceAttributes, variableAttributes, variableName, propReferenceName, ref info);

            if(info != null)
            {
                info.declarationType = declarationType;
                info.isSubField = isSubField;
                if(isSubField)
                    info.baseAccessChain = variableName;
                results.Add(info);
            }

            return info != null;
        }

        static bool ExtractUniformDeclarationAttribute(IEnumerable<ShaderAttribute> variableAttributes, ShaderType variableType, string variableName,
            HLSLDeclaration declarationType, ref UniformInfo info)
        {
            var uniformDeclAttribute = UniformDeclarationAttribute.Find(variableAttributes);
            if (uniformDeclAttribute == null)
                return false;

            if (info == null)
                info = new UniformInfo();

            info.uniformDeclaration = uniformDeclAttribute.BuildDeclarationString(variableType, variableName);
            var rhsVariableName = uniformDeclAttribute.BuildVariableNameString(variableName);
            info.assignmentExpression = BuildAssignmentExpression(variableType, rhsVariableName, declarationType);
            return true;
        }

        static bool ExtractPropertyVariableAttribute(IEnumerable<ShaderAttribute> variableAttributes, ShaderType variableType, string variableName,
            HLSLDeclaration declarationType, ref UniformInfo info)
        {
            var propVariableAtt = PropertyVariableAttribute.Find(variableAttributes);
            if (propVariableAtt == null)
                return false;

            if (info == null)
                info = new UniformInfo();

            info.uniformDeclaration = propVariableAtt.BuildDeclarationString(variableType, variableName);
            var rhsVariableName = propVariableAtt.BuildVariableNameString(variableName);
            info.assignmentExpression = BuildAssignmentExpression(variableType, rhsVariableName, declarationType);
            return true;
        }

        static bool ExtractDefaultValueAttribute(IEnumerable<ShaderAttribute> instanceAttributes, IEnumerable<ShaderAttribute> fieldAttributes, string variableName, string propReferenceName, ref UniformInfo info)
        {
            var defaultValueAtt = DefaultValueAttribute.Find(instanceAttributes, variableName);
            if (defaultValueAtt == null)
                defaultValueAtt = DefaultValueAttribute.Find(fieldAttributes);
            if (defaultValueAtt == null)
                return false;

            if (info == null)
                info = new UniformInfo();

            info.assignmentExpression = $"# = {defaultValueAtt.DefaultValue.Replace("#", propReferenceName)}";
            return true;
        }

        static string BuildAssignmentExpression(ShaderType type, string rhsExpression, HLSLDeclaration declarationType)
        {
            if (declarationType == HLSLDeclaration.HybridPerInstance)
                return $"# = UNITY_ACCESS_HYBRID_INSTANCED_PROP({rhsExpression}, {type.Name})";
            else
                return $"# = {rhsExpression}";
        }

        internal void Copy(ShaderFunction.Builder builder, VariableLinkInstance owningVariable)
        {
            string variableDeclaration = owningVariable.GetDeclarationString();
            if (isSubField)
                variableDeclaration = $"{variableDeclaration}.{baseAccessChain}";

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

    internal static class UniformDeclaration
    {
        internal static void Copy(ShaderFunction.Builder builder, VariableLinkInstance variable, VariableLinkInstance parent)
        {
            var passProps = UniformInfo.Extract(variable);
            foreach (var passProp in passProps)
            {
                passProp.Copy(builder, parent);
            }
        }

        internal static void Declare(UniformDeclarationContext context, BlockVariable variable)
        {
            var passProps = UniformInfo.Extract(variable);
            foreach (var passProp in passProps)
            {
                passProp.Declare(context);
            }
        }
    }
}
