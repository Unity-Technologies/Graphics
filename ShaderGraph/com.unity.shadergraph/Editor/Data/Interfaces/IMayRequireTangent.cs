using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public interface IMayRequireTangent
    {
        NeededCoordinateSpace RequiresTangent();
    }

    public static class MayRequireTangentExtensions
    {
        public static NeededCoordinateSpace RequiresTangent(this ISlot slot)
        {
            var mayRequireTangent = slot as IMayRequireTangent;
            return mayRequireTangent != null ? mayRequireTangent.RequiresTangent() : NeededCoordinateSpace.None;
        }
    }
}
