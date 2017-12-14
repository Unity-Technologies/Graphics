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
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
        }
    }
}
