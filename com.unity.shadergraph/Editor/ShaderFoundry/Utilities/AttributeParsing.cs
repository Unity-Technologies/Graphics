using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal class AttributeParsing
    {
        public delegate void ParseDelegate<TargetType>(ShaderAttributeParam attributeParam, int parameterIndex, TargetType target);

        internal class ParameterDescription<TargetType>
        {
            readonly public string ParamName;
            readonly public ParseDelegate<TargetType> ParseCallback;

            public ParameterDescription() { }
            public ParameterDescription(string paramName, ParseDelegate<TargetType> parseCallback)
            {
                ParamName = paramName;
                ParseCallback = parseCallback;
            }
        }

        internal class SignatureDescription<TargetType>
        {
            public List<ParameterDescription<TargetType>> ParameterDescriptions;
            public ParseDelegate<TargetType> UnknownParameterCallback;
        }

        static internal void Parse<TargetType>(ShaderAttribute attribute, SignatureDescription<TargetType> signatureDescription, TargetType target)
        {
            var index = 0;
            foreach (var attributeParam in attribute.Parameters)
            {
                var paramDesc = signatureDescription.ParameterDescriptions.Find((p) => (p.ParamName == attributeParam.Name));
                if (paramDesc != null)
                {
                    paramDesc.ParseCallback(attributeParam, index, target);
                }
                else
                {
                    if (signatureDescription.UnknownParameterCallback == null)
                        ErrorHandling.ReportError($"Unknown parameter {attributeParam.Name} at position {index}.");
                    signatureDescription.UnknownParameterCallback(attributeParam, index, target);
                }

                ++index;
            }
        }

        public static void ParseString(ShaderAttributeParam attributeParam, int parameterIndex, ref string result)
        {
            result = attributeParam.Value;
        }

        public static void ParseBool(ShaderAttributeParam attributeParam, int parameterIndex, ref bool result)
        {
            if (!bool.TryParse(attributeParam.Value, out bool value))
                ErrorHandling.ReportError($"Parameter {attributeParam.Name} at position {parameterIndex} must be a boolean.");
            result = value;
        }

        public static void ParseInt(ShaderAttributeParam attributeParam, int parameterIndex, ref int result)
        {
            if (!int.TryParse(attributeParam.Value, out int value))
                ErrorHandling.ReportError($"Parameter {attributeParam.Name} at position {parameterIndex} must be an integer.");
            result = value;
        }

        public static void ParseIntRange(ShaderAttributeParam attributeParam, int parameterIndex, int rangeMin, int rangeMax, ref int result)
        {
            if (!int.TryParse(attributeParam.Value, out int value))
                ErrorHandling.ReportError($"Parameter {attributeParam.Name} at position {parameterIndex} must be an integer.");
            if (value < rangeMin || rangeMax < value)
                ErrorHandling.ReportError($"Parameter {attributeParam.Name} at position {parameterIndex} must be in the range of [{rangeMin}, {rangeMax}).");
            result = value;
        }

        public static void ParseEnum<EnumType>(ShaderAttributeParam attributeParam, int parameterIndex, ref EnumType result) where EnumType : struct, Enum
        {
            if (!Enum.TryParse(attributeParam.Value, out EnumType value))
                ErrorHandling.ReportError($"Parameter {attributeParam.Name} at index {parameterIndex} with value {attributeParam.Value} must be a valid {typeof(EnumType).Name} enum value.");
            result = value;
        }

        public static void ReportRequiredParameterIsMissing(string attributeName, string parameterName)
        {
            ErrorHandling.ReportError($"{attributeName}: Required parameter {parameterName} was not found.");
        }
    }
}
