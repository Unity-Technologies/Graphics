using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Vector4MaterialSlot : MaterialSlot, IDynamicDimensionSlot, IMaterialSlotHasValue<Vector4>
    {
        [SerializeField]
        private Vector4 m_Value;

        [SerializeField]
        private Vector4 m_DefaultValue = Vector4.zero;

        string[] m_Labels;

        public Vector4MaterialSlot()
        {
            m_Labels = new[] { "X", "Y", "Z", "W" };
        }

        public Vector4MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector4 value,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            string label1 = "X",
            string label2 = "Y",
            string label3 = "Z",
            string label4 = "W",
            bool hidden = false,
            string dynamicDimensionGroup = null)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_Value = value;
            m_Labels = new[] { label1, label2, label3, label4 };
            m_DynamicDimensionGroup = dynamicDimensionGroup;
        }

        public Vector4 defaultValue { get { return m_DefaultValue; } }

        public Vector4 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override VisualElement InstantiateControl()
        {
            var labels = m_Labels.Take(concreteValueType.GetChannelCount()).ToArray();
            return new MultiFloatSlotControlView(owner, labels, () => value, (newValue) => value = newValue);
        }

        protected override string ConcreteSlotValueAsVariable()
        {
            if (concreteValueType == ConcreteSlotValueType.Vector1)
                return NodeUtils.FloatToShaderValue(value.x);
            else if (concreteValueType == ConcreteSlotValueType.Vector2)
                return $"$precision2 ({NodeUtils.FloatToShaderValue(value.x)}, {NodeUtils.FloatToShaderValue(value.y)})";
            else if (concreteValueType == ConcreteSlotValueType.Vector3)
                return $"$precision3 ({NodeUtils.FloatToShaderValue(value.x)}, {NodeUtils.FloatToShaderValue(value.y)}, {NodeUtils.FloatToShaderValue(value.z)})";
            else
                return $"$precision4 ({NodeUtils.FloatToShaderValue(value.x)}, {NodeUtils.FloatToShaderValue(value.y)}, {NodeUtils.FloatToShaderValue(value.z)}, {NodeUtils.FloatToShaderValue(value.w)})";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            AbstractShaderProperty property;
            if (m_ConcreteValueType == ConcreteSlotValueType.Vector1)
                property = new Vector1ShaderProperty() { value = value.x };
            if (m_ConcreteValueType == ConcreteSlotValueType.Vector2)
                property = new Vector2ShaderProperty() { value = new Vector2(value.x, value.y) };
            else if (m_ConcreteValueType == ConcreteSlotValueType.Vector3)
                property = new Vector3ShaderProperty() { value = new Vector3(value.x, value.y, value.z) };
            else
                property = new Vector4ShaderProperty() { value = value };

            property.overrideReferenceName = matOwner.GetVariableNameForSlot(id);
            property.generatePropertyBlock = false;
            properties.AddShaderProperty(property);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            if (m_ConcreteValueType == ConcreteSlotValueType.Vector1)
            {
                var pp = new PreviewProperty(PropertyType.Vector1)
                {
                    name = name,
                    floatValue = value.x,
                };
                properties.Add(pp);
            }
            else if (m_ConcreteValueType == ConcreteSlotValueType.Vector2)
            {
                var pp = new PreviewProperty(PropertyType.Vector2)
                {
                    name = name,
                    vector4Value = new Vector4(value.x, value.y, 0, 0),
                };
                properties.Add(pp);
            }
            else if (m_ConcreteValueType == ConcreteSlotValueType.Vector3)
            {
                var pp = new PreviewProperty(PropertyType.Vector3)
                {
                    name = name,
                    vector4Value = new Vector4(value.x, value.y, value.z, 0)
                };
                properties.Add(pp);
            }
            else
            {
                var pp = new PreviewProperty(PropertyType.Vector4)
                {
                    name = name,
                    vector4Value = new Vector4(value.x, value.y, value.z, value.w),
                };
                properties.Add(pp);
            }
        }

        public override SlotValueType valueType => SlotValueType.Vector4;
        public override ConcreteSlotValueType concreteValueType => m_ConcreteValueType;

        [SerializeField]
        private string m_DynamicDimensionGroup = null;
        private ConcreteSlotValueType m_ConcreteValueType = ConcreteSlotValueType.Vector4;

        public bool isDynamic => m_DynamicDimensionGroup != null;
        public string dynamicDimensionGroup => m_DynamicDimensionGroup;
        public bool IsShrank => m_ConcreteValueType != ConcreteSlotValueType.Vector4;

        public void SetConcreteType(ConcreteSlotValueType valueType)
        {
            if (!isDynamic)
                throw new InvalidOperationException("not dynamic");

            if (valueType == ConcreteSlotValueType.Vector1 || valueType == ConcreteSlotValueType.Vector2 || valueType == ConcreteSlotValueType.Vector3 || valueType == ConcreteSlotValueType.Vector4)
                m_ConcreteValueType = valueType;
            else
                throw new InvalidOperationException("Incompatible concrete type");
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Vector4MaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
