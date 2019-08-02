using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    // Checks if the fragment shader needs access to the screen pixel coordinate built-in shader value
    // AKA: SV_POSITION, VPOS, gl_FragCoord, ...
    interface IMayRequireRequirePixelCoordinate
    {
        bool RequiresPixelCoordinate(ShaderStageCapability stageCapability = ShaderStageCapability.Fragment);
    }

    static class MayRequirePixelCoordinateExtensions
    {
        public static bool RequiresPixelCoordinate(this AbstractMaterialNode node)
        {
            return node is IMayRequireRequirePixelCoordinate mayRequirePixelCoordinate && mayRequirePixelCoordinate.RequiresPixelCoordinate();
        }

        public static bool RequiresPixelCoordinate(this ISlot slot)
        {
            var mayRequirePixelCoordinate = slot as IMayRequireRequirePixelCoordinate;
            return mayRequirePixelCoordinate != null && mayRequirePixelCoordinate.RequiresPixelCoordinate();
        }
    }
}
