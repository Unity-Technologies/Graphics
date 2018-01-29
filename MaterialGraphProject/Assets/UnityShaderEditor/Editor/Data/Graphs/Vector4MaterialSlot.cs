using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Vector4MaterialSlot : MaterialSlot, IMaterialSlotHasValue<Vector4>
    {
        [SerializeField]
        private Vector4 m_Value;

        [SerializeField]
        private Vector4 m_DefaultValue;

        public Vector4MaterialSlot()
        {
        }

        public Vector4MaterialSlot(
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
            return new MultiFloatSlotControlView(owner, 4, () => value, (newValue) => value = newValue);
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "4 (" + NodeUtils.FloatToShaderValue(value.x) + "," + NodeUtils.FloatToShaderValue(value.y) + "," + NodeUtils.FloatToShaderValue(value.z) + "," + NodeUtils.FloatToShaderValue(value.w) + ")";
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

        public override PreviewProperty GetPreviewProperty(string name)
        {
            var pp = new PreviewProperty(PropertyType.Vector4)
            {
                name = name,
                vector4Value = new Vector4(value.x, value.y, value.z, value.w),
            };
            return pp;
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector4; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector4; } }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Vector4MaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
