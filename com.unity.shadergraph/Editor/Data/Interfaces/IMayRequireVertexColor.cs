using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireVertexColor
    {
        bool RequiresVertexColor(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireVertexColorExtensions
    {
        public static bool RequiresVertexColor(this ISlot slot)
        {
            var mayRequireVertexColor = slot as IMayRequireVertexColor;
            return mayRequireVertexColor != null && mayRequireVertexColor.RequiresVertexColor();
        }
    }
}
