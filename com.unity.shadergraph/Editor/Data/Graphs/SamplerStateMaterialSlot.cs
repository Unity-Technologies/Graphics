using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class SamplerStateMaterialSlot : MaterialSlot
    {
        public SamplerStateMaterialSlot()
        {
        }

        public SamplerStateMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
        }

        [SerializeField]
        internal bool m_BareResource = false;
        internal override bool bareResource
        {
            get { return m_BareResource; }
            set { m_BareResource = value; }
        }

        public override string GetHLSLVariableType()
        {
            if (m_BareResource)
                return "SamplerState";
            else
                return concreteValueType.ToShaderString();
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var nodeOwner = owner as AbstractMaterialNode;
            if (nodeOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return "UnityBuildSamplerStateStruct(SamplerState_Linear_Repeat)";
            //return $"{nodeOwner.GetVariableNameForSlot(id)}_Linear_Repeat";     // TODO: ??
        }

        public override SlotValueType valueType { get { return SlotValueType.SamplerState; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.SamplerState; } }
        public override bool isDefaultValue => true;
        
        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            var nodeOwner = owner as AbstractMaterialNode;
            if (nodeOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            properties.AddShaderProperty(new SamplerStateShaderProperty()
            {
                value = new TextureSamplerState()
                {
                    filter = TextureSamplerState.FilterMode.Linear,
                    wrap = TextureSamplerState.WrapMode.Repeat
                },
                // overrideReferenceName = $"{nodeOwner.GetVariableNameForSlot(id)}_Linear_Repeat",
                generatePropertyBlock = false,
            });
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {}

    }
}
