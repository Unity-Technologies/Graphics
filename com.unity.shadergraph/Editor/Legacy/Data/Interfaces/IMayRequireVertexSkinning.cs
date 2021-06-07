using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireVertexSkinning
    {
        bool RequiresVertexSkinning(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireVertexSkinningExtensions
    {
        public static bool RequiresVertexSkinning(this MaterialSlot slot)
        {
            var mayRequireVertexSkinning = slot as IMayRequireVertexSkinning;
            return mayRequireVertexSkinning != null && mayRequireVertexSkinning.RequiresVertexSkinning();
        }
    }
}
