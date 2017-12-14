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

        string GetFunctionName()
        {
            return "Unity_Swizzle_" + precision + "_" + GuidEncoder.Encode(guid);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        static Dictionary<TextureChannel, string> m_ComponentList = new Dictionary<TextureChannel, string>
        {
            {TextureChannel.Red, ".r" },
            {TextureChannel.Green, ".g" },
            {TextureChannel.Blue, ".b" },
            {TextureChannel.Alpha, ".a" },
        };

        [SerializeField]
        private TextureChannel m_RedChannel;

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
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        [SerializeField]
        private TextureChannel m_GreenChannel;

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
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        [SerializeField]
        private TextureChannel m_BlueChannel;

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
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        [SerializeField]
        private TextureChannel m_AlphaChannel;

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
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        void ValidateChannelCount()
        {
            int channelCount = (int)SlotValueHelper.GetChannelCount(FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType);
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
            string inputValue = GetSlotValue(InputSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            visitor.AddShaderChunk(string.Format("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision), GetVariableNameForSlot(OutputSlotId)), false);
            visitor.AddShaderChunk(GetFunctionCallBody(inputValue, outputValue), false);
        }

        string GetFunctionCallBody(string inputValue, string outputValue)
        {
            return GetFunctionName() + " (" + inputValue + ", " + outputValue + ");";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            ValidateChannelCount();
            var sb = new ShaderStringBuilder();
            sb.AppendLine("void {0} ({1} In, out {2} Out)", GetFunctionName(), FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToString(precision), FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision));
            using (sb.BlockScope())
            {
                sb.AppendLine("Out = {0}({1}{2}, {1}{3}, {1}{4}, {1}{5});", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision), kInputSlotName, m_ComponentList[m_RedChannel], m_ComponentList[m_GreenChannel], m_ComponentList[m_BlueChannel], m_ComponentList[m_AlphaChannel]);
            }

            visitor.AddShaderChunk(sb.ToString(), true);
        }
    }
}
