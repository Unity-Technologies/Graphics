using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal class SamplerStateAttribute
    {
        public enum FilterModeEnum
        {
            Point,
            Linear,
            Trilinear
        }
        public enum WrapModeParameterStates
        {
            Clamp, ClampU, ClampV, ClampW,
            Repeat, RepeatU, RepeatV, RepeatW,
            Mirror, MirrorU, MirrorV, MirrorW,
            MirrorOnce, MirrorOnceU, MirrorOnceV, MirrorOnceW
        }
        public enum WrapModeEnum
        {
            Clamp, Repeat, Mirror, MirrorOnce
        }

        internal const char WrapModeDelimeter = ',';
        internal const string AttributeName = "SamplerState";
        internal const string FilterModeParamName = "filterMode";
        internal const string WrapModeParamName = "wrapMode";
        internal const string DepthCompareParamName = "depthCompare";
        internal const string AnisotropicLevelParamName = "anisotropicLevel";

        public FilterModeEnum FilterMode = FilterModeEnum.Linear;
        public WrapModeEnum WrapModeU = WrapModeEnum.Repeat;
        public WrapModeEnum WrapModeV = WrapModeEnum.Repeat;
        public WrapModeEnum WrapModeW = WrapModeEnum.Repeat;
        public bool DepthCompare = false;
        public int AnisotropicLevel = 0;

        internal static SamplerStateAttribute FindFirst(IEnumerable<ShaderAttribute> attributes)
        {
            var attribute = attributes.FindFirst(AttributeName);
            if (attribute.IsValid)
                return TryParse(attribute);
            return null;
        }

        static AttributeParsing.SignatureDescription<SamplerStateAttribute> AttributeSignature = new AttributeParsing.SignatureDescription<SamplerStateAttribute>()
        {
            ParameterDescriptions = new List<AttributeParsing.ParameterDescription<SamplerStateAttribute>>
            {
                new AttributeParsing.ParameterDescription<SamplerStateAttribute>(FilterModeParamName, (param, index, target) => AttributeParsing.ParseEnum<FilterModeEnum>(param, index, ref target.FilterMode)),
                new AttributeParsing.ParameterDescription<SamplerStateAttribute>(WrapModeParamName, (param, index, target) => ParseWrapMode(param, index, ref target)),
                new AttributeParsing.ParameterDescription<SamplerStateAttribute>(DepthCompareParamName, (param, index, target) => AttributeParsing.ParseBool(param, index, ref target.DepthCompare)),
                new AttributeParsing.ParameterDescription<SamplerStateAttribute>(AnisotropicLevelParamName, (param, index, target) => ParseAnisotropicLevel(param, index, ref target.AnisotropicLevel)),
            },
        };

        static void ParseWrapMode(ShaderAttributeParam attributeParam, int parameterIndex, ref SamplerStateAttribute target)
        {
            var tokens = attributeParam.Value.Split(WrapModeDelimeter);
            foreach (var token in tokens)
            {
                if (!Enum.TryParse(token, out WrapModeParameterStates enumValue))
                {
                    ErrorHandling.ReportError($"Parameter {attributeParam.Name} at index {parameterIndex} with value {token} must be a valid {typeof(WrapModeParameterStates).Name} enum value.");
                    continue;
                }
                switch (enumValue)
                {
                    case WrapModeParameterStates.Clamp:
                        target.WrapModeU = WrapModeEnum.Clamp;
                        target.WrapModeV = WrapModeEnum.Clamp;
                        target.WrapModeW = WrapModeEnum.Clamp;
                        break;
                    case WrapModeParameterStates.ClampU:
                        target.WrapModeU = WrapModeEnum.Clamp;
                        break;
                    case WrapModeParameterStates.ClampV:
                        target.WrapModeV = WrapModeEnum.Clamp;
                        break;
                    case WrapModeParameterStates.ClampW:
                        target.WrapModeW = WrapModeEnum.Clamp;
                        break;
                    case WrapModeParameterStates.Repeat:
                        target.WrapModeU = WrapModeEnum.Repeat;
                        target.WrapModeV = WrapModeEnum.Repeat;
                        target.WrapModeW = WrapModeEnum.Repeat;
                        break;
                    case WrapModeParameterStates.RepeatU:
                        target.WrapModeU = WrapModeEnum.Repeat;
                        break;
                    case WrapModeParameterStates.RepeatV:
                        target.WrapModeV = WrapModeEnum.Repeat;
                        break;
                    case WrapModeParameterStates.RepeatW:
                        target.WrapModeW = WrapModeEnum.Repeat;
                        break;
                    case WrapModeParameterStates.Mirror:
                        target.WrapModeU = WrapModeEnum.Mirror;
                        target.WrapModeV = WrapModeEnum.Mirror;
                        target.WrapModeW = WrapModeEnum.Mirror;
                        break;
                    case WrapModeParameterStates.MirrorU:
                        target.WrapModeU = WrapModeEnum.Mirror;
                        break;
                    case WrapModeParameterStates.MirrorV:
                        target.WrapModeV = WrapModeEnum.Mirror;
                        break;
                    case WrapModeParameterStates.MirrorW:
                        target.WrapModeW = WrapModeEnum.Mirror;
                        break;
                    case WrapModeParameterStates.MirrorOnce:
                        target.WrapModeU = WrapModeEnum.MirrorOnce;
                        target.WrapModeV = WrapModeEnum.MirrorOnce;
                        target.WrapModeW = WrapModeEnum.MirrorOnce;
                        break;
                    case WrapModeParameterStates.MirrorOnceU:
                        target.WrapModeU = WrapModeEnum.MirrorOnce;
                        break;
                    case WrapModeParameterStates.MirrorOnceV:
                        target.WrapModeV = WrapModeEnum.MirrorOnce;
                        break;
                    case WrapModeParameterStates.MirrorOnceW:
                        target.WrapModeW = WrapModeEnum.MirrorOnce;
                        break;
                }
            }
        }

        static void ParseAnisotropicLevel(ShaderAttributeParam attributeParam, int parameterIndex, ref int result)
        {
            AttributeParsing.ParseInt(attributeParam, parameterIndex, ref result);
            switch (result)
            {
                case 2: break;
                case 4: break;
                case 8: break;
                case 16: break;
                default:
                    ErrorHandling.ReportError($"Parameter {attributeParam.Name} at position {parameterIndex} with value {result} must be an integer value of 2, 4, 8, or 16.");
                    return;
            }
        }

        internal static SamplerStateAttribute TryParse(ShaderAttribute attribute)
        {
            if (attribute.Name != AttributeName)
                return null;

            var result = new SamplerStateAttribute();
            AttributeParsing.Parse(attribute, AttributeSignature, result);
            return result;
        }

        public string BuildUniformName(string baseUniformName)
        {
            var uniformNameBuilder = new ShaderBuilder();
            uniformNameBuilder.Add(baseUniformName);
            uniformNameBuilder.Add($"_{FilterMode}");

            AppendWrapModeName(uniformNameBuilder, WrapModeU, WrapModeV, WrapModeW);

            if (DepthCompare)
                uniformNameBuilder.Add($"_compare");

            if (AnisotropicLevel != 0)
                uniformNameBuilder.Add($"_aniso{AnisotropicLevel}");

            return uniformNameBuilder.ToString();
        }

        static void AppendWrapModeName(ShaderBuilder builder, WrapModeEnum wrapModeU, WrapModeEnum wrapModeV, WrapModeEnum wrapModeW)
        {
            bool allEqual = wrapModeU == wrapModeV && wrapModeU == wrapModeW;
            // Handle all states being equal
            if (allEqual)
                builder.Add($"_{wrapModeU}");
            // Handle two states being equal
            else if (wrapModeU == wrapModeV)
            {
                builder.Add($"_{wrapModeU}");
                builder.Add($"_{wrapModeW}W");
            }
            else if (wrapModeU == wrapModeW)
            {
                builder.Add($"_{wrapModeU}");
                builder.Add($"_{wrapModeV}V");
            }
            else if (wrapModeV == wrapModeW)
            {
                builder.Add($"_{wrapModeV}");
                builder.Add($"_{wrapModeU}U");
            }
            // Handle all of the states being different
            else
            {
                builder.Add($"_{wrapModeU}U");
                builder.Add($"_{wrapModeV}V");
                builder.Add($"_{wrapModeW}W");
            }
        }

        internal static string BuildWrapModeParameterValue(IEnumerable<WrapModeParameterStates> wrapModes)
        {
            var wrapModeBuilder = new ShaderBuilder();
            var first = true;
            foreach (var wrapMode in wrapModes)
            {
                if (!first)
                    wrapModeBuilder.Add(WrapModeDelimeter.ToString());
                else
                    first = false;
                wrapModeBuilder.Add(wrapMode.ToString());
            }
            return wrapModeBuilder.ToString();
        }
    }
}
