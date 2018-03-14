using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class GradientInputMaterialSlot : GradientMaterialSlot
    {
        [SerializeField]
        private Gradient m_Gradient = new Gradient();

        public Gradient gradient
        {
            get { return m_Gradient; }
            set { m_Gradient = value; }
        }

        public GradientInputMaterialSlot()
        {
        }

        public GradientInputMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, shaderStage, hidden)
        {
        }

        public override VisualElement InstantiateControl()
        {
            return new GradientSlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return string.Format("Unity{0}()", matOwner.GetVariableNameForSlot(id));
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var prop = new GradientShaderProperty();
            prop.overrideReferenceName = matOwner.GetVariableNameForSlot(id);
            prop.generatePropertyBlock = false;
            prop.value = gradient;
            properties.AddShaderProperty(prop);
        }

        public override PreviewProperty GetPreviewProperty(string name)
        {
            var pp = new PreviewProperty(PropertyType.Gradient)
            {
                name = name,
                gradientValue = gradient
            };
            return pp;
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as GradientInputMaterialSlot;
            if (slot != null)
                m_Gradient = slot.gradient;
        }
    }
}