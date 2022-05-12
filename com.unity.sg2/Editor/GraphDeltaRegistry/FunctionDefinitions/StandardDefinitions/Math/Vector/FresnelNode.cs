using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class FresnelNode : IStandardNode
    {
        public static string Name = "Fresnel";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);",
            new ParameterDescriptor("Normal", TYPE.Vec3, Usage.In, defaultValue: REF.WorldSpace_Normal),
            new ParameterDescriptor("ViewDir", TYPE.Vec3, Usage.In, defaultValue: REF.WorldSpace_ViewDirection),
            new ParameterDescriptor("Power", TYPE.Float, Usage.In, new float[] { 1f }),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Fresnel Effect",
            tooltip: "Calculates the inverted dot product between the view direction and the normal.",
            categories: new string[2] { "Math", "Vector" },
            synonyms: new string[0],
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "Normal",
                    tooltip: "normal direction"
                ),
                new ParameterUIDescriptor(
                    name: "ViewDir",
                    tooltip: "view direction"
                ),
                new ParameterUIDescriptor(
                    name: "Power",
                    tooltip: "exponent of the power calculation"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the inverted dot product between the view direction and the normal"
                )
            }
        );
    }
}
