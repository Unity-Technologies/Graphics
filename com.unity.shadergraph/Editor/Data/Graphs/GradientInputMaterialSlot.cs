using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class GradientInputMaterialSlot : GradientMaterialSlot, IMaterialSlotHasValue<Gradient>
    {
        [SerializeField]
        Gradient m_Value = new Gradient();

        [SerializeField]
        Gradient m_DefaultValue = new Gradient();

        public GradientInputMaterialSlot()
        {
        }

        public GradientInputMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, stageCapability, hidden)
        {
        }

        public Gradient value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public Gradient defaultValue { get { return m_DefaultValue; } }

        public override VisualElement InstantiateControl()
        {
            return new GradientSlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            if (generationMode.IsPreview())
                return GradientUtil.GetGradientForPreview(matOwner.GetVariableNameForSlot(id));

            return ConcreteSlotValueAsVariable();
        }

        protected override string ConcreteSlotValueAsVariable()
        {
            return GradientUtil.GetGradientValue(value, "");
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            if (generationMode != GenerationMode.Preview)
                return;

            GradientUtil.GetGradientPropertiesForPreview(properties, matOwner.GetVariableNameForSlot(id), value);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            properties.Add(new PreviewProperty(PropertyType.Gradient)
            {
                name = name,
                gradientValue = value
            });
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as GradientInputMaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
