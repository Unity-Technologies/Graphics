using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic/Adjustment/Channel Mixer")]
    public class ChannelMixerNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public ChannelMixerNode()
        {
            name = "Channel Mixer";
            UpdateNodeAfterDeserialization();
        }

        private const int InputSlotId = 0;
        private const int OutputSlotId = 1;
        private const string kInputSlotName = "In";
        private const string kOutputSlotName = "Out";

        public override bool hasPreview
        {
            get { return true; }
        }

        protected string GetFunctionName()
        {
            return "Unity_ChannelMixer_" + precision;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(GetInputSlot());
            AddSlot(GetOutputSlot());
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { InputSlotId, OutputSlotId }; }
        }

        protected virtual MaterialSlot GetInputSlot()
        {
            return new Vector3MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector3.zero);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero);
        }
        
        [Serializable]
        public class ChannelMixer
        {
            public OutChannel[] outChannels;
            public ChannelMixer(OutChannel[] c)
            {
                outChannels = c;
            }
        }

        [Serializable]
        public class OutChannel
        {
            public float[] inChannels;
            public OutChannel(float[] c)
            {
                inChannels = c;
            }
        }

        [SerializeField]
        private ChannelMixer m_ChannelMixer;

        [ChannelMixerControl("")]
        public ChannelMixer channelMixer
        {
            get
            {
                if(m_ChannelMixer == null)
                {
                    m_ChannelMixer = new ChannelMixer(
                    new OutChannel[3]
                    {
                        new OutChannel( new float[3] { 1, 0, 0 } ),
                        new OutChannel( new float[3] { 0, 1, 0 } ),
                        new OutChannel( new float[3] { 0, 0, 1 } )
                    });
                }
                return m_ChannelMixer;
            }
            set
            {
                /*if (m_ChannelMixer == value) // This is always true with nested arrays in a class
                    return;*/

                m_ChannelMixer = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        protected string GetFunctionPrototype(string arg1Name, string arg2Name)
        {
            return string.Format("void {0} ({1} {2}, out {3} {4})", GetFunctionName(), ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType), arg1Name, ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), arg2Name);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlotId }, new[] { OutputSlotId });
            string inputValue = GetSlotValue(InputSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            visitor.AddShaderChunk(string.Format("{0} {1};", ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);
            visitor.AddShaderChunk(GetFunctionCallBody(inputValue, outputValue), true);
        }

        protected string GetFunctionCallBody(string inputValue, string outputValue)
        {
            return GetFunctionName() + " (" + inputValue + ", " + outputValue + ");";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("In", "Out"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk(string.Format("{0}3 red = {0}3 ({1}, {2}, {3});",
                precision, channelMixer.outChannels[0].inChannels[0], channelMixer.outChannels[0].inChannels[1], channelMixer.outChannels[0].inChannels[2]), true);
            outputString.AddShaderChunk(string.Format("{0}3 green = {0}3 ({1}, {2}, {3});",
                precision, channelMixer.outChannels[1].inChannels[0], channelMixer.outChannels[1].inChannels[1], channelMixer.outChannels[1].inChannels[2]), true);
            outputString.AddShaderChunk(string.Format("{0}3 blue = {0}3 ({1}, {2}, {3});",
                precision, channelMixer.outChannels[2].inChannels[0], channelMixer.outChannels[2].inChannels[1], channelMixer.outChannels[2].inChannels[2]), true);

            outputString.AddShaderChunk(string.Format("Out = {0} {1};",
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType),
                "(dot(In, red), dot(In, green), dot(In, blue))"), true);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
