using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireFaceSign
    {
        bool RequiresFaceSign(ShaderStageCapability stageCapability = ShaderStageCapability.Fragment);
    }

    static class IMayRequireFaceSignExtensions
    {
        public static bool RequiresFaceSign(this MaterialSlot slot)
        {
            var mayRequireFaceSign = slot as IMayRequireFaceSign;
            return mayRequireFaceSign != null && mayRequireFaceSign.RequiresFaceSign();
        }
    }
}
