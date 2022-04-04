using System;
using System.Collections.Generic;
using UnityEngine;

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
            // Handles emitting a warning if a value was specified twice.
            void UpdateWrapMode(ref WrapModeEnum? wrapMode, WrapModeEnum value, WrapModeParameterStates paramValue)
            {
                if(wrapMode != null)
                    Debug.Log($"Wrap mode '{paramValue}' will override state '{wrapMode.Value}'.");
                wrapMode = value;
            }
            WrapModeEnum? wrapModeUVW = null;
            WrapModeEnum? wrapModeU = null;
            WrapModeEnum? wrapModeV = null;
            WrapModeEnum? wrapModeW = null;
            
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
                        UpdateWrapMode(ref wrapModeUVW, WrapModeEnum.Clamp, enumValue);
                        break;
                    case WrapModeParameterStates.ClampU:
                        UpdateWrapMode(ref wrapModeU, WrapModeEnum.Clamp, enumValue);
                        break;
                    case WrapModeParameterStates.ClampV:
                        UpdateWrapMode(ref wrapModeV, WrapModeEnum.Clamp, enumValue);
                        break;
                    case WrapModeParameterStates.ClampW:
                        UpdateWrapMode(ref wrapModeW, WrapModeEnum.Clamp, enumValue);
                        break;
                    case WrapModeParameterStates.Repeat:
                        UpdateWrapMode(ref wrapModeUVW, WrapModeEnum.Repeat, enumValue);
                        break;
                    case WrapModeParameterStates.RepeatU:
                        UpdateWrapMode(ref wrapModeU, WrapModeEnum.Repeat, enumValue);
                        break;
                    case WrapModeParameterStates.RepeatV:
                        UpdateWrapMode(ref wrapModeV, WrapModeEnum.Repeat, enumValue);
                        break;
                    case WrapModeParameterStates.RepeatW:
                        UpdateWrapMode(ref wrapModeW, WrapModeEnum.Repeat, enumValue);
                        break;
                    case WrapModeParameterStates.Mirror:
                        UpdateWrapMode(ref wrapModeUVW, WrapModeEnum.Mirror, enumValue);
                        break;
                    case WrapModeParameterStates.MirrorU:
                        UpdateWrapMode(ref wrapModeU, WrapModeEnum.Mirror, enumValue);
                        break;
                    case WrapModeParameterStates.MirrorV:
                        UpdateWrapMode(ref wrapModeV, WrapModeEnum.Mirror, enumValue);
                        break;
                    case WrapModeParameterStates.MirrorW:
                        UpdateWrapMode(ref wrapModeW, WrapModeEnum.Mirror, enumValue);
                        break;
                    case WrapModeParameterStates.MirrorOnce:
                        UpdateWrapMode(ref wrapModeUVW, WrapModeEnum.MirrorOnce, enumValue);
                        break;
                    case WrapModeParameterStates.MirrorOnceU:
                        UpdateWrapMode(ref wrapModeU, WrapModeEnum.MirrorOnce, enumValue);
                        break;
                    case WrapModeParameterStates.MirrorOnceV:
                        UpdateWrapMode(ref wrapModeV, WrapModeEnum.MirrorOnce, enumValue);
                        break;
                    case WrapModeParameterStates.MirrorOnceW:
                        UpdateWrapMode(ref wrapModeW, WrapModeEnum.MirrorOnce, enumValue);
                        break;
                }
            }
            // Always use the local value if it was specified, otherwise fall back to the full-width value, otherwise fall-back to repeat.
            target.WrapModeU = wrapModeU ?? wrapModeUVW ?? WrapModeEnum.Repeat;
            target.WrapModeV = wrapModeV ?? wrapModeUVW ?? WrapModeEnum.Repeat;
            target.WrapModeW = wrapModeW ?? wrapModeUVW ?? WrapModeEnum.Repeat;
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
