using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireBarycentricCoordinates
    {
        bool RequiresBarycentricCoordinates(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireBarycentricCoordinatesExtensions
    {
        public static bool RequiresBarycentricCoordinates(this MaterialSlot slot)
        {
            var mayRequireBarycentricCoordinates = slot as IMayRequireBarycentricCoordinates;
            return mayRequireBarycentricCoordinates != null && mayRequireBarycentricCoordinates.RequiresBarycentricCoordinates();
        }
    }
}
