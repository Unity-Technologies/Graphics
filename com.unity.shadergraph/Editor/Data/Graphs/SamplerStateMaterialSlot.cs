using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class SamplerStateMaterialSlot : MaterialSlot
    {
        [SerializeField]
        private int m_TextureSlot = -1;

        [SerializeField]
        private TextureSamplerState m_Value = new TextureSamplerState();

        public int textureSlotId
        {
            get => m_TextureSlot;
            set => m_TextureSlot = value;
        }

        public TextureSamplerState value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

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

        private bool isTextureSlotConnected
        {
            get
            {
                var textureSlot = owner?.FindInputSlot<MaterialSlot>(textureSlotId);
                return textureSlot != null && (textureSlot.isConnected
                    || (textureSlot is Texture2DInputMaterialSlot texture2DSlot && texture2DSlot.texture != null)
                    || (textureSlot is Texture2DArrayInputMaterialSlot texture2DArraySlot && texture2DArraySlot.textureArray != null)
                    || (textureSlot is Texture3DInputMaterialSlot texture3DSlot && texture3DSlot.texture != null)
                    || (textureSlot is CubemapInputMaterialSlot cubemapSlot && cubemapSlot.cubemap != null));
            }
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            if (owner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return isTextureSlotConnected
                ? $"sampler{owner.GetSlotValue(textureSlotId, generationMode)}"
                : SamplerStateShaderProperty.GetSystemSamplerName(value.filter, value.wrap);
        }

        public override SlotValueType valueType { get { return SlotValueType.SamplerState; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.SamplerState; } }
        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (owner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            properties.AddShaderProperty(new SamplerStateShaderProperty()
            {
                overrideReferenceName = isTextureSlotConnected ? $"sampler{owner.GetSlotValue(textureSlotId, generationMode)}" : null,
                generatePropertyBlock = false,
                value = value
            });
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as SamplerStateMaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
