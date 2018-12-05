using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireDepthTexture
    {
        bool RequiresDepthTexture(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireDepthTextureExtensions
    {
        public static bool RequiresDepthTexture(this ISlot slot)
        {
            var mayRequireDepthTexture = slot as IMayRequireDepthTexture;
            return mayRequireDepthTexture != null && mayRequireDepthTexture.RequiresDepthTexture();
        }
    }
}
