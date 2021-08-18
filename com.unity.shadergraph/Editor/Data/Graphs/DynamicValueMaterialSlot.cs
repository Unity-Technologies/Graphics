using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class DynamicValueMaterialSlot : MaterialSlot, IMaterialSlotHasValue<Matrix4x4>
    {
        [SerializeField]
        private Matrix4x4 m_Value;

        [SerializeField]
        private Matrix4x4 m_DefaultValue = Matrix4x4.identity;

        static readonly string[] k_Labels = { "X", "Y", "Z", "W" };

        private ConcreteSlotValueType m_ConcreteValueType = ConcreteSlotValueType.Vector4;

        public DynamicValueMaterialSlot()
        {
        }

        public DynamicValueMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Matrix4x4 value,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_Value = value;
        }

        public Matrix4x4 defaultValue { get { return m_DefaultValue; } }

        public Matrix4x4 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override bool isDefaultValue => value.Equals(defaultValue);

        public override VisualElement InstantiateControl()
        {
            var labels = k_Labels.Take(concreteValueType.GetChannelCount()).ToArray();
            return new MultiFloatSlotControlView(owner, labels, () => value.GetRow(0), (newValue) =>
                value = new Matrix4x4()
                {
                    m00 = newValue.x,
                    m01 = newValue.y,
                    m02 = newValue.z,
                    m03 = newValue.w,
                    m10 = value.m10,
                    m11 = value.m11,
                    m12 = value.m12,
                    m13 = value.m13,
                    m20 = value.m20,
                    m21 = value.m21,
                    m22 = value.m22,
                    m23 = value.m23,
                    m30 = value.m30,
                    m31 = value.m31,
                    m32 = value.m32,
                    m33 = value.m33,
                });
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

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var propType = concreteValueType.ToPropertyType();
            var pp = new PreviewProperty(propType) { name = name };
            if (propType == PropertyType.Float)
                pp.floatValue = value.m00;
            else
                pp.vector4Value = new Vector4(value.m00, value.m01, value.m02, value.m03);
            properties.Add(pp);
        }

        protected override string ConcreteSlotValueAsVariable()
        {
            var channelCount = SlotValueHelper.GetChannelCount(concreteValueType);
            string values = NodeUtils.FloatToShaderValue(value.m00);
            if (channelCount == 1)
                return values;
            for (var i = 1; i < channelCount; i++)
                values += ", " + NodeUtils.FloatToShaderValue(value.GetRow(0)[i]);
            return string.Format("$precision{0}({1})", channelCount, values);
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            AbstractShaderProperty property;
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
                    property = new Vector1ShaderProperty();
                    break;
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
            var slot = foundSlot as DynamicValueMaterialSlot;
            if (slot != null)
                value = slot.value;
        }

        public override void CopyDefaultValue(MaterialSlot other)
        {
            base.CopyDefaultValue(other);
            if (other is IMaterialSlotHasValue<Matrix4x4> ms)
            {
                m_DefaultValue = ms.defaultValue;
            }
        }
    }
}
