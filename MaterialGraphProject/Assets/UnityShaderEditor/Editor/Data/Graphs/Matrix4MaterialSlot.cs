using System;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Matrix4MaterialSlot : MaterialSlot
    {
        [SerializeField]
        private Matrix4x4 m_Value;

        [SerializeField]
        private Matrix4x4 m_DefaultValue;

        public Matrix4MaterialSlot()
        {
        }

        public Matrix4MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
        }

        public Matrix4x4 defaultValue { get { return m_DefaultValue; } }

        public Matrix4x4 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "4x4 (1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1)";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var property = new Matrix4ShaderProperty()
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddShaderProperty(property);
        }

        public override PreviewProperty GetPreviewProperty(string name)
        {
            var pp = new PreviewProperty
            {
                name = name,
                propType = ConvertConcreteSlotValueTypeToPropertyType(concreteValueType),
                vector4Value = new Vector4(value.GetRow(0).x, value.GetRow(0).y, value.GetRow(0).z, value.GetRow(0).w),
                floatValue = value.GetRow(0).x,
                colorValue = new Vector4(value.GetRow(0).x, value.GetRow(0).x, value.GetRow(0).z, value.GetRow(0).w)
            };
            return pp;
        }

        public override SlotValueType valueType { get { return SlotValueType.Matrix4; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Matrix4; } }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Matrix4MaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
