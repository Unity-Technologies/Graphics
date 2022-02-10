using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal enum KeywordMode
    {
        shader_feature,
        multi_compile,
        dynamic_branch,
    };

    static class KeywordModeExtensions
    {
        public static string ToShaderLabString(this KeywordMode keywordMode)
        {
            return keywordMode.ToString();
        }
    }

    internal class BoolKeywordAttribute
    {
        const string AttributeName = "BoolKeyword";
        const string KeywordNameParamName = "keywordName";
        const string KeywordModeParamName = "keywordMode";
        const string ScopeParamName = "scope";
        const string StageParamName = "stage";

        internal string KeywordName;
        internal KeywordMode KeywordMode = KeywordMode.shader_feature;
        internal string Scope = "local";
        internal string Stage = "all";

        public string GetKeywordName(string uniformName)
        {
            return (KeywordName ?? uniformName).ToUpperInvariant();
        }

        public KeywordDescriptor BuildDescriptor(ShaderContainer container, string uniformName)
        {
            var definition = KeywordMode.ToShaderLabString();
            var stage = Stage == "all" ? null : Stage;
            var ops = new string[] { GetKeywordName(uniformName) };
            var builder = new KeywordDescriptor.Builder(container, "_", definition, Scope, stage, ops);
            return builder.Build();
        }

        internal ShaderAttribute Build(ShaderContainer container)
        {
            var attributeBuilder = new ShaderAttribute.Builder(container, AttributeName);
            if (KeywordName != null)
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, KeywordNameParamName, KeywordName).Build());
            if (Scope != null)
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, ScopeParamName, Scope).Build());
            if (Stage != null)
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, StageParamName, Stage).Build());
            attributeBuilder.Param(new ShaderAttributeParam.Builder(container, KeywordModeParamName, KeywordMode.ToString()).Build());
            return attributeBuilder.Build();
        }

        internal static BoolKeywordAttribute FindFirst(IEnumerable<ShaderAttribute> attributes)
        {
            var attribute = attributes.FindFirst(AttributeName);
            if (attribute.IsValid)
                return TryParse(attribute);
            return null;
        }

        internal static BoolKeywordAttribute TryParse(ShaderAttribute attribute)
        {
            if (attribute.Name != AttributeName)
                return null;

            var result = new BoolKeywordAttribute();

            var signature = new AttributeParsing.SignatureDescription();
            signature.ParameterDescriptions = new List<AttributeParsing.ParameterDescription>
            {
                new AttributeParsing.ParameterDescription(KeywordNameParamName, (param, index) => AttributeParsing.StringParseCallback(param, index, ref result.KeywordName)),
                new AttributeParsing.ParameterDescription(KeywordModeParamName, (param, index) => AttributeParsing.EnumParseCallback(param, index, ref result.KeywordMode) ),
                new AttributeParsing.ParameterDescription(ScopeParamName, (param, index) => AttributeParsing.StringParseCallback(param, index, ref result.Scope)),
                new AttributeParsing.ParameterDescription(StageParamName, (param, index) => AttributeParsing.StringParseCallback(param, index, ref result.Stage)),
            };
            AttributeParsing.Parse(attribute, signature);

            return result;
        }
    }

    internal class EnumKeywordAttribute
    {
        const string AttributeName = "EnumKeyword";
        const string KeywordModeParamName = "keywordMode";
        const string AllowsNoneParamName = "allowsNone";
        const string ScopeParamName = "scope";
        const string StageParamName = "stage";

        public class EnumPair
        {
            public string Name;
            public int Value;
        }

        List<EnumPair> enumPairs = new List<EnumPair>();
        internal IEnumerable<EnumPair> EnumPairs => enumPairs;

        internal bool AllowsNone = false;
        internal KeywordMode KeywordMode = KeywordMode.shader_feature;
        internal string Scope = "local";
        internal string Stage = "all";

        internal int EnumCount => enumPairs.Count;

        // Adds a new enum name. The value of the enum will be the previous enum value + 1.
        public void AddEnum(string name)
        {
            var enumValue = 0;
            if (enumPairs.Count != 0)
                enumValue = enumPairs[enumPairs.Count - 1].Value + 1;
            AddEnum(name, enumValue);
        }

        public void AddEnum(string name, int value)
        {
            enumPairs.Add(new EnumPair {Name = name, Value = value });
        }

        public EnumPair GetEnum(int index) => enumPairs[index];

        public string GetKeywordName(string uniformName, string enumValueName)
        {
            return $"{uniformName}_{enumValueName}".ToUpperInvariant();
        }

        public KeywordDescriptor BuildDescriptor(ShaderContainer container, string uniformName)
        {
            var definition = KeywordMode.ToShaderLabString();
            var stage = Stage == "all" ? null : Stage;

            var keywordOps = new List<string>();
            if (AllowsNone)
                keywordOps.Add("_");
            foreach (var pair in enumPairs)
                keywordOps.Add(GetKeywordName(uniformName, pair.Name));

            var keywordDescriptorBuilder = new ShaderFoundry.KeywordDescriptor.Builder(container, null, definition, Scope, stage, keywordOps);
            return keywordDescriptorBuilder.Build();
        }

        internal ShaderAttribute Build(ShaderContainer container)
        {
            if (enumPairs == null)
                return ShaderAttribute.Invalid;

            var attributeBuilder = new ShaderAttribute.Builder(container, AttributeName);
            foreach (var pair in enumPairs)
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, pair.Name, pair.Value.ToString()).Build());
            attributeBuilder.Param(new ShaderAttributeParam.Builder(container, KeywordModeParamName, KeywordMode.ToString()).Build());
            if (AllowsNone == false)
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, AllowsNoneParamName, AllowsNone.ToString()).Build());
            if (Scope != null)
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, ScopeParamName, Scope).Build());
            if (Stage != null)
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, StageParamName, Stage).Build());
            return attributeBuilder.Build();
        }

        internal string BuildPropertyAttributeString()
        {
            var attributeStringBuilder = new ShaderBuilder();
            attributeStringBuilder.Append("[KeywordEnum(");
            for (var i = 0; i < enumPairs.Count; ++i)
            {
                if (i != 0)
                    attributeStringBuilder.Append(", ");
                attributeStringBuilder.Append(enumPairs[i].Name);
            }
            attributeStringBuilder.Append(")]");
            return attributeStringBuilder.ToString();
        }

        internal static EnumKeywordAttribute FindFirst(IEnumerable<ShaderAttribute> attributes)
        {
            var attribute = attributes.FindFirst(AttributeName);
            if (attribute.IsValid)
                return TryParse(attribute);
            return null;
        }

        internal static EnumKeywordAttribute TryParse(ShaderAttribute attribute)
        {
            if (attribute.Name != AttributeName)
                return null;

            var currentNameValue = 0;
            var result = new EnumKeywordAttribute();

            var signature = new AttributeParsing.SignatureDescription();
            signature.ParameterDescriptions = new List<AttributeParsing.ParameterDescription>
            {
                new AttributeParsing.ParameterDescription(KeywordModeParamName, (param, index) => AttributeParsing.EnumParseCallback(param, index, ref result.KeywordMode) ),
                new AttributeParsing.ParameterDescription(AllowsNoneParamName, (param, index) => AttributeParsing.BoolParseCallback(param, index, ref result.AllowsNone)),
                new AttributeParsing.ParameterDescription(ScopeParamName, (param, index) => AttributeParsing.StringParseCallback(param, index, ref result.Scope)),
                new AttributeParsing.ParameterDescription(StageParamName, (param, index) => AttributeParsing.StringParseCallback(param, index, ref result.Stage)),
            };
            // Any unknown parameter is an enum value
            signature.UnknownParameterCallback = (param, index) =>
            {
                string enumName;
                int enumValue = currentNameValue;

                // If the enum has a name and value then use the value. If the enum doesn't have a name then the value is implicit.
                // Note: attribute parameters of the form [Attribute(value)] are specifying a value, not a name, effectively a positional argument, not named.
                if (!string.IsNullOrEmpty(param.Name))
                {
                    enumName = param.Name;
                    if (!int.TryParse(param.Value, out enumValue))
                        throw new Exception($"Param {param.Name} value {param.Value} is not a valid integer.");
                }
                else
                    enumName = param.Value;

                result.AddEnum(enumName, enumValue);
                ++currentNameValue;
            };

            AttributeParsing.Parse(attribute, signature);

            return result;
        }
    }
}
