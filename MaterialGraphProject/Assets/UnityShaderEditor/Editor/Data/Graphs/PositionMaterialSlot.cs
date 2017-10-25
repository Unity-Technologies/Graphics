using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class PositionMaterialSlot : Vector3MaterialSlot, IMayRequirePosition
    {
        private CoordinateSpace m_Space = CoordinateSpace.World;

        public CoordinateSpace space
        {
            get { return m_Space; }
            set { m_Space = value; }
        }

        public PositionMaterialSlot(int slotId, string displayName, string shaderOutputName, CoordinateSpace space,
            ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector3.zero, shaderStage, hidden)
        {
            this.space = space;
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return space.ToVariableName(InterpolatorType.Position);
        }

        public NeededCoordinateSpace RequiresPosition()
        {
            if (isConnected)
                return NeededCoordinateSpace.None;
            return space.ToNeededCoordinateSpace();
        }
    }
}