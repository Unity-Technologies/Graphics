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
    class Vector3MaterialSlot : MaterialSlot, IMaterialSlotHasValue<Vector3>
    {
        [SerializeField]
        private Vector3 m_Value;

        [SerializeField]
        private Vector3 m_DefaultValue = Vector3.zero;

        [SerializeField]
        string[] m_Labels; // this can be null, which means fallback to k_LabelDefaults

        static readonly string[] k_LabelDefaults = { "X", "Y", "Z" };
        string[] labels
        {
            get
            {
                if ((m_Labels == null) || (m_Labels.Length != k_LabelDefaults.Length))
                    return k_LabelDefaults;
                return m_Labels;
            }
        }

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
            string label1 = null,
            string label2 = null,
            string label3 = null,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_Value = value;
            m_DefaultValue = value;
            if ((label1 != null) || (label2 != null) || (label3 != null))
            {
                m_Labels = new[]
                {
                    label1 ?? k_LabelDefaults[0],
                    label2 ?? k_LabelDefaults[1],
                    label3 ?? k_LabelDefaults[2]
                };
            }
        }

        public Vector3 defaultValue { get { return m_DefaultValue; } }

        public Vector3 value
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
            return string.Format("$precision3 ({0}, {1}, {2})"
                , NodeUtils.FloatToShaderValue(value.x)
                , NodeUtils.FloatToShaderValue(value.y)
                , NodeUtils.FloatToShaderValue(value.z));
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var property = new Vector3ShaderProperty()
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddShaderProperty(property);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Vector3)
            {
                name = name,
                vector4Value = new Vector4(value.x, value.y, value.z, 0)
            };
            properties.Add(pp);
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector3; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector3; } }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Vector3MaterialSlot;
            if (slot != null)
                value = slot.value;
        }

        public override void CopyDefaultValue(MaterialSlot other)
        {
            base.CopyDefaultValue(other);
            if (other is IMaterialSlotHasValue<Vector3> ms)
            {
                m_DefaultValue = ms.defaultValue;
            }
        }
    }
}
