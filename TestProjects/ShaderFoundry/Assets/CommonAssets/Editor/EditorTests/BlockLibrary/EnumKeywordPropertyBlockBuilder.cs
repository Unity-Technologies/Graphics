using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class EnumKeywordPropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public class EnumEntry
        {
            public string Name;
            public string Value;
            public string ResultValue;
        }
        public List<EnumEntry> EnumEntries;

        public EnumKeywordPropertyBlockBuilder()
        {
            BlockName = "EnumKeywordProperty";
            FieldName = "FieldValue";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "0" };
        }

        public Block Build(ShaderContainer container)
        {
            var enumKeywordAttributeBuilder = new EnumKeywordAttribute();
            foreach (var entry in EnumEntries)
            {
                if (int.TryParse(entry.Value, out int value))
                    enumKeywordAttributeBuilder.AddEnum(entry.Name, value);
                else
                    enumKeywordAttributeBuilder.AddEnum(entry.Name);
            }
            var keywordEnumAttribute = enumKeywordAttributeBuilder.Build(container);

            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                PropertyAttribute = PropertyAttribute,
                FieldType = container._int,
                FieldName = FieldName,
                OutputsAssignmentCallback = (builder, propData) => BuildExpression(builder, propData),
                ExtraAttributes = new List<ShaderAttribute> { keywordEnumAttribute },
            };
            var blockBuilder = BlockBuilderUtilities.CreateSimplePropertyBlockBuilder(container, BlockName, propData);
            return blockBuilder.Build();
        }

        public string GetKeywordName(string enumValueName)
        {
            return $"{PropertyAttribute.UniformName}_{enumValueName}".ToUpperInvariant();
        }

        void BuildExpression(ShaderBuilder builder, BlockBuilderUtilities.PropertyDeclarationData propData)
        {
            var index = 0;
            foreach (var entry in EnumEntries)
            {
                var keywordName = GetKeywordName(entry.Name);
                var entryValue = entry.Value ?? index.ToString();
                if (index == 0)
                    builder.AppendLine($"if (inputs.{FieldName} == {entryValue})");
                else
                    builder.AppendLine($"else if (inputs.{FieldName} == {entryValue})");
                builder.Indent();
                builder.AppendLine($"outputs.BaseColor = {entry.ResultValue};");
                builder.Deindent();
                ++index;
            }
        }
    }
}
