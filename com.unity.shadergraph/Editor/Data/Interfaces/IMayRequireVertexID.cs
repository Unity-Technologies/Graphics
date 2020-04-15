using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireVertexID
    {
        bool RequiresVertexID(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireVertexIDExtensions
    {
        public static bool RequiresVertexID(this ISlot slot)
        {
            var mayRequireVertexID = slot as IMayRequireVertexID;
            return mayRequireVertexID != null && mayRequireVertexID.RequiresVertexID();
        }
    }
}
