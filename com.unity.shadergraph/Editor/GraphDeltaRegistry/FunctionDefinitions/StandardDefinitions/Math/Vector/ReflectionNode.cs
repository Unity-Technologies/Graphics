using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class ReflectionNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Reflection", // Name
            "Out = reflect(In, Normal);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Normal", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Vector" },
            { "Name.Synonyms", "mirror" },
            { "Tooltip", "creates a new vector mirrored around the axis of the input normal" },
            { "Parameters.In.Tooltip", "a vector to mirror" },
            { "Parameters.Normal.Tooltip", "the facing direction of the surface" },
            { "Parameters.Out.Tooltip", "the In vector mirrored around the axis of the Normal" }
        };
    }
}
