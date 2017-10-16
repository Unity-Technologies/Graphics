using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Texture/Sampler State")]
    public class SamplerStateNode : AbstractMaterialNode
    {
        [SerializeField]
        private TextureSamplerState.FilterMode m_filter = TextureSamplerState.FilterMode.Linear;

        public TextureSamplerState.FilterMode filter
        {
            get { return m_filter; }
            set
            {
                if (m_filter == value)
                    return;

                m_filter = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        [SerializeField]
        private TextureSamplerState.WrapMode m_wrap = TextureSamplerState.WrapMode.Repeat;

        public TextureSamplerState.WrapMode wrap
        {
            get { return m_wrap; }
            set
            {
                if (m_wrap == value)
                    return;

                m_wrap = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        public SamplerStateNode()
        {
            name = "SamplerState";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Sampler Output";

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
                overrideReferenceName = GetVariableNameForNode(),
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
            string ss = name + "_"
                        + Enum.GetName(typeof(TextureSamplerState.FilterMode), filter) + "_"
                        + Enum.GetName(typeof(TextureSamplerState.WrapMode), wrap) + "_sampler;";
            return ss;
        }
    }
}
