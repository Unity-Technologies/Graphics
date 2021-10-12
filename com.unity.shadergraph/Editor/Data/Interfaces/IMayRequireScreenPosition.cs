using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireScreenPosition
    {
        bool RequiresScreenPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    interface IMayRequireNDCPosition
    {
        bool RequiresNDCPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    interface IMayRequirePixelPosition
    {
        bool RequiresPixelPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireScreenPositionExtensions
    {
        public static bool RequiresScreenPosition(this MaterialSlot slot, ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            var mayRequireScreenPosition = slot as IMayRequireScreenPosition;
            return mayRequireScreenPosition?.RequiresScreenPosition(stageCapability) ?? false;
        }

        public static bool RequiresNDCPosition(this MaterialSlot slot, ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            var mayRequireNDCPosition = slot as IMayRequireNDCPosition;
            return mayRequireNDCPosition?.RequiresNDCPosition(stageCapability) ?? false;
        }

        public static bool RequiresPixelPosition(this MaterialSlot slot, ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            var mayRequirePixelPosition = slot as IMayRequirePixelPosition;
            return mayRequirePixelPosition?.RequiresPixelPosition(stageCapability) ?? false;
        }
    }
}
