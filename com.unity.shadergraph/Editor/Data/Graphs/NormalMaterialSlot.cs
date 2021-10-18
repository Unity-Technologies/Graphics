using System;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class NormalMaterialSlot : SpaceMaterialSlot, IMayRequireNormal
    {
        public NormalMaterialSlot()
        { }

        public NormalMaterialSlot(int slotId, string displayName, string shaderOutputName, CoordinateSpace space,
                                  ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, space, stageCapability, hidden)
        { }

        public override VisualElement InstantiateControl()
        {
            return new LabelSlotControlView(space + " Space");
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            // HACK: we don't define AbsoluteWorldSpaceNormal, but it is the same as WorldSpaceNormal
            var coordSpace = (space == CoordinateSpace.AbsoluteWorld) ? CoordinateSpace.World : space;
            return string.Format("IN.{0}", coordSpace.ToVariableName(InterpolatorType.Normal));
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            if (isConnected)
                return NeededCoordinateSpace.None;
            // HACK: we don't define AbsoluteWorldSpaceNormal, but it is the same as WorldSpaceNormal
            var coordSpace = (space == CoordinateSpace.AbsoluteWorld) ? CoordinateSpace.World : space;
            return coordSpace.ToNeededCoordinateSpace();
        }
    }
}
