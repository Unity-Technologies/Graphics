using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class PowerNode : IStandardNode
    {
        public static string Name = "Power";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "    Out = UnsignedBase ? pow(abs(Base), Exp) : pow(Base, Exp);",
            new ParameterDescriptor("Base", TYPE.Any, Usage.In),
            new ParameterDescriptor("Exp", TYPE.Any, Usage.In, new float[] { 2f, 2f, 2f, 2f }),
            new ParameterDescriptor("UnsignedBase", TYPE.Bool, Usage.Static, new float[] { 1f }),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Multiplies Base by itself the number of times given by Exp.",
            categories: new string[2] { "Math", "Basic" },
            synonyms: new string[2] { "Exponentiation", "^" },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "Base",
                    tooltip: "Base"
                ),
                new ParameterUIDescriptor(
                    name: "Exp",
                    tooltip: "Exponent"
                ),
                new ParameterUIDescriptor(
                    name: "UnsignedBase",
                    tooltip: "Performing power on negative values results in a NaN. When true, this feature prevents that."
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "Base raised to the power of Exp"
                )
            }
        );
    }
}
