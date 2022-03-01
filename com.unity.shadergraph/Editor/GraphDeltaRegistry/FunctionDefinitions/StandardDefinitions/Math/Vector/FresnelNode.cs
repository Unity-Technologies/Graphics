using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class FresnelNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Fresnel",
            "Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);",
            new ParameterDescriptor("Normal", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("ViewDir", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("Power", TYPE.Float, Usage.In, new float[] { 1f, 1f, 1f, 1f }),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Vector" },
            { "DisplayName", "Fresnel Effect" },
            { "Tooltip", "returns the inverted dot product between the view direction and the normal" },
            { "Parameters.Normal.Tooltip", "Normal direction" },
            { "Parameters.ViewDir.Tooltip", "View direction" },
            { "Parameters.Power.Tooltip", "Exponent of the power calculation" },
            { "Parameters.Out.Tooltip", "the inverted dot product between the view direction and the normal" }
        };
    }
}
