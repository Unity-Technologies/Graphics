using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireNormal
    {
        NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireNormalExtensions
    {
        public static NeededCoordinateSpace RequiresNormal(this ISlot slot)
        {
            var mayRequireNormal = slot as IMayRequireNormal;
            return mayRequireNormal != null ? mayRequireNormal.RequiresNormal() : NeededCoordinateSpace.None;
        }
    }
}
