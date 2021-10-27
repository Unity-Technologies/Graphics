using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class PropertyConnectionStateMaterialSlot : MaterialSlot, IMaterialSlotHasValue<bool>
    {
        public PropertyConnectionStateMaterialSlot()
        { }

        public PropertyConnectionStateMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
        }

        public override VisualElement InstantiateControl()
        {
            return new PropertyConnectionStateSlotControlView(this);
        }

        protected override string ConcreteSlotValueAsVariable()
        {
            // This is a funky slot, that doesn't directly hold a value.
            return "false";
        }

        public bool defaultValue
        {
            // This is a funky slot, that doesn't directly hold a value.
            get { return false; }
        }

        public bool value
        {
            // This is a funky slot, that doesn't directly hold a value.
            get { return false; }
        }

        public override bool isDefaultValue => true;

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            if (owner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var property = new BooleanShaderProperty()
            {
                overrideReferenceName = owner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = isConnected
            };
            properties.AddShaderProperty(property);
        }

        public override SlotValueType valueType { get { return SlotValueType.PropertyConnectionState; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.PropertyConnectionState; } }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            // This is a funky slot, that doesn't directly hold a value.
        }
    }
}
