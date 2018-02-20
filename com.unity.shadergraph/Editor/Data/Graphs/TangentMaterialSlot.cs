using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class TangentMaterialSlot : SpaceMaterialSlot, IMayRequireTangent
    {
        public TangentMaterialSlot()
        {}

        public TangentMaterialSlot(int slotId, string displayName, string shaderOutputName, CoordinateSpace space,
                                   ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, space, shaderStage, hidden)
        {}

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return string.Format("IN.{0}", space.ToVariableName(InterpolatorType.Tangent));
        }

        public NeededCoordinateSpace RequiresTangent()
        {
            if (isConnected)
                return NeededCoordinateSpace.None;
            return space.ToNeededCoordinateSpace();
        }
    }
}
