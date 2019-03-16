using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireScreenPosition
    {
        bool RequiresScreenPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireScreenPositionExtensions
    {
        public static bool RequiresScreenPosition(this ISlot slot)
        {
            var mayRequireScreenPosition = slot as IMayRequireScreenPosition;
            return mayRequireScreenPosition != null && mayRequireScreenPosition.RequiresScreenPosition();
        }
    }
}
