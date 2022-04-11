using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Internal
{
    public enum ColorMode
    {
        Default,
        HDR
    }
}

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Basic", "Color")]
    class ColorNode : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override int latestVersion => 1;

        public ColorNode()
        {
            name = "Color";
            synonyms = new string[] { "rgba" };
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        Color m_Color = new Color(UnityEngine.Color.clear, ColorMode.Default);

        [Serializable]
        public struct Color
        {
            public UnityEngine.Color color;
            public ColorMode mode;

            public Color(UnityEngine.Color color, ColorMode mode)
            {
                this.color = color;
                this.mode = mode;
            }
        }

        [ColorControl("")]
        public Color color
        {
            get { return m_Color; }
            set
            {
                if ((value.color == m_Color.color) && (value.mode == m_Color.mode))
                    return;

                if ((value.mode != m_Color.mode) && (value.mode == ColorMode.Default))
                {
                    float r = Mathf.Clamp(value.color.r, 0, 1);
                    float g = Mathf.Clamp(value.color.g, 0, 1);
                    float b = Mathf.Clamp(value.color.b, 0, 1);
                    float a = Mathf.Clamp(value.color.a, 0, 1);
                    value.color = new UnityEngine.Color(r, g, b, a);
                }

                m_Color = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            properties.AddShaderProperty(new ColorShaderProperty()
            {
                overrideReferenceName = GetVariableNameForNode(),
                generatePropertyBlock = false,
                value = color.color,
                colorMode = color.mode
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;

            switch (sgVersion)
            {
                case 0:
                    sb.AppendLine(@"$precision4 {0} = IsGammaSpace() ? $precision4({1}, {2}, {3}, {4}) : $precision4(SRGBToLinear($precision3({1}, {2}, {3})), {4});"
                        , GetVariableNameForNode()
                        , NodeUtils.FloatToShaderValue(color.color.r)
                        , NodeUtils.FloatToShaderValue(color.color.g)
                        , NodeUtils.FloatToShaderValue(color.color.b)
                        , NodeUtils.FloatToShaderValue(color.color.a));
                    break;
                case 1:
                    //HDR color picker assumes Linear space, regular color picker assumes SRGB. Handle both cases
                    if (color.mode == ColorMode.Default)
                    {
                        sb.AppendLine(@"$precision4 {0} = IsGammaSpace() ? $precision4({1}, {2}, {3}, {4}) : $precision4(SRGBToLinear($precision3({1}, {2}, {3})), {4});"
                            , GetVariableNameForNode()
                            , NodeUtils.FloatToShaderValue(color.color.r)
                            , NodeUtils.FloatToShaderValue(color.color.g)
                            , NodeUtils.FloatToShaderValue(color.color.b)
                            , NodeUtils.FloatToShaderValue(color.color.a));
                    }
                    else
                    {
                        sb.AppendLine(@"$precision4 {0} = IsGammaSpace() ? LinearToSRGB($precision4({1}, {2}, {3}, {4})) : $precision4({1}, {2}, {3}, {4});"
                            , GetVariableNameForNode()
                            , NodeUtils.FloatToShaderValue(color.color.r)
                            , NodeUtils.FloatToShaderValue(color.color.g)
                            , NodeUtils.FloatToShaderValue(color.color.b)
                            , NodeUtils.FloatToShaderValue(color.color.a));
                    }
                    break;
            }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            UnityEngine.Color propColor = color.color;
            if (color.mode == ColorMode.Default)
            {
                if (PlayerSettings.colorSpace == ColorSpace.Linear)
                    propColor = propColor.linear;
            }
            if (color.mode == ColorMode.HDR)
            {
                switch (sgVersion)
                {
                    case 0:
                        if (PlayerSettings.colorSpace == ColorSpace.Linear)
                            propColor = propColor.linear;
                        break;
                    case 1:
                        if (PlayerSettings.colorSpace == ColorSpace.Gamma)
                            propColor = propColor.gamma;
                        break;
                }
            }

            // we use Vector4 type to avoid all of the automatic color conversions of PropertyType.Color
            properties.Add(new PreviewProperty(PropertyType.Vector4)
            {
                name = GetVariableNameForNode(),
                vector4Value = propColor
            });
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            return new ColorShaderProperty() { value = color.color, colorMode = color.mode };
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
