using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class BoolKeywordPropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public string KeywordName = "_VALUE";
        public string TrueValue = "float3(1, 0, 0)";
        public string FalseValue = "float3(0, 0, 1)";

        public BoolKeywordPropertyBlockBuilder()
        {
            BlockName = "BoolKeywordProperty";
            FieldName = "FieldValue";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "1" };
        }

        public Block Build(ShaderContainer container)
        {
            var attributeBuilder = new BoolKeywordAttribute { KeywordName = KeywordName };
            var attribute = attributeBuilder.Build(container);
            var testData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                FieldType = container._bool,
                FieldName = FieldName,
                PropertyAttribute = PropertyAttribute,
                OutputsAssignmentCallback = (builder, propData) => BuildExpression(builder, propData),
                ExtraAttributes = new List<ShaderAttribute> { attribute },
            };
            var blockBuilder = BlockBuilderUtilities.CreateSimplePropertyBlockBuilder(container, BlockName, testData);
            return blockBuilder.Build();
        }

        void BuildExpression(ShaderBuilder builder, BlockBuilderUtilities.PropertyDeclarationData propData)
        {
            builder.AppendLine($"if (inputs.{FieldName})");
            builder.Indent();
            builder.AppendLine($"outputs.BaseColor = {TrueValue};");
            builder.Deindent();

            builder.AppendLine($"else");
            builder.Indent();
            builder.AppendLine($" outputs.BaseColor = {FalseValue};");
            builder.Deindent();
        }
    }
}
