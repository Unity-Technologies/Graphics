using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Vector1MaterialSlot : MaterialSlot, IMaterialSlotHasValue<float>, IMaterialSlotSupportsLiteralMode
    {
        [SerializeField]
        float m_Value;

        [SerializeField]
        float m_DefaultValue;

        [SerializeField]
        string[] m_Labels; // this can be null, which means fallback to k_LabelDefaults

        static readonly string[] k_LabelDefaults = { "X" };
        string[] labels
        {
            get
            {
                if ((m_Labels == null) || (m_Labels.Length != k_LabelDefaults.Length))
                    return k_LabelDefaults;
                return m_Labels;
            }
        }

        [SerializeField]
        bool m_LiteralMode = false;

        public bool LiteralMode
        {
            get => m_LiteralMode;
            set => m_LiteralMode = value;
        }

        internal override bool canHideConnector => true;
        public Vector1MaterialSlot()
        {
        }

        public Vector1MaterialSlot(int slotId, Vector1ShaderProperty fromProperty)
           : this(slotId, fromProperty.displayName, fromProperty.referenceName, SlotType.Input, fromProperty.value, literal: fromProperty.LiteralFloatMode)
        {
        }

        public Vector1MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            float value,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            string label1 = null,
            bool hidden = false,
            bool literal = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_DefaultValue = value;
            m_Value = value;
            m_LiteralMode = literal;
            if (label1 != null)
                m_Labels = new[] { label1 };
        }

        public float defaultValue { get { return m_DefaultValue; } }

        public float value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override bool isDefaultValue => value.Equals(defaultValue);

        public override VisualElement InstantiateControl()
        {
            return hideConnector
                ? new FloatSlotControlView(this)
                : new MultiFloatSlotControlView(owner, labels, () => new Vector4(value, 0f, 0f, 0f), (newValue) => value = newValue.x);
        }

        protected override string ConcreteSlotValueAsVariable()
        {
            return string.Format("$precision({0})", NodeUtils.FloatToShaderValue(value));
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var property = new Vector1ShaderProperty()
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddShaderProperty(property);
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector1; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector1; } }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Float)
            {
                name = name,
                floatValue = value,
            };
            properties.Add(pp);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            if (foundSlot is IMaterialSlotSupportsLiteralMode literal)
                LiteralMode = literal.LiteralMode;

            switch(foundSlot)
            {
                case IMaterialSlotHasValue<float> slot1: value = slot1.value; break;
                case IMaterialSlotHasValue<Vector2> slot2: value = slot2.value.x; break;
                case IMaterialSlotHasValue<Vector3> slot3: value = slot3.value.x; break;
                case IMaterialSlotHasValue<Vector4> slot4: value = slot4.value.x; break;
            }
        }

        public override void CopyDefaultValue(MaterialSlot other)
        {
            base.CopyDefaultValue(other);
            if (other is IMaterialSlotHasValue<float> ms)
            {
                m_DefaultValue = ms.defaultValue;
            }
        }

        class FloatSlotControlView : VisualElement
        {
            Vector1MaterialSlot m_Slot;

            public FloatSlotControlView(Vector1MaterialSlot slot)
            {
                m_Slot = slot;
                var integerField = slot.hideConnector
                    ? new FloatField(slot.RawDisplayName())
                    : new FloatField();

                integerField.value = slot.value;
                integerField.RegisterValueChangedCallback(OnValueChange);
                Add(integerField);
            }

            void OnValueChange(ChangeEvent<float> evt)
            {
                if (evt.newValue != m_Slot.value)
                {
                    m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Integer Change");
                    m_Slot.value = evt.newValue;
                    m_Slot.owner.Dirty(ModificationScope.Node);
                }
            }
        }
    }
}
