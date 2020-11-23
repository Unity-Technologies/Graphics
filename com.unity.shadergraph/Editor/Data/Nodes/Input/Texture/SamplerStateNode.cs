using System;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Sampler State")]
    class SamplerStateNode : AbstractMaterialNode, IPropertyFromNode
    {
        [SerializeField]
        private TextureSamplerState.FilterMode m_filter = TextureSamplerState.FilterMode.Linear;

        [EnumControl]
        public TextureSamplerState.FilterMode filter
        {
            get { return m_filter; }
            set
            {
                if (m_filter == value)
                    return;

                m_filter = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        private TextureSamplerState.WrapMode m_wrap = TextureSamplerState.WrapMode.Repeat;

        [EnumControl]
        public TextureSamplerState.WrapMode wrap
        {
            get { return m_wrap; }
            set
            {
                if (m_wrap == value)
                    return;

                m_wrap = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public SamplerStateNode()
        {
            name = "Sampler State";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new SamplerStateMaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new SamplerStateShaderProperty()
            {
                overrideReferenceName = string.Format("{0}_{1}_{2}", NodeUtils.GetHLSLSafeName(name), m_filter, m_wrap),
                generatePropertyBlock = false,

                value = new TextureSamplerState()
                {
                    filter = m_filter,
                    wrap =  m_wrap
                }
            });
        }

        public override string GetVariableNameForNode()
        {
            return string.Format(@"{0}_{1}_{2}", NodeUtils.GetHLSLSafeName(name),
                Enum.GetName(typeof(TextureSamplerState.FilterMode), filter),
                Enum.GetName(typeof(TextureSamplerState.WrapMode), wrap));
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            return new SamplerStateShaderProperty
            {
                value = new TextureSamplerState()
                {
                    filter = this.filter,
                    wrap = this.wrap
                }
            };
        }

        public int outputSlotId { get { return kOutputSlotId; } }
    }
}
