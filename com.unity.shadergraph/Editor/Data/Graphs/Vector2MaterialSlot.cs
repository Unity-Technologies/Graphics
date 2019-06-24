using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Vector2MaterialSlot : MaterialSlot, IDynamicDimensionSlot, IMaterialSlotHasValue<Vector2>
    {
        [SerializeField]
        Vector2 m_Value;

        [SerializeField]
        Vector2 m_DefaultValue = Vector2.zero;

        [SerializeField]
        string[] m_Labels;

        public Vector2MaterialSlot()
        {
        }

        public Vector2MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector2 value,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            string label1 = "X",
            string label2 = "Y",
            bool hidden = false,
            string dynamicDimensionGroup = null)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_Value = value;
            m_Labels = new[] { label1, label2 };
            m_DynamicDimensionGroup = dynamicDimensionGroup;
        }

        public Vector2 defaultValue { get { return m_DefaultValue; } }

        public Vector2 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override VisualElement InstantiateControl()
        {
            var labels = m_Labels.Take(concreteValueType.GetChannelCount()).ToArray();
            return new MultiFloatSlotControlView(owner, labels, () => new Vector4(value.x, value.y, 0, 0), (newValue) => value = new Vector2(newValue.x, newValue.y));
        }

        protected override string ConcreteSlotValueAsVariable()
        {
            return concreteValueType == ConcreteSlotValueType.Vector1
                ? NodeUtils.FloatToShaderValue(value.x) : $"$precision2 ({NodeUtils.FloatToShaderValue(value.x)}, {NodeUtils.FloatToShaderValue(value.y)})";
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
            else
                property = new Vector2ShaderProperty() { value = value };

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
                    vector4Value = new Vector4(value.x, 0, 0, 0),
                };
                properties.Add(pp);
            }
            else
            {
                var pp = new PreviewProperty(PropertyType.Vector2)
                {
                    name = name,
                    vector4Value = new Vector4(value.x, value.y, 0, 0),
                };
                properties.Add(pp);
            }
        }

        public override SlotValueType valueType => SlotValueType.Vector2;
        public override ConcreteSlotValueType concreteValueType => m_ConcreteValueType;

        [SerializeField]
        private string m_DynamicDimensionGroup = null;
        private ConcreteSlotValueType m_ConcreteValueType = ConcreteSlotValueType.Vector2;

        public bool isDynamic => m_DynamicDimensionGroup != null;
        public string dynamicDimensionGroup => m_DynamicDimensionGroup;
        public bool IsShrank => m_ConcreteValueType != ConcreteSlotValueType.Vector2;

        public void SetConcreteType(ConcreteSlotValueType valueType)
        {
            if (!isDynamic)
                throw new InvalidOperationException("not dynamic");

            if (valueType == ConcreteSlotValueType.Vector1 || valueType == ConcreteSlotValueType.Vector2)
                m_ConcreteValueType = valueType;
            else
                throw new InvalidOperationException("Incompatible concrete type");
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Vector2MaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
