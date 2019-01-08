using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireBitangent
    {
        NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireBitangentExtensions
    {
        public static NeededCoordinateSpace RequiresBitangent(this ISlot slot)
        {
            var mayRequireBitangent = slot as IMayRequireBitangent;
            return mayRequireBitangent != null ? mayRequireBitangent.RequiresBitangent() : NeededCoordinateSpace.None;
        }
    }
}
