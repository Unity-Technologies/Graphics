using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal class AttributeParsing
    {
        public delegate void ParseDelegate(ShaderAttributeParam attributeParam, int parameterIndex);

        internal class ParameterDescription
        {
            public string ParamName;
            public ParseDelegate ParseCallback;

            public ParameterDescription() {}
            public ParameterDescription(string paramName, ParseDelegate parseCallback)
            {
                ParamName = paramName;
                ParseCallback = parseCallback;
            }
        }

        internal class SignatureDescription
        {
            public List<ParameterDescription> ParameterDescriptions;
            public ParseDelegate UnknownParameterCallback;
        }

        static internal void Parse(ShaderAttribute attribute, SignatureDescription signatureDescription)
        {
            var index = 0;
            foreach (var attributeParam in attribute.Parameters)
            {
                var paramDesc = signatureDescription.ParameterDescriptions.Find((p) => (p.ParamName == attributeParam.Name));
                if (paramDesc != null)
                {
                    paramDesc.ParseCallback(attributeParam, index);
                    continue;
                }
                else
                {
                    if (signatureDescription.UnknownParameterCallback == null)
                        ErrorHandling.ReportError($"Unknown parameter {attributeParam.Name} at position {index}.");
                    signatureDescription.UnknownParameterCallback(attributeParam, index);
                }

                ++index;
            }
        }

        public static void StringParseCallback(ShaderAttributeParam attributeParam, int parameterIndex, ref string result)
        {
            result = attributeParam.Value;
        }

        public static void BoolParseCallback(ShaderAttributeParam attributeParam, int parameterIndex, ref bool result)
        {
            if (!bool.TryParse(attributeParam.Value, out bool value))
                ErrorHandling.ReportError($"Parameter {attributeParam.Name} at position {parameterIndex} must be an boolean.");
            result = value;
        }

        public static void IntParseCallback(ShaderAttributeParam attributeParam, int parameterIndex, ref int result)
        {
            if (!int.TryParse(attributeParam.Value, out int value))
                ErrorHandling.ReportError($"Parameter {attributeParam.Name} at position {parameterIndex} must be an integer.");
            result = value;
        }

        public static void IntRangeParseCallback(ShaderAttributeParam attributeParam, int parameterIndex, int rangeMin, int rangeMax, ref int result)
        {
            if (!int.TryParse(attributeParam.Value, out int value))
                ErrorHandling.ReportError($"Parameter {attributeParam.Name} at position {parameterIndex} must be an integer.");
            if (value < rangeMin || rangeMax < value)
                ErrorHandling.ReportError($"Parameter {attributeParam.Name} at position {parameterIndex} must be in the range of [{rangeMin}, {rangeMax}].");
            result = value;
        }

        public static void EnumParseCallback<EnumType>(ShaderAttributeParam attributeParam, int parameterIndex, ref EnumType result) where EnumType : struct, Enum
        {
            if (!Enum.TryParse(attributeParam.Value, out EnumType value))
                ErrorHandling.ReportError($"Parameter {attributeParam.Name} at index {parameterIndex} with value {attributeParam.Value} must be a valid {typeof(EnumType).Name} enum value.");
            result = value;
        }
    }
}
