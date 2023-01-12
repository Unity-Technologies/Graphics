using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal class RangeAttribute
    {
        internal const string AttributeName = "Range";
        internal const string MinParamName = "min";
        internal const string MaxParamName = "max";

        internal float Min = float.MinValue;
        internal float Max = float.MinValue;

        internal ShaderAttribute Build(ShaderContainer container)
        {
            var attributeBuilder = new ShaderAttribute.Builder(container, AttributeName);
            attributeBuilder.Param(Min.ToString());
            attributeBuilder.Param(Max.ToString());
            return attributeBuilder.Build();
        }

        internal string GetDisplayTypeString()
        {
            return $"Range({Min}, {Max})";
        }

        internal static RangeAttribute FindFirst(IEnumerable<ShaderAttribute> attributes)
        {
            var attribute = attributes.FindFirst(AttributeName);
            if (attribute.IsValid)
                return TryParse(attribute);
            return null;
        }

        static AttributeParsing.SignatureDescription<RangeAttribute> AttributeSignature = new AttributeParsing.SignatureDescription<RangeAttribute>()
        {
            ParameterDescriptions = new List<AttributeParsing.ParameterDescription<RangeAttribute>>
            {
                new AttributeParsing.ParameterDescription<RangeAttribute>(MinParamName, (param, index, target) => AttributeParsing.ParseFloat(param, index, ref target.Min)),
                new AttributeParsing.ParameterDescription<RangeAttribute>(MaxParamName, (param, index, target) => AttributeParsing.ParseFloat(param, index, ref target.Max)),
            },
            UnknownParameterCallback = (param, index, target) =>
            {
                if (index == 0)
                    AttributeParsing.ParseFloat(param, index, ref target.Min);
                else if (index == 1)
                    AttributeParsing.ParseFloat(param, index, ref target.Max);
                else
                    ErrorHandling.ReportError($"Attribute {AttributeName} must be of the signature (float min, float max).");
            }
        };

        internal static RangeAttribute TryParse(ShaderAttribute attribute)
        {
            if (attribute.Name != AttributeName)
                return null;

            var result = new RangeAttribute();
            AttributeParsing.Parse(attribute, AttributeSignature, result);
            // TODO @ SHADERS: Ideally make require parameters part of the AttributeParsing utility.
            if (result.Min == float.MinValue)
                AttributeParsing.ReportRequiredParameterIsMissing(AttributeName, MinParamName);
            if (result.Max == float.MinValue)
                AttributeParsing.ReportRequiredParameterIsMissing(AttributeName, MaxParamName);
            return result;
        }
    }
}
