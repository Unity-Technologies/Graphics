using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class IrisUVLocationNode : IStandardNode
    {
        public static string Name => "IrisUVLocation";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    irisUVCentered = PositionOS.xy / IrisRadius;
    IrisUV = (irisUVCentered * 0.5 + float2(0.5, 0.5));",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("PositionOS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("IrisRadius", TYPE.Float, Usage.In, new float[] { 0.225f }),
                new ParameterDescriptor("IrisUV", TYPE.Vec2, Usage.Out),
                new ParameterDescriptor("irisUVCentered", TYPE.Vec2, Usage.Local)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Iris UV Location",
            tooltip: "Converts the object position of the cornea/iris to UVs.",
            category: "Utility/HDRP/Eye",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "PositionOS",
                    displayName: "Position OS",
                    tooltip: "Position on the iris Plane in object space"
                ),
                new ParameterUIDescriptor(
                    name: "IrisRadius",
                    displayName: "Iris Radius",
                    tooltip: "The radius of the Iris in the used model"
                ),
                new ParameterUIDescriptor(
                    name: "IrisUV",
                    displayName: "Iris UV",
                    tooltip: "Normalized UV coordinates that can be used to sample an iris texture"
                )
            }
        );
    }
}
