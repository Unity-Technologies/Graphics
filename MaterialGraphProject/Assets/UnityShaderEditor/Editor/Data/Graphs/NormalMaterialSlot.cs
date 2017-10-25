using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class NormalMaterialSlot : Vector3MaterialSlot, IMayRequireNormal
    {
        private CoordinateSpace m_Space = CoordinateSpace.World;

        public CoordinateSpace space
        {
            get { return m_Space; }
            set { m_Space = value; }
        }

        public NormalMaterialSlot(int slotId, string displayName, string shaderOutputName, CoordinateSpace space,
            ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector3.zero, shaderStage, hidden)
        {
            this.space = space;
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return space.ToVariableName(InterpolatorType.Normal);
        }

        public NeededCoordinateSpace RequiresNormal()
        {
            if (isConnected)
                return NeededCoordinateSpace.None;
            return space.ToNeededCoordinateSpace();
        }
    }
}