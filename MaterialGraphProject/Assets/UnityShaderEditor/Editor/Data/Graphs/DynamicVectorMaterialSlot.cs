using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class DynamicVectorMaterialSlot : MaterialSlot, IMaterialSlotHasValue<Vector4>
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
            : base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
            m_Value = value;
        }

        public Vector4 defaultValue { get { return m_DefaultValue; } }

        public Vector4 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override VisualElement InstantiateControl()
        {
            int components =
                concreteValueType == ConcreteSlotValueType.Vector4 ? 4 :
                concreteValueType == ConcreteSlotValueType.Vector3 ? 3 :
                concreteValueType == ConcreteSlotValueType.Vector2 ? 2 :
                concreteValueType == ConcreteSlotValueType.Vector1 ? 1 : 0;
            return new MultiFloatSlotControlView(owner, components, () => value, (newValue) => value = newValue);
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
            var propType = ConvertConcreteSlotValueTypeToPropertyType(concreteValueType);
            var pp = new PreviewProperty(propType) { name = name };
            if (propType == PropertyType.Float)
                pp.floatValue = value.x;
            else
                pp.vector4Value = new Vector4(value.x, value.y, value.z, value.w);
            return pp;
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            var channelCount = SlotValueHelper.GetChannelCount(concreteValueType);
            var values = value.x.ToString();
            if (channelCount == 1)
                return values;
            for (var i = 1; i < channelCount; i++)
                values += ", " + value[i];
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
