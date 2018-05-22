using System;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class PositionMaterialSlot : SpaceMaterialSlot, IMayRequirePosition
    {
        public PositionMaterialSlot()
        {}

        public PositionMaterialSlot(int slotId, string displayName, string shaderOutputName, CoordinateSpace space,
                                    ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, space, shaderStage, hidden)
        {}

        public override VisualElement InstantiateControl()
        {
            return new LabelSlotControlView(space + " Space");
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return string.Format("IN.{0}", space.ToVariableName(InterpolatorType.Position));
        }

        public NeededCoordinateSpace RequiresPosition()
        {
            if (isConnected)
                return NeededCoordinateSpace.None;
            return space.ToNeededCoordinateSpace();
        }
    }
}
