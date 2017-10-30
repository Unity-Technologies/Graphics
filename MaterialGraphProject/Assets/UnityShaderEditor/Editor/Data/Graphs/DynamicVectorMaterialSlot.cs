using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class DynamicVectorMaterialSlot : MaterialSlot, IMaterialSlotHasVaule<Vector4>
    {
        [SerializeField]
        private Vector4 m_Value;

        [SerializeField]
        private Vector4 m_DefaultValue;

        private ConcreteSlotValueType m_ConcreteValueType = ConcreteSlotValueType.Vector4;

        public DynamicVectorMaterialSlot()
        {
        }

        public DynamicVectorMaterialSlot(
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


        public override SlotValueType valueType { get { return SlotValueType.Dynamic; } }

        public override ConcreteSlotValueType concreteValueType
        {
            get { return m_ConcreteValueType; }
        }

        public void SetConcreteType(ConcreteSlotValueType valueType)
        {
            m_ConcreteValueType = valueType;
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

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "4 (" + value.x + "," + value.y + "," + value.z + "," + value.w + ")";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            IShaderProperty property;
            switch (concreteValueType)
            {
                case ConcreteSlotValueType.Vector4:
                    property = new Vector4ShaderProperty();
                    break;
                case ConcreteSlotValueType.Vector3:
                    property = new Vector3ShaderProperty();
                    break;
                case ConcreteSlotValueType.Vector2:
                    property = new Vector2ShaderProperty();
                    break;
                case ConcreteSlotValueType.Vector1:
                    property = new FloatShaderProperty();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            property.overrideReferenceName = matOwner.GetVariableNameForSlot(id);
            property.generatePropertyBlock = false;
            properties.AddShaderProperty(property);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as DynamicVectorMaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
