using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Channel", "Swizzle")]
    public class SwizzleNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public SwizzleNode()
        {
            name = "Swizzle";
            UpdateNodeAfterDeserialization();
        }

        const int InputSlotId = 0;
        const int OutputSlotId = 1;
        const string kInputSlotName = "In";
        const string kOutputSlotName = "Out";

        public override bool hasPreview
        {
            get { return true; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        static Dictionary<TextureChannel, string> s_ComponentList = new Dictionary<TextureChannel, string>
        {
            {TextureChannel.Red, "r" },
            {TextureChannel.Green, "g" },
            {TextureChannel.Blue, "b" },
            {TextureChannel.Alpha, "a" },
        };

        [SerializeField]
        TextureChannel m_RedChannel;

        [ChannelEnumControl("Red Out")]
        public TextureChannel redChannel
        {
            get { return m_RedChannel; }
            set
            {
                if (m_RedChannel == value)
                    return;

                m_RedChannel = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Node);
                }
            }
        }

        [SerializeField]
        TextureChannel m_GreenChannel;

        [ChannelEnumControl("Green Out")]
        public TextureChannel greenChannel
        {
            get { return m_GreenChannel; }
            set
            {
                if (m_GreenChannel == value)
                    return;

                m_GreenChannel = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Node);
                }
            }
        }

        [SerializeField]
        TextureChannel m_BlueChannel;

        [ChannelEnumControl("Blue Out")]
        public TextureChannel blueChannel
        {
            get { return m_BlueChannel; }
            set
            {
                if (m_BlueChannel == value)
                    return;

                m_BlueChannel = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Node);
                }
            }
        }

        [SerializeField]
        TextureChannel m_AlphaChannel;

        [ChannelEnumControl("Alpha Out")]
        public TextureChannel alphaChannel
        {
            get { return m_AlphaChannel; }
            set
            {
                if (m_AlphaChannel == value)
                    return;

                m_AlphaChannel = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Node);
                }
            }
        }

        void ValidateChannelCount()
        {
            var channelCount = SlotValueHelper.GetChannelCount(FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType);
            if ((int)redChannel >= channelCount)
                redChannel = TextureChannel.Red;
            if ((int)greenChannel >= channelCount)
                greenChannel = TextureChannel.Red;
            if ((int)blueChannel >= channelCount)
                blueChannel = TextureChannel.Red;
            if ((int)alphaChannel >= channelCount)
                alphaChannel = TextureChannel.Red;
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            ValidateChannelCount();
            var outputSlotType = FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision);
            var outputName = GetVariableNameForSlot(OutputSlotId);
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            var inputValueType = FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType;
            if (generationMode == GenerationMode.ForReals)
                visitor.AddShaderChunk(string.Format("{0} {1} = {2}.{3}{4}{5}{6};", outputSlotType, outputName, inputValue, s_ComponentList[m_RedChannel], s_ComponentList[m_GreenChannel], s_ComponentList[m_BlueChannel], s_ComponentList[m_AlphaChannel]), false);
            else
                visitor.AddShaderChunk(string.Format("{0} {1} = {0}(Unity_Swizzle_Select_{4}({3}, {2}, 0), Unity_Swizzle_Select_{4}({3}, {2}, 1), Unity_Swizzle_Select_{4}({3}, {2}, 2), Unity_Swizzle_Select_{4}({3}, {2}, 3));",
                    outputSlotType,
                    outputName,
                    GetVariableNameForNode(),
                    inputValue,
                    inputValueType.ToString(precision)), false);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            base.CollectShaderProperties(properties, generationMode);
            if (generationMode != GenerationMode.Preview)
                return;
            properties.AddShaderProperty(new FloatShaderProperty
            {
                overrideReferenceName = GetVariableNameForNode(),
                generatePropertyBlock = false
            });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            var value = ((int)redChannel) | ((int)greenChannel << 2) | ((int)blueChannel << 4) | ((int)alphaChannel << 6);
            properties.Add(new PreviewProperty
            {
                name = GetVariableNameForNode(),
                propType = PropertyType.Float,
                floatValue = value
            });
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode != GenerationMode.Preview)
                return;

            var sb = new ShaderStringBuilder();
            var valueType = FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType;
            sb.AppendLine("float Unity_Swizzle_Select_{0}({0} Value, int Swizzle, int Channel)", valueType.ToString(precision));
            using (sb.BlockScope())
            {
                var channelCount = SlotValueHelper.GetChannelCount(valueType);
                if (channelCount == 1)
                {
                    sb.AppendLine("return Value;");
                }
                else
                {
                    sb.AppendLine("int index = (Swizzle >> (2 * Channel)) & 3;");
                    sb.AppendLine("if (index == 0) return Value.r;");
                    if (channelCount >= 2)
                        sb.AppendLine("if (index == 1) return Value.g;");
                    if (channelCount >= 3)
                        sb.AppendLine("if (index == 2) return Value.b;");
                    if (channelCount == 4)
                        sb.AppendLine("if (index == 3) return Value.a;");
                    sb.AppendLine("return 0;");
                }
            }
            visitor.AddShaderChunk(sb.ToString(), true);
        }
    }
}
