using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal enum KeywordMode
    {
        ShaderFeature,
        MultiCompile,
        DynamicBranch,
    };

    static class KeywordModeExtensions
    {
        public static string ToShaderLabString(this KeywordMode keywordMode)
        {
            switch (keywordMode)
            {
                case KeywordMode.ShaderFeature:
                    return "shader_feature";
                case KeywordMode.MultiCompile:
                    return "multi_compile";
                case KeywordMode.DynamicBranch:
                    return "dynamic_branch";
            }
            throw new Exception("Invalid keyword mode");
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
        internal KeywordMode KeywordMode = KeywordMode.ShaderFeature;
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

        static AttributeParsing.SignatureDescription<BoolKeywordAttribute> AttributeSignature = new AttributeParsing.SignatureDescription<BoolKeywordAttribute>()
        {
            ParameterDescriptions = new List<AttributeParsing.ParameterDescription<BoolKeywordAttribute>>
            {
                new AttributeParsing.ParameterDescription<BoolKeywordAttribute>(KeywordNameParamName, (param, index, target) => AttributeParsing.ParseString(param, index, ref target.KeywordName)),
                new AttributeParsing.ParameterDescription<BoolKeywordAttribute>(KeywordModeParamName, (param, index, target) => AttributeParsing.ParseEnum(param, index, ref target.KeywordMode)),
                new AttributeParsing.ParameterDescription<BoolKeywordAttribute>(ScopeParamName, (param, index, target) => AttributeParsing.ParseString(param, index, ref target.Scope)),
                new AttributeParsing.ParameterDescription<BoolKeywordAttribute>(StageParamName, (param, index, target) => AttributeParsing.ParseString(param, index, ref target.Stage)),
            }
        };

        internal static BoolKeywordAttribute TryParse(ShaderAttribute attribute)
        {
            if (attribute.Name != AttributeName)
                return null;

            var result = new BoolKeywordAttribute();
            AttributeParsing.Parse(attribute, AttributeSignature, result);
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
        internal KeywordMode KeywordMode = KeywordMode.ShaderFeature;
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
            enumPairs.Add(new EnumPair { Name = name, Value = value });
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

        static AttributeParsing.SignatureDescription<EnumKeywordAttribute> AttributeSignature = new AttributeParsing.SignatureDescription<EnumKeywordAttribute>()
        {
            ParameterDescriptions = new List<AttributeParsing.ParameterDescription<EnumKeywordAttribute>>
            {
                new AttributeParsing.ParameterDescription<EnumKeywordAttribute>(KeywordModeParamName, (param, index, target) => AttributeParsing.ParseEnum(param, index, ref target.KeywordMode)),
                new AttributeParsing.ParameterDescription<EnumKeywordAttribute>(AllowsNoneParamName, (param, index, target) => AttributeParsing.ParseBool(param, index, ref target.AllowsNone)),
                new AttributeParsing.ParameterDescription<EnumKeywordAttribute>(ScopeParamName, (param, index, target) => AttributeParsing.ParseString(param, index, ref target.Scope)),
                new AttributeParsing.ParameterDescription<EnumKeywordAttribute>(StageParamName, (param, index, target) => AttributeParsing.ParseString(param, index, ref target.Stage)),
            },
            // Any unknown parameter is an enum entry
            UnknownParameterCallback = (param, index, target) => ParseEnumEntry(param, index, target),
        };

        static void ParseEnumEntry(ShaderAttributeParam param, int index, EnumKeywordAttribute target)
        {
            string enumName;
            int enumValue = 0;

            // If the enum has a name and value then use the value. If the enum doesn't have a name then the value is implicit.
            // Note: attribute parameters of the form [Attribute(value)] are specifying a value, not a name, effectively a positional argument, not named.
            if (!string.IsNullOrEmpty(param.Name))
            {
                enumName = param.Name;
                AttributeParsing.ParseInt(param, index, ref enumValue);
            }
            else
            {
                // For an implicit enum, the value is the last enum's value + 1 (or 0 if there are no values)
                enumName = param.Value;
                if (target.EnumCount == 0)
                    enumValue = 0;
                else
                    enumValue = target.GetEnum(target.EnumCount - 1).Value + 1;
            }

            target.AddEnum(enumName, enumValue);
        }

        internal static EnumKeywordAttribute TryParse(ShaderAttribute attribute)
        {
            if (attribute.Name != AttributeName)
                return null;

            var result = new EnumKeywordAttribute();
            AttributeParsing.Parse(attribute, AttributeSignature, result);
            return result;
        }
    }
}
