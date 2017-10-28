using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Vector1MaterialSlot : MaterialSlot, IMaterialSlotHasVaule<float>
    {
        [SerializeField]
        private float m_Value;

        [SerializeField]
        private float m_DefaultValue;

        public Vector1MaterialSlot()
        {
        }

        public Vector1MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            float value,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
            m_DefaultValue = value;
            m_Value = value;
        }

        public float defaultValue { get { return m_DefaultValue; } }

        public float value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return value.ToString();
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector1; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector1; } }

        public override PreviewProperty GetPreviewProperty(string name)
        {
            var pp = new PreviewProperty
            {
                m_Name = name,
                m_PropType = ConvertConcreteSlotValueTypeToPropertyType(concreteValueType),
                m_Vector4 = new Vector4(value, value, value, value),
                m_Float = value,
                m_Color = new Vector4(value, value, value, value),
            };
            return pp;
        }
    }
}
