using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class EyeSurfaceTypeDebugNode : IStandardNode
    {
        public static string Name => "EyeSurfaceTypeDebug";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    bool isSclera = length(PositionOS.xy) > IrisRadius;
    bool isPupil = !isSclera && length(PositionOS.xy / IrisRadius) < PupilRadius;
    SurfaceColor = IsActive ? (isSclera ? 0.0 : (isPupil ? 1.0 : EyeColor)) : EyeColor;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("PositionOS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("EyeColor", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("IrisRadius", TYPE.Float, Usage.In),
                new ParameterDescriptor("PupilRadius", TYPE.Float, Usage.In),
                new ParameterDescriptor("IsActive", TYPE.Bool, Usage.In),
                new ParameterDescriptor("SurfaceColor", TYPE.Vec3, Usage.Out),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Eye Surface Type Debug",
            tooltip: "Use to visually validate the current pupil radius.",
            category: "Utility/HDRP/Eye",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "PositionOS",
                    displayName: "Position OS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "EyeColor",
                    displayName: "Eye Color",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "IrisRadius",
                    displayName: "Iris Radius",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "PupilRadius",
                    displayName: "Pupil Radius",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "IsActive",
                    displayName: "Is Active",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "SurfaceColor",
                    displayName: "Surface Color",
                    tooltip: ""
                )
            }
        );
    }
}
