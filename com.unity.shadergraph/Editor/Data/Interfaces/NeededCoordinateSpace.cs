using System;
using System.Linq;

namespace UnityEditor.ShaderGraph.Internal
{
    [Flags]
    public enum NeededCoordinateSpace
    {
        None = 0,
        Object = 1 << 0,
        View = 1 << 1,
        World = 1 << 2,
        Tangent = 1 << 3,
        AbsoluteWorld = 1 << 4
    }

    public enum CoordinateSpace
    {
        Object,
        View,
        World,
        Tangent,
        AbsoluteWorld
    }

    public enum InterpolatorType
    {
        Normal,
        BiTangent,
        Tangent,
        ViewDirection,
        Position,
        PositionPredisplacement,
    }

    public static class CoordinateSpaceExtensions
    {
        static int s_SpaceCount = Enum.GetValues(typeof(CoordinateSpace)).Length;
        static int s_InterpolatorCount = Enum.GetValues(typeof(InterpolatorType)).Length;
        static string[] s_VariableNames = new string[s_SpaceCount * s_InterpolatorCount];

        public static string ToVariableName(this CoordinateSpace space, InterpolatorType type)
        {
            var index = (int)space + (int)type * s_SpaceCount;
            if (string.IsNullOrEmpty(s_VariableNames[index]))
                s_VariableNames[index] = string.Format("{0}Space{1}", space, type);
            return s_VariableNames[index];
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
                case CoordinateSpace.AbsoluteWorld:
                    return NeededCoordinateSpace.AbsoluteWorld;
                default:
                    throw new ArgumentOutOfRangeException(nameof(space), space, null);
            }
        }

        public static CoordinateSpace ToCoordinateSpace(this NeededCoordinateSpace space)
        {
            switch (space)
            {
                case NeededCoordinateSpace.Object:
                    return CoordinateSpace.Object;
                case NeededCoordinateSpace.View:
                    return CoordinateSpace.View;
                case NeededCoordinateSpace.World:
                    return CoordinateSpace.World;
                case NeededCoordinateSpace.Tangent:
                    return CoordinateSpace.Tangent;
                case NeededCoordinateSpace.AbsoluteWorld:
                    return CoordinateSpace.AbsoluteWorld;
                default:
                    throw new ArgumentOutOfRangeException(nameof(space), space, null);
            }
        }
    }
}
