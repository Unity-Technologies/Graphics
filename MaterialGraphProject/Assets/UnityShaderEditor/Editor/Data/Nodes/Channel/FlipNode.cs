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
            return "Unity_Flip_" + ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType);
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
                    onModified(this, ModificationScope.Node);
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
                    onModified(this, ModificationScope.Node);
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
                    onModified(this, ModificationScope.Node);
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
                    onModified(this, ModificationScope.Node);
                }
            }
        }

        string GetFunctionPrototype(string inArg, string flipArg, string outArg)
        {
            return string.Format("void {0} ({1} {2}, {3} {4}, out {5} {6})", GetFunctionName(), 
                ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType), inArg,
                ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType), flipArg, 
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), outArg);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string inputValue = GetSlotValue(InputSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            visitor.AddShaderChunk(string.Format("{0} {1};", ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);

            if(!generationMode.IsPreview())
            {
                visitor.AddShaderChunk(string.Format("{0} _{1}_Flip = {0} ({2}{3}{4}{5});",
                    ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType),
                    GetVariableNameForNode(),
                    string.Format("{0}", (Convert.ToInt32(m_RedChannel)).ToString()),
                    channelCount > 1 ? string.Format(", {0}", (Convert.ToInt32(m_GreenChannel)).ToString()) : "",
                    channelCount > 2 ? string.Format(", {0}", (Convert.ToInt32(m_BlueChannel)).ToString()) : "",
                    channelCount > 3 ? string.Format(", {0}", (Convert.ToInt32(m_AlphaChannel)).ToString()) : ""), true);
            }

            visitor.AddShaderChunk(GetFunctionCallBody(inputValue, string.Format("_{0}_Flip", GetVariableNameForNode()), outputValue), true);
        }

        string GetFunctionCallBody(string inputValue, string flipValue, string outputValue)
        {
            return GetFunctionName() + " (" + inputValue + ", " + flipValue + ", " + outputValue + ");";
        }
        
        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            properties.Add(new PreviewProperty()
            {
                m_Name = string.Format("_{0}_Flip", GetVariableNameForNode()),
                m_PropType = PropertyType.Vector4,
                m_Vector4 = new Vector4(Convert.ToInt32(m_RedChannel), Convert.ToInt32(m_GreenChannel), Convert.ToInt32(m_BlueChannel), Convert.ToInt32(m_AlphaChannel)),
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            base.CollectShaderProperties(properties, generationMode);

            properties.AddShaderProperty(new Vector4ShaderProperty
            {
                overrideReferenceName = string.Format("_{0}_Flip", GetVariableNameForNode()),
                generatePropertyBlock = false
            });
        }
        
        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("In", "Flip", "Out"), true);
            outputString.AddShaderChunk("{", true);
            outputString.Indent();
            outputString.AddShaderChunk("Out = abs(Flip - In);", true);
            outputString.Deindent();
            outputString.AddShaderChunk("}", true);
            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}