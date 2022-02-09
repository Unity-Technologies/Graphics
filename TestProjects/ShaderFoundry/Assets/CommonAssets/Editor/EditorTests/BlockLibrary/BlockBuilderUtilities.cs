using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class PropertyAttributeData
    {
        public string UniformName;
        public string DisplayName;
        public string DefaultValue;
        public bool? Exposed;
        public UniformDataSource? DataSource;
    }

    class BlockBuilderUtilities
    {
        internal static void MarkAsProperty(ShaderContainer container, StructField.Builder fieldBuilder, PropertyAttributeData propertyAttribute)
        {
            var propAttributeBuilder = new ShaderAttribute.Builder(container, "Property");
            if (!string.IsNullOrEmpty(propertyAttribute.UniformName))
                propAttributeBuilder.Param("uniformName", propertyAttribute.UniformName);
            if (!string.IsNullOrEmpty(propertyAttribute.DisplayName))
                propAttributeBuilder.Param("displayName", propertyAttribute.DisplayName);
            if (!string.IsNullOrEmpty(propertyAttribute.DefaultValue))
                propAttributeBuilder.Param("defaultValue", propertyAttribute.DefaultValue);
            if (propertyAttribute.DataSource is UniformDataSource dataSource)
                propAttributeBuilder.Param("dataSource", dataSource.ToString());
            if (propertyAttribute.Exposed is bool exposed)
                propAttributeBuilder.Param("exposed", exposed.ToString());
            fieldBuilder.AddAttribute(propAttributeBuilder.Build());
        }

        internal class PropertyDeclarationData
        {
            internal string OutputInstanceName = "outputs";
            internal string InputInstanceName = "inputs";

            internal PropertyAttributeData PropertyAttribute;
            internal List<ShaderAttribute> ExtraAttributes;
            internal ShaderType FieldType;
            internal string FieldName;
            internal delegate void OutputsAssignmentDelegate(ShaderFunction.Builder builder, PropertyDeclarationData propData);
            internal OutputsAssignmentDelegate OutputsAssignmentCallback = null;
        }

        internal static Block.Builder CreateSimplePropertyBlockBuilder(ShaderContainer container, string blockName, PropertyDeclarationData propertyData)
        {
            var blockBuilder = new Block.Builder(container, blockName);

            // Build the input type
            var inputTypeBuilder = new ShaderType.StructBuilder(blockBuilder, "Input");
            var fieldName = propertyData.FieldName;
            var inputFieldBuilder = new StructField.Builder(container, fieldName, propertyData.FieldType);
            if(propertyData.PropertyAttribute != null)
                MarkAsProperty(container, inputFieldBuilder, propertyData.PropertyAttribute);

            if(propertyData.ExtraAttributes != null)
            {
                foreach (var attribute in propertyData.ExtraAttributes)
                    inputFieldBuilder.AddAttribute(attribute);
            }
            inputTypeBuilder.AddField(inputFieldBuilder.Build());
            var inputAlphaBuilder = new StructField.Builder(container, "Alpha", container._float);
            inputTypeBuilder.AddField(inputAlphaBuilder.Build());
            var inputType = inputTypeBuilder.Build();

            // Build the output type
            var outputTypeBuilder = new ShaderType.StructBuilder(blockBuilder, "Output");
            var outputBaseColorBuilder = new StructField.Builder(container, "BaseColor", container._float3);
            outputTypeBuilder.AddField(outputBaseColorBuilder.Build());
            var outputAlphaBuilder = new StructField.Builder(container, "Alpha", container._float);
            outputTypeBuilder.AddField(outputAlphaBuilder.Build());
            var outputType = outputTypeBuilder.Build();

            // Build the entry point
            var entryPointFnBuilder = new ShaderFunction.Builder(blockBuilder, "Apply", outputType);
            entryPointFnBuilder.AddInput(inputType, propertyData.InputInstanceName);
            entryPointFnBuilder.AddLine($"{outputType.Name} {propertyData.OutputInstanceName};");
            entryPointFnBuilder.AddLine($"{propertyData.OutputInstanceName}.Alpha = {propertyData.InputInstanceName}.Alpha;");
            if (propertyData.OutputsAssignmentCallback != null)
                propertyData.OutputsAssignmentCallback(entryPointFnBuilder, propertyData);
            entryPointFnBuilder.AddLine($"return {propertyData.OutputInstanceName};");
            var entryPointFn = entryPointFnBuilder.Build();

            // Setup the block
            blockBuilder.AddType(inputType);
            blockBuilder.AddType(outputType);
            blockBuilder.SetEntryPointFunction(entryPointFn);
            return blockBuilder;
        }

        internal static Block CreateSimplePropertyBlock(ShaderContainer container, string blockName, PropertyDeclarationData propertyData)
        {
            return CreateSimplePropertyBlockBuilder(container, blockName, propertyData).Build();
        }
    }
}
