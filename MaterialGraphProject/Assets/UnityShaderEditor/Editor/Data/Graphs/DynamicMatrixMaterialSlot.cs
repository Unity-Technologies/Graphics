using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class DynamicMatrixMaterialSlot : MaterialSlot, IMaterialSlotHasVaule<Matrix4x4>
    {
        [SerializeField]
        private Matrix4x4 m_Value;

        [SerializeField]
        private Matrix4x4 m_DefaultValue;

        private ConcreteSlotValueType m_ConcreteValueType = ConcreteSlotValueType.Matrix4;

        public DynamicMatrixMaterialSlot()
        {
        }

        public DynamicMatrixMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
            m_Value = value;
        }

        public Matrix4x4 defaultValue { get { return m_DefaultValue; } }

        public Matrix4x4 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override SlotValueType valueType { get { return SlotValueType.DynamicMatrix; } }

        public override ConcreteSlotValueType concreteValueType
        {
            get { return m_ConcreteValueType; }
        }

        public void SetConcreteType(ConcreteSlotValueType valueType)
        {
            m_ConcreteValueType = valueType;
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            var channelCount = (int)SlotValueHelper.GetChannelCount(concreteValueType);
            var values = "";
            for (var r = 0; r < channelCount; r++)
            {
                for (var i = 0; i < channelCount; i++)
                {
                    values += ", " + value.GetRow(r)[i];
                }
            }
            return string.Format("{0}{1}({2})", precision, channelCount, values);
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
                case ConcreteSlotValueType.Matrix4:
                    property = new Matrix4ShaderProperty();
                    break;
                case ConcreteSlotValueType.Matrix3:
                    property = new Matrix3ShaderProperty();
                    break;
                case ConcreteSlotValueType.Matrix2:
                    property = new Matrix2ShaderProperty();
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
            var slot = foundSlot as DynamicMatrixMaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
