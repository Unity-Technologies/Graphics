using System;

namespace UnityEngine.MaterialGraph
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
    
    public enum CoordinateSpace
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
    }
}