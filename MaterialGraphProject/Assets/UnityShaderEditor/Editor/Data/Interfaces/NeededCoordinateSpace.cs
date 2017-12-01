using System;

namespace UnityEditor.ShaderGraph
{
    [Flags]
    public enum NeededCoordinateSpace
    {
        None = 0,
        Object = 1 << 0,
        View = 1 << 1,
        World = 1 << 2,
        Tangent = 1 << 3
    }

    public enum CoordinateSpace : int
    {
        Object,
        View,
        World,
        Tangent
    }

    public enum InterpolatorType
    {
        Normal,
        BiTangent,
        Tangent,
        ViewDirection,
        Position
    }

    public static class CoordinateSpaceNameExtensions
    {
        public static string ToVariableName(this CoordinateSpace space, InterpolatorType type)
        {
            return string.Format("{0}Space{1}", space, type);
        }

        public static NeededCoordinateSpace ToNeededCoordinateSpace(this CoordinateSpace space)
        {
            switch (space)
            {
                case CoordinateSpace.Object:
                    return NeededCoordinateSpace.Object;
                case CoordinateSpace.View:
                    return NeededCoordinateSpace.View;
                case CoordinateSpace.World:
                    return NeededCoordinateSpace.World;
                case CoordinateSpace.Tangent:
                    return NeededCoordinateSpace.Tangent;
                default:
                    throw new ArgumentOutOfRangeException("space", space, null);
            }
        }
    }
}
