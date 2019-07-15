using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class ProceduralUVMaterialSlot : MaterialSlot
    {
        public override ConcreteSlotValueType concreteValueType => ConcreteSlotValueType.Vector2;
        public override SlotValueType valueType => SlotValueType.Vector2;

        public override VisualElement InstantiateControl()
            => new LabelSlotControlView(shaderOutputName);

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        { }

        public ProceduralUVMaterialSlot()
        { }

        public ProceduralUVMaterialSlot(int slotId, string displayName, string shaderOutputName,
                                    ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, stageCapability, hidden: hidden)
        { }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        { }
    }
}
