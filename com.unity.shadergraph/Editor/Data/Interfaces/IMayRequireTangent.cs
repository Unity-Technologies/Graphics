using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireTangent
    {
        NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireTangentExtensions
    {
        public static NeededCoordinateSpace RequiresTangent(this MaterialSlot slot)
        {
            var mayRequireTangent = slot as IMayRequireTangent;
            return mayRequireTangent != null ? mayRequireTangent.RequiresTangent() : NeededCoordinateSpace.None;
        }
    }
}
