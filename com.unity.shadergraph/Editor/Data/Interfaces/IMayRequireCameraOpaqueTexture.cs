using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireCameraOpaqueTexture
    {
        bool RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireCameraOpaqueTextureExtensions
    {
        public static bool RequiresCameraOpaqueTexture(this ISlot slot)
        {
            var mayRequireCameraOpaqueTexture = slot as IMayRequireCameraOpaqueTexture;
            return mayRequireCameraOpaqueTexture != null && mayRequireCameraOpaqueTexture.RequiresCameraOpaqueTexture();
        }
    }
}
