using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireViewDirection
    {
        NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireViewDirectionExtensions
    {
        public static NeededCoordinateSpace RequiresViewDirection(this MaterialSlot slot)
        {
            var mayRequireViewDirection = slot as IMayRequireViewDirection;
            return mayRequireViewDirection != null ? mayRequireViewDirection.RequiresViewDirection() : NeededCoordinateSpace.None;
        }
    }
}
