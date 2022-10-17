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
                new ParameterDescriptor("IrisRadius", TYPE.Float, Usage.In, new float[] { 0.02f }),
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
                    tooltip: "Position of the current fragment to shade in object space "
                ),
                new ParameterUIDescriptor(
                    name: "EyeColor",
                    displayName: "Eye Color",
                    tooltip: "Final diffuse color of the eye"
                ),
                new ParameterUIDescriptor(
                    name: "IrisRadius",
                    displayName: "Iris Radius",
                    tooltip: "The radius of the Iris in the used model"
                ),
                new ParameterUIDescriptor(
                    name: "PupilRadius",
                    displayName: "Pupil Radius",
                    tooltip: "Radius of the pupil in the iris texture as a percentage"
                ),
                new ParameterUIDescriptor(
                    name: "IsActive",
                    displayName: "Is Active",
                    tooltip: "Flag that defines if the node should be active"
                ),
                new ParameterUIDescriptor(
                    name: "SurfaceColor",
                    displayName: "Surface Color",
                    tooltip: "Final diffuse color of the eye"
                )
            }
        );
    }
}
