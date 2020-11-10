using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Texture2DMaterialSlot : MaterialSlot
    {
        public Texture2DMaterialSlot()
        {}

        public Texture2DMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {}

        [SerializeField]
        internal bool m_BareTexture = false;
        internal override bool bareTexture
        {
            get { return m_BareTexture; }
            set { m_BareTexture = value; }
        }

        public override string GetHLSLVariableType()
        {
            if (m_BareTexture)
                return "Texture2D";
            else
                return concreteValueType.ToShaderString();
        }

        public override SlotValueType valueType { get { return SlotValueType.Texture2D; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Texture2D; } }
        public override bool isDefaultValue => true;

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {}

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Texture2DMaterialSlot;
            if (slot != null)
            {
                m_BareTexture = slot.m_BareTexture;
            }
        }
    }
}
