using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Vector4MaterialSlot : MaterialSlot, IMaterialSlotHasVaule<Vector4>
    {
        [SerializeField]
        private Vector4 m_Value;

        [SerializeField]
        private Vector4 m_DefaultValue;

        public Vector4MaterialSlot()
        {
        }

        public Vector4MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector4 value,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
            m_Value = value;
        }

        public Vector4 defaultValue { get { return m_DefaultValue; } }

        public Vector4 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "4 (" + value.x + "," + value.y + "," + value.z + "," + value.w + ")";
        }

        public override PreviewProperty GetPreviewProperty(string name)
        {
            var pp = new PreviewProperty
            {
                m_Name = name,
                m_PropType = ConvertConcreteSlotValueTypeToPropertyType(concreteValueType),
                m_Vector4 = new Vector4(value.x, value.y, value.z, value.w),
                m_Float = value.x,
                m_Color = new Vector4(value.x, value.x, value.z, value.w),
            };
            return pp;
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector4; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector4; } }
    }
}
