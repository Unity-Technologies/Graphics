using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequirePosition
    {
        NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequirePositionExtensions
    {
        public static NeededCoordinateSpace RequiresPosition(this MaterialSlot slot)
        {
            var mayRequirePosition = slot as IMayRequirePosition;
            return mayRequirePosition != null ? mayRequirePosition.RequiresPosition() : NeededCoordinateSpace.None;
        }
    }
}
