using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class SphereMaskNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "SphereMask",
            "Out = 1 - saturate((distance(Coords, Center) - Radius) / (1 - Hardness));",
            new ParameterDescriptor("Coords", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Center", TYPE.Vector, Usage.In, new float[] { 0.5f, 0.5f, 0.5f, 0.5f }),
            new ParameterDescriptor("Radius", TYPE.Float, Usage.In, new float[] { 0.1f, 0.1f, 0.1f, 0.1f }),
            new ParameterDescriptor("Hardness", TYPE.Float, Usage.In, new float[] { 0.8f, 0.8f, 0.8f, 0.8f }),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Vector" },
            { "DisplayName", "Sphere Mask" },
            { "Tooltip", "creates a spherical volume mask originating at the given position" },
            { "Parameters.Coords.Tooltip", "Coordinate space input" },
            { "Parameters.Center.Tooltip", "Coordinates of the sphere origin" },
            { "Parameters.Radius.Tooltip", "Radius of the sphere" },
            { "Parameters.Hardness.Tooltip", "Soften falloff of the sphere" },
            { "Parameters.Out.Tooltip", "a spherical volume mask originating at the given position" }
        };
    }
}
