using System;
using System.Collections.Generic;
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
            return "Unity_ChannelMixer_" + precision;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        [SerializeField]
        ChannelMixer m_ChannelMixer = new ChannelMixer( new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1));

        [Serializable]
        public struct ChannelMixer
        {
            public Vector3 outRed;
            public Vector3 outGreen;
            public Vector3 outBlue;

            public ChannelMixer(Vector3 red, Vector3 green, Vector3 blue)
            {
                outRed = red;
                outGreen = green;
                outBlue = blue;
            }
        }

        [ChannelMixerControl("")]
        public ChannelMixer channelMixer
        {
            get { return m_ChannelMixer; }
            set
            {
                if ((value.outRed == m_ChannelMixer.outRed) && (value.outGreen == m_ChannelMixer.outGreen) && (value.outBlue == m_ChannelMixer.outBlue))
                    return;
                m_ChannelMixer = value;
                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }

        string GetFunctionPrototype(string argIn, string argRed, string argGreen, string argBlue, string argOut)
        {
            return string.Format("void {0} ({1} {2}, {3} {4}, {3} {5}, {3} {6}, out {7} {8})", GetFunctionName(), 
                ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType), argIn, 
                precision+"3", argRed, argGreen, argBlue, 
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), argOut);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string inputValue = GetSlotValue(InputSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            visitor.AddShaderChunk(string.Format("{0} {1};", ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);

            if(!generationMode.IsPreview())
            {
                visitor.AddShaderChunk(string.Format("{0}3 _{1}_Red = {0}3 ({2}, {3}, {4});", precision, GetVariableNameForNode(), channelMixer.outRed[0], channelMixer.outRed[1], channelMixer.outRed[2]), true);
                visitor.AddShaderChunk(string.Format("{0}3 _{1}_Green = {0}3 ({2}, {3}, {4});", precision, GetVariableNameForNode(), channelMixer.outGreen[0], channelMixer.outGreen[1], channelMixer.outGreen[2]), true);
                visitor.AddShaderChunk(string.Format("{0}3 _{1}_Blue = {0}3 ({2}, {3}, {4});", precision, GetVariableNameForNode(), channelMixer.outBlue[0], channelMixer.outBlue[1], channelMixer.outBlue[2]), true);
            }

            visitor.AddShaderChunk(GetFunctionCallBody(inputValue, string.Format("_{0}_Red", GetVariableNameForNode()), string.Format("_{0}_Green", GetVariableNameForNode()), string.Format("_{0}_Blue", GetVariableNameForNode()), outputValue), true);
        }

        string GetFunctionCallBody(string inputValue, string red, string green, string blue, string outputValue)
        {
            return GetFunctionName() + " (" + inputValue + ", " + red + ", " + green + ", " + blue + ", " + outputValue + ");";
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            properties.Add(new PreviewProperty()
            {
                m_Name = string.Format("_{0}_Red", GetVariableNameForNode()),
                m_PropType = PropertyType.Vector3,
                m_Vector4 = channelMixer.outRed
            });

            properties.Add(new PreviewProperty()
            {
                m_Name = string.Format("_{0}_Green", GetVariableNameForNode()),
                m_PropType = PropertyType.Vector3,
                m_Vector4 = channelMixer.outGreen
            });

            properties.Add(new PreviewProperty()
            {
                m_Name = string.Format("_{0}_Blue", GetVariableNameForNode()),
                m_PropType = PropertyType.Vector3,
                m_Vector4 = channelMixer.outBlue
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            base.CollectShaderProperties(properties, generationMode);

            properties.AddShaderProperty(new Vector4ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_Red", GetVariableNameForNode()),
                generatePropertyBlock = false
            });

            properties.AddShaderProperty(new Vector4ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_Green", GetVariableNameForNode()),
                generatePropertyBlock = false
            });

            properties.AddShaderProperty(new Vector4ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_Blue", GetVariableNameForNode()),
                generatePropertyBlock = false
            });
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk(GetFunctionPrototype("In", "Red", "Green", "Blue", "Out"), false);
            visitor.AddShaderChunk("{", false);
            visitor.Indent();

            visitor.AddShaderChunk(string.Format("Out = {0} (dot(In, Red), dot(In, Green), dot(In, Blue));",
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType)), true);

            visitor.Deindent();
            visitor.AddShaderChunk("}", false);
        }
    }
}
