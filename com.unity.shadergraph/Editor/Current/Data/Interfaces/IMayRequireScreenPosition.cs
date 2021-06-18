using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireScreenPosition
    {
        bool RequiresScreenPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireScreenPositionExtensions
    {
        public static bool RequiresScreenPosition(this MaterialSlot slot, ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            var mayRequireScreenPosition = slot as IMayRequireScreenPosition;
            return mayRequireScreenPosition != null && mayRequireScreenPosition.RequiresScreenPosition(stageCapability);
        }
    }
}
