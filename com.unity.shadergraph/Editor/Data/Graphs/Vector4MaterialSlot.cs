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
    class Vector4MaterialSlot : MaterialSlot, IMaterialSlotHasValue<Vector4>
    {
        [SerializeField]
        private Vector4 m_Value;

        [SerializeField]
        private Vector4 m_DefaultValue = Vector4.zero;

        [SerializeField]
        string[] m_Labels; // this can be null, which means fallback to k_LabelDefaults

        static readonly string[] k_LabelDefaults = { "X", "Y", "Z", "W" };
        string[] labels
        {
            get
            {
                if ((m_Labels == null) || (m_Labels.Length != k_LabelDefaults.Length))
                    return k_LabelDefaults;
                return m_Labels;
            }
        }

        public Vector4MaterialSlot()
        {
        }

        public Vector4MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector4 value,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            string label1 = null,
            string label2 = null,
            string label3 = null,
            string label4 = null,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_Value = value;
            if ((label1 != null) || (label2 != null) || (label3 != null) || (label4 != null))
            {
                m_Labels = new[]
                {
                    label1 ?? k_LabelDefaults[0],
                    label2 ?? k_LabelDefaults[1],
                    label3 ?? k_LabelDefaults[2],
                    label4 ?? k_LabelDefaults[3]
                };
            }
        }

        public Vector4 defaultValue { get { return m_DefaultValue; } }

        public Vector4 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override bool isDefaultValue => value.Equals(defaultValue);

        public override VisualElement InstantiateControl()
        {
            return new MultiFloatSlotControlView(owner, labels, () => value, (newValue) => value = newValue);
        }

        protected override string ConcreteSlotValueAsVariable()
        {
            return string.Format("$precision4 ({0}, {1}, {2}, {3})"
                , NodeUtils.FloatToShaderValue(value.x)
                , NodeUtils.FloatToShaderValue(value.y)
                , NodeUtils.FloatToShaderValue(value.z)
                , NodeUtils.FloatToShaderValue(value.w));
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var property = new Vector4ShaderProperty()
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddShaderProperty(property);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Vector4)
            {
                name = name,
                vector4Value = new Vector4(value.x, value.y, value.z, value.w),
            };
            properties.Add(pp);
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector4; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector4; } }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Vector4MaterialSlot;
            if (slot != null)
                value = slot.value;
        }

        public override void CopyDefaultValue(MaterialSlot other)
        {
            base.CopyDefaultValue(other);
            if (other is IMaterialSlotHasValue<Vector4> ms)
            {
                m_DefaultValue = ms.defaultValue;
            }
        }
    }
}
