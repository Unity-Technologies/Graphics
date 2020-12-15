using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Channel", "Flip")]
    class FlipNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
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
            return $"Unity_Flip_{FindSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString(concretePrecision)}";
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector4.zero));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        int channelCount { get { return SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(InputSlotId).concreteValueType); } }

        [SerializeField]
        private bool m_RedChannel;

        [ToggleControl("Red")]
        public ToggleData redChannel
        {
            get { return new ToggleData(m_RedChannel, channelCount > 0); }
            set
            {
                if (m_RedChannel == value.isOn)
                    return;
                m_RedChannel = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        private bool m_GreenChannel;

        [ToggleControl("Green")]
        public ToggleData greenChannel
        {
            get { return new ToggleData(m_GreenChannel, channelCount > 1); }
            set
            {
                if (m_GreenChannel == value.isOn)
                    return;
                m_GreenChannel = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        private bool m_BlueChannel;

        [ToggleControl("Blue")]
        public ToggleData blueChannel
        {
            get { return new ToggleData(m_BlueChannel, channelCount > 2); }
            set
            {
                if (m_BlueChannel == value.isOn)
                    return;
                m_BlueChannel = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        private bool m_AlphaChannel;

        [ToggleControl("Alpha")]
        public ToggleData alphaChannel
        {
            get { return new ToggleData(m_AlphaChannel, channelCount > 3); }
            set
            {
                if (m_AlphaChannel == value.isOn)
                    return;
                m_AlphaChannel = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputSlotId));

            if (!generationMode.IsPreview())
            {
                sb.AppendLine("{0} _{1}_Flip = {0} ({2}",
                    FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString(),
                    GetVariableNameForNode(),
                    Convert.ToInt32(m_RedChannel));
                if (channelCount > 1)
                    sb.Append(", {0}", Convert.ToInt32(m_GreenChannel));
                if (channelCount > 2)
                    sb.Append(", {0}", Convert.ToInt32(m_BlueChannel));
                if (channelCount > 3)
                    sb.Append(", {0}", Convert.ToInt32(m_AlphaChannel));
                sb.Append(");");
            }

            sb.AppendLine("{0}({1}, _{2}_Flip, {3});", GetFunctionName(), inputValue, GetVariableNameForNode(), outputValue);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            properties.Add(new PreviewProperty(PropertyType.Vector4)
            {
                name = string.Format("_{0}_Flip", GetVariableNameForNode()),
                vector4Value = new Vector4(Convert.ToInt32(m_RedChannel), Convert.ToInt32(m_GreenChannel), Convert.ToInt32(m_BlueChannel), Convert.ToInt32(m_AlphaChannel)),
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

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
            {
                s.AppendLine("void {0}({1} In, {2} Flip, out {3} Out)",
                    GetFunctionName(),
                    FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToShaderString(),
                    FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToShaderString(),
                    FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString());
                using (s.BlockScope())
                {
                    s.AppendLine("Out = (Flip * -2 + 1) * In;");
                }
            });
        }
    }
}
