using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    enum TextureChannel
    {
        Red,
        Green,
        Blue,
        Alpha
    }

    [Title("Artistic", "Mask", "Channel Mask")]
    class ChannelMaskNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public ChannelMaskNode()
        {
            name = "Channel Mask";
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
            string channelSum = "None";
            if (channelMask != 0)
            {
                bool red = (channelMask & 1) != 0;
                bool green = (channelMask & 2) != 0;
                bool blue = (channelMask & 4) != 0;
                bool alpha = (channelMask & 8) != 0;
                channelSum = string.Format("{0}{1}{2}{3}", red ? "Red" : "", green ? "Green" : "", blue ? "Blue" : "", alpha ? "Alpha" : "");
            }
            return $"Unity_ChannelMask_{channelSum}_{FindSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString(concretePrecision)}";
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector3.zero));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        public TextureChannel channel;

        [SerializeField]
        private int m_ChannelMask = -1;

        [ChannelEnumMaskControl("Channels")]
        public int channelMask
        {
            get { return m_ChannelMask; }
            set
            {
                if (m_ChannelMask == value)
                    return;

                m_ChannelMask = value;
                Dirty(ModificationScope.Graph);
            }
        }

        void ValidateChannelCount()
        {
            int channelCount = SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(InputSlotId).concreteValueType);
            if (channelMask >= 1 << channelCount)
                channelMask = -1;
        }

        string GetFunctionPrototype(string argIn, string argOut)
        {
            return string.Format("void {0} ({1} {2}, out {3} {4})"
                , GetFunctionName()
                , FindInputSlot<DynamicVectorMaterialSlot>(InputSlotId).concreteValueType.ToShaderString()
                , argIn
                , FindOutputSlot<DynamicVectorMaterialSlot>(OutputSlotId).concreteValueType.ToShaderString()
                , argOut);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            ValidateChannelCount();
            string inputValue = GetSlotValue(InputSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            sb.AppendLine(string.Format("{0} {1};", FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputSlotId)));
            sb.AppendLine(GetFunctionCallBody(inputValue, outputValue));
        }

        string GetFunctionCallBody(string inputValue, string outputValue)
        {
            return GetFunctionName() + " (" + inputValue + ", " + outputValue + ");";
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            ValidateChannelCount();
            registry.ProvideFunction(GetFunctionName(), s =>
            {
                int channelCount = SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(InputSlotId).concreteValueType);
                s.AppendLine(GetFunctionPrototype("In", "Out"));
                using (s.BlockScope())
                {
                    if (channelMask == 0)
                        s.AppendLine("Out = 0;");
                    else if (channelMask == -1)
                        s.AppendLine("Out = In;");
                    else
                    {
                        bool red = (channelMask & 1) != 0;
                        bool green = (channelMask & 2) != 0;
                        bool blue = (channelMask & 4) != 0;
                        bool alpha = (channelMask & 8) != 0;

                        switch (channelCount)
                        {
                            case 1:
                                s.AppendLine("Out = In.r;");
                                break;
                            case 2:
                                s.AppendLine(string.Format("Out = $precision2({0}, {1});",
                                    red ? "In.r" : "0", green ? "In.g" : "0"));
                                break;
                            case 3:
                                s.AppendLine(string.Format("Out = $precision3({0}, {1}, {2});",
                                    red ? "In.r" : "0", green ? "In.g" : "0", blue ? "In.b" : "0"));
                                break;
                            case 4:
                                s.AppendLine(string.Format("Out = $precision4({0}, {1}, {2}, {3});",
                                    red ? "In.r" : "0", green ? "In.g" : "0", blue ? "In.b" : "0", alpha ? "In.a" : "0"));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            });
        }
    }
}
