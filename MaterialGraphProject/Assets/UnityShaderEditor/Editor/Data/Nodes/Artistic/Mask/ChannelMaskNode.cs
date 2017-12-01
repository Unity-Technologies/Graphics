using System.Reflection;
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

    [Title("Artistic/Mask/Channel Mask")]
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
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        void ValidateChannelCount()
        {
            int channelCount = (int)SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(InputSlotId).concreteValueType);
            if ((int)channel >= channelCount)
                channel = TextureChannel.Red;
        }

        string GetFunctionPrototype(string argIn, string argOut)
        {
            return string.Format("void {0} ({1} {2}, out {3} {4})", GetFunctionName(),
                ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<DynamicVectorMaterialSlot>(InputSlotId).concreteValueType), argIn,
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<DynamicVectorMaterialSlot>(OutputSlotId).concreteValueType), argOut);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            ValidateChannelCount();
            string inputValue = GetSlotValue(InputSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            visitor.AddShaderChunk(string.Format("{0} {1};", ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);
            visitor.AddShaderChunk(GetFunctionCallBody(inputValue, outputValue), true);
        }

        string GetFunctionCallBody(string inputValue, string outputValue)
        {
            return GetFunctionName() + " (" + inputValue + ", " + outputValue + ");";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            ValidateChannelCount();
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("In", "Out"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            switch (channel)
            {
                case TextureChannel.Green:
                    outputString.AddShaderChunk("Out = In.yyyy;", false);
                    break;
                case TextureChannel.Blue:
                    outputString.AddShaderChunk("Out = In.zzzz;", false);
                    break;
                case TextureChannel.Alpha:
                    outputString.AddShaderChunk("Out = In.wwww;", false);
                    break;
                default:
                    outputString.AddShaderChunk("Out = In.xxxx;", false);
                    break;
            }

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
