using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class BitangentMaterialSlot : Vector3MaterialSlot, IMayRequireBitangent
    {
        private CoordinateSpace m_Space = CoordinateSpace.World;

        public CoordinateSpace space
        {
            get { return m_Space; }
            set { m_Space = value; }
        }

        public BitangentMaterialSlot(int slotId, string displayName, string shaderOutputName, CoordinateSpace space,
            ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector3.zero, shaderStage, hidden)
        {
            this.space = space;
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return space.ToVariableName(InterpolatorType.BiTangent);
        }

        public NeededCoordinateSpace RequiresBitangent()
        {
            if (isConnected)
                return NeededCoordinateSpace.None;
            return space.ToNeededCoordinateSpace();
        }
    }
}