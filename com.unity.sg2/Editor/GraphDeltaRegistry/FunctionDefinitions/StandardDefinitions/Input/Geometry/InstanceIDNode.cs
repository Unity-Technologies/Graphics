using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class InstanceIDNode : IStandardNode
    {
        public static string Name => "InstanceID";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"#if UNITY_ANY_INSTANCING_ENABLED
    Out = InstanceID;
#else
    Out = 0;
#endif
",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out),
                new ParameterDescriptor("InstanceID", TYPE.Float, GraphType.Usage.Local, REF.InstanceID)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Provides the unique id of individual instances or zero when instances aren't in use.",
            category: "Input/Geometry",
            synonyms: new string[0] { },
            displayName: "Instance ID",
            hasPreview:false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "Instance ID for mesh of a given instanced draw call."
                )
            }
        );
    }
}
