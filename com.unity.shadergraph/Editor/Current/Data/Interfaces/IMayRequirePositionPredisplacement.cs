using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequirePositionPredisplacement
    {
        NeededCoordinateSpace RequiresPositionPredisplacement(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequirePositionPredisplacementExtensions
    {
        public static NeededCoordinateSpace RequiresPositionPredisplacement(this MaterialSlot slot)
        {
            var mayRequirePositionPredisplacement = slot as IMayRequirePositionPredisplacement;
            return mayRequirePositionPredisplacement != null ? mayRequirePositionPredisplacement.RequiresPositionPredisplacement() : NeededCoordinateSpace.None;
        }
    }
}
