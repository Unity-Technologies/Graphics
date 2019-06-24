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
    class Vector3MaterialSlot : MaterialSlot, IDynamicDimensionSlot, IMaterialSlotHasValue<Vector3>
    {
        [SerializeField]
        private Vector3 m_Value;

        [SerializeField]
        private Vector3 m_DefaultValue = Vector3.zero;

        [SerializeField]
        string[] m_Labels;

        public Vector3MaterialSlot()
        {
        }

        public Vector3MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector3 value,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            string label1 = "X",
            string label2 = "Y",
            string label3 = "Z",
            bool hidden = false,
            string dynamicDimensionGroup = null)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_Value = value;
            m_Labels = new[] { label1, label2, label3 };
            m_DynamicDimensionGroup = dynamicDimensionGroup;
        }

        public Vector3 defaultValue { get { return m_DefaultValue; } }

        public Vector3 value
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
            else
                return $"$precision3 ({NodeUtils.FloatToShaderValue(value.x)}, {NodeUtils.FloatToShaderValue(value.y)}, {NodeUtils.FloatToShaderValue(value.z)})";
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
            else
                property = new Vector3ShaderProperty() { value = value };

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
            else if (m_ConcreteValueType == ConcreteSlotValueType.Vector2)
            {
                var pp = new PreviewProperty(PropertyType.Vector2)
                {
                    name = name,
                    vector4Value = new Vector4(value.x, value.y, 0, 0),
                };
                properties.Add(pp);
            }
            else
            {
                var pp = new PreviewProperty(PropertyType.Vector3)
                {
                    name = name,
                    vector4Value = new Vector4(value.x, value.y, value.z, 0)
                };
                properties.Add(pp);
            }
        }

        public override SlotValueType valueType => SlotValueType.Vector3;
        public override ConcreteSlotValueType concreteValueType => m_ConcreteValueType;

        [SerializeField]
        private string m_DynamicDimensionGroup = null;
        private ConcreteSlotValueType m_ConcreteValueType = ConcreteSlotValueType.Vector3;

        public bool isDynamic => m_DynamicDimensionGroup != null;
        public string dynamicDimensionGroup => m_DynamicDimensionGroup;
        public bool IsShrank => m_ConcreteValueType != ConcreteSlotValueType.Vector3;

        public void SetConcreteType(ConcreteSlotValueType valueType)
        {
            if (!isDynamic)
                throw new InvalidOperationException("not dynamic");

            if (valueType == ConcreteSlotValueType.Vector1 || valueType == ConcreteSlotValueType.Vector2 || valueType == ConcreteSlotValueType.Vector3)
                m_ConcreteValueType = valueType;
            else
                throw new InvalidOperationException("Incompatible concrete type");
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Vector3MaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
