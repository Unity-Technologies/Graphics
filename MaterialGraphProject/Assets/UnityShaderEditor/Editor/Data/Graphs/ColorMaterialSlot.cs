using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    public class ColorMaterialSlot : Vector4MaterialSlot
    {
        public ColorMaterialSlot() {}

        public ColorMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector4 value,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, value, shaderStage, hidden)
        {
        }

        public override VisualElement InstantiateControl()
        {
            return new ColorSlotControlView(this);
        }
    }
}
