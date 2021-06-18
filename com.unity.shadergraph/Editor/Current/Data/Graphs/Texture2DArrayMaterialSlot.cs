using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Texture2DArrayMaterialSlot : MaterialSlot
    {
        public Texture2DArrayMaterialSlot()
        {}

        public Texture2DArrayMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability shaderStageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden)
        {}

        [SerializeField]
        bool m_BareResource = false;
        internal override bool bareResource
        {
            get { return m_BareResource; }
            set { m_BareResource = value; }
        }

        public override void AppendHLSLParameterDeclaration(ShaderStringBuilder sb, string paramName)
        {
            if (m_BareResource)
            {
                sb.Append("TEXTURE2D_ARRAY(");
                sb.Append(paramName);
                sb.Append(")");
            }
            else
                base.AppendHLSLParameterDeclaration(sb, paramName);
        }

        public override SlotValueType valueType { get { return SlotValueType.Texture2DArray; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Texture2DArray; } }
        public override bool isDefaultValue => true;

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {}

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Texture2DArrayMaterialSlot;
            if (slot != null)
            {
                m_BareResource = slot.m_BareResource;
            }
        }
    }
}
