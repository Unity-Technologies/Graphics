using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum TextureChannel
    {
        Red,
        Green,
        Blue,
        Alpha
    }

    [Title("Artistic", "Mask", "Channel Mask")]
    public class ChannelMaskNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
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
            return string.Format("Unity_ChannelMask_{0}_{1}", channel, precision);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector3.zero));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        [SerializeField]
        private TextureChannel m_Channel = TextureChannel.Red;

        [ChannelEnumControl("Channel")]
        public TextureChannel channel
        {
            get { return m_Channel; }
            set
            {
                if (m_Channel == value)
                    return;

                m_Channel = value;
                Dirty(ModificationScope.Graph);
            }
        }

        void ValidateChannelCount()
        {
            int channelCount = SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(InputSlotId).concreteValueType);
            if ((int)channel >= channelCount)
                channel = TextureChannel.Red;
        }

        string GetFunctionPrototype(string argIn, string argOut)
        {
            return string.Format("void {0} ({1} {2}, out {3} {4})", GetFunctionName(), NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<DynamicVectorMaterialSlot>(InputSlotId).concreteValueType), argIn, NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<DynamicVectorMaterialSlot>(OutputSlotId).concreteValueType), argOut);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            ValidateChannelCount();
            string inputValue = GetSlotValue(InputSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            visitor.AddShaderChunk(string.Format("{0} {1};", NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);
            visitor.AddShaderChunk(GetFunctionCallBody(inputValue, outputValue), true);
        }

        string GetFunctionCallBody(string inputValue, string outputValue)
        {
            return GetFunctionName() + " (" + inputValue + ", " + outputValue + ");";
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
            {
                s.AppendLine(GetFunctionPrototype("In", "Out"));
                using (s.BlockScope())
                {
                    switch (channel)
                    {
                        case TextureChannel.Green:
                            s.AppendLine("Out = In.yyyy;");
                            break;
                        case TextureChannel.Blue:
                            s.AppendLine("Out = In.zzzz;");
                            break;
                        case TextureChannel.Alpha:
                            s.AppendLine("Out = In.wwww;");
                            break;
                        case TextureChannel.Red:
                            s.AppendLine("Out = In.xxxx;");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            });
        }
    }
}
