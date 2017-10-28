using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class ViewDirectionMaterialSlot : Vector3MaterialSlot, IMayRequireViewDirection
    {
        private CoordinateSpace m_Space = CoordinateSpace.World;

        public CoordinateSpace space
        {
            get { return m_Space; }
            set { m_Space = value; }
        }

        public ViewDirectionMaterialSlot(int slotId, string displayName, string shaderOutputName, CoordinateSpace space,
            ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector3.zero, shaderStage, hidden)
        {
            this.space = space;
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return space.ToVariableName(InterpolatorType.ViewDirection);
        }

        public NeededCoordinateSpace RequiresViewDirection()
        {
            if (isConnected)
                return NeededCoordinateSpace.None;
            return space.ToNeededCoordinateSpace();
        }
    }
}
