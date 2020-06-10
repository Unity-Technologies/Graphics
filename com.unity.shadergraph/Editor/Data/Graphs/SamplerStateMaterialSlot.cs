using System;
using UnityEditor.Graphing;

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

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return $"{matOwner.GetVariableNameForSlot(id)}_Linear_Repeat";
        }

        public override SlotValueType valueType { get { return SlotValueType.SamplerState; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.SamplerState; } }
        public override bool isDefaultValue => true;
        
        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            properties.AddShaderProperty(new SamplerStateShaderProperty()
            {
                value = new TextureSamplerState()
                {
                    filter = TextureSamplerState.FilterMode.Linear,
                    wrap = TextureSamplerState.WrapMode.Repeat
                },
                overrideReferenceName = $"{matOwner.GetVariableNameForSlot(id)}_Linear_Repeat",
                generatePropertyBlock = false,
            });
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {}

    }
}
