using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Channel/Flip")]
    public class FlipNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public FlipNode()
        {
            name = "Flip";
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
            return "Unity_Flip_" + precision + "_" + GuidEncoder.Encode(guid);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector4.zero));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        int channelCount { get { return (int)SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(InputSlotId).concreteValueType); } }

        [SerializeField]
        private bool m_RedChannel;

        [ToggleControl("Red")]
        public ToggleState redChannel
        {
            get { return ToggleHelper.GetToggleValue(m_RedChannel, channelCount > 0); }
            set
            {
                bool isOn = ToggleHelper.GetBoolValue(value);
                if (m_RedChannel == isOn)
                    return;
                m_RedChannel = isOn;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        [SerializeField]
        private bool m_GreenChannel;

        [ToggleControl("Green")]
        public ToggleState greenChannel
        {
            get { return ToggleHelper.GetToggleValue(m_GreenChannel, channelCount > 1); }
            set
            {
                bool isOn = ToggleHelper.GetBoolValue(value);
                if (m_GreenChannel == isOn)
                    return;
                m_GreenChannel = isOn;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        [SerializeField]
        private bool m_BlueChannel;

        [ToggleControl("Blue")]
        public ToggleState blueChannel
        {
            get { return ToggleHelper.GetToggleValue(m_BlueChannel, channelCount > 2); }
            set
            {
                bool isOn = ToggleHelper.GetBoolValue(value);
                if (m_BlueChannel == isOn)
                    return;
                m_BlueChannel = isOn;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        private bool m_AlphaChannel;

        [ToggleControl("Alpha")]
        public ToggleState alphaChannel
        {
            get { return ToggleHelper.GetToggleValue(m_AlphaChannel, channelCount > 3); }
            set
            {
                bool isOn = ToggleHelper.GetBoolValue(value);
                if (m_AlphaChannel == isOn)
                    return;
                m_AlphaChannel = isOn;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        string GetFunctionPrototype(string inArg, string outArg)
        {
            return string.Format("void {0} ({1} {2}, out {3} {4})", GetFunctionName(), 
                ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType), inArg, 
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), outArg);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string inputValue = GetSlotValue(InputSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            visitor.AddShaderChunk(string.Format("{0} {1};", ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);
            visitor.AddShaderChunk(GetFunctionCallBody(inputValue, outputValue), true);
        }

        string GetFunctionCallBody(string inputValue, string outputValue)
        {
            return GetFunctionName() + " (" + inputValue + ", " + outputValue + ");";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("In", "Out"), true);
            outputString.AddShaderChunk("{", true);
            outputString.Indent();

            int channelCount = (int)SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(InputSlotId).concreteValueType);
            switch(channelCount)
            {
                case 2:
                    outputString.AddShaderChunk(string.Format("Out = {0} ({1}, {2});",
                    ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType),
                    string.Format("{0}{1}{2}", m_RedChannel ? "1 - " : "", kInputSlotName, ".r"),
                    string.Format("{0}{1}{2}", m_GreenChannel ? "1 - " : "", kInputSlotName, ".g")), true);
                    break;
                case 3:
                    outputString.AddShaderChunk(string.Format("Out = {0} ({1}, {2}, {3});",
                    ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType),
                    string.Format("{0}{1}{2}", m_RedChannel ? "1 - " : "", kInputSlotName, ".r"),
                    string.Format("{0}{1}{2}", m_GreenChannel ? "1 - " : "", kInputSlotName, ".g"),
                    string.Format("{0}{1}{2}", m_BlueChannel ? "1 - " : "", kInputSlotName, ".b")), true);
                    break;
                case 4:
                    outputString.AddShaderChunk(string.Format("Out = {0} ({1}, {2}, {3}, {4});",
                    ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType),
                    string.Format("{0}{1}{2}", m_RedChannel ? "1 - " : "", kInputSlotName, ".r"),
                    string.Format("{0}{1}{2}", m_GreenChannel ? "1 - " : "", kInputSlotName, ".g"),
                    string.Format("{0}{1}{2}", m_BlueChannel ? "1 - " : "", kInputSlotName, ".b"),
                    string.Format("{0}{1}{2}", m_AlphaChannel ? "1 - " : "", kInputSlotName, ".a")), true);
                    break;
                default:
                    outputString.AddShaderChunk(string.Format("Out = {0};",
                    string.Format("{0}{1}{2}", m_RedChannel ? "1 - " : "", kInputSlotName, ".r")), true);
                    break;
            }
            outputString.Deindent();
            outputString.AddShaderChunk("}", true);
            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}