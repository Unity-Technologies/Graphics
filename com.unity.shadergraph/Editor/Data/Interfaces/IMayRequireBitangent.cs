using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireBitangent
    {
        NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireBitangentExtensions
    {
        public static NeededCoordinateSpace RequiresBitangent(this MaterialSlot slot)
        {
            var mayRequireBitangent = slot as IMayRequireBitangent;
            return mayRequireBitangent != null ? mayRequireBitangent.RequiresBitangent() : NeededCoordinateSpace.None;
        }
    }
}
