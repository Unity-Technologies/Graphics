using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Texture3DMaterialSlot : MaterialSlot
    {
        public Texture3DMaterialSlot()
        {}

        public Texture3DMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability shaderStageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden)
        {}

        [SerializeField]
        internal bool m_BareTexture = false;
        internal override bool bareTexture
        {
            get { return m_BareTexture; }
            set { m_BareTexture = value; }
        }

        public override SlotValueType valueType { get { return SlotValueType.Texture3D; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Texture3D; } }
        public override bool isDefaultValue => true;

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {}

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Texture3DMaterialSlot;
            if (slot != null)
            {
                m_BareTexture = slot.m_BareTexture;
            }
        }
    }
}
