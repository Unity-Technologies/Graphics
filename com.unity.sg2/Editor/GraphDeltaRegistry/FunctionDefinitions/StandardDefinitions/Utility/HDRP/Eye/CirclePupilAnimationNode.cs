using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class CirclePupilAnimationNode : IStandardNode
    {
        public static string Name => "CirclePupilAnimation";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    // Compute the normalized iris position
    irisUVCentered = (IrisUV - 0.5f) * 2.0f;

    // Compute the radius of the point inside the eye
    localIrisRadius = length(irisUVCentered);

    // First based on the pupil aperture, let's define the new position of the pupil
    newPupilRadius = PupilAperture > 0.5 ? lerp(PupilRadius, MaxPupilAperture, (PupilAperture - 0.5) * 2.0) : lerp(MinPupilAperture, PupilRadius, PupilAperture * 2.0);

    // If we are inside the pupil
    newIrisRadius = localIrisRadius < newPupilRadius ? ((PupilRadius / newPupilRadius) * localIrisRadius) : 1.0 - ((1.0 - PupilRadius) / (1.0 - newPupilRadius)) * (1.0 - localIrisRadius);
    AnimatedIrisUV = irisUVCentered / localIrisRadius * newIrisRadius;

    // Convert it back to UV space.
    AnimatedIrisUV = (AnimatedIrisUV * 0.5 + float2(0.5, 0.5));",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("IrisUV", TYPE.Vec2, Usage.In),
                new ParameterDescriptor("PupilRadius", TYPE.Float, Usage.In),
                new ParameterDescriptor("PupilAperture", TYPE.Float, Usage.In),
                new ParameterDescriptor("MinPupilAperture", TYPE.Float, Usage.In),
                new ParameterDescriptor("MaxPupilAperture", TYPE.Float, Usage.In),
                new ParameterDescriptor("AnimatedIrisUV", TYPE.Vec2, Usage.Out),
                new ParameterDescriptor("irisUVCentered", TYPE.Vec2, Usage.Local),
                new ParameterDescriptor("localIrisRadius", TYPE.Float, Usage.Local),
                new ParameterDescriptor("newPupilRadius", TYPE.Float, Usage.Local),
                new ParameterDescriptor("newIrisRadius", TYPE.Float, Usage.Local)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Circle Pupil Animation",
            tooltip: "Applies a deformation to a normalized Iris UV to simulate the opening and closing of the pupil.",
            category: "Utility/HDRP/Eye",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/CirclePupilAnimation.md",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "IrisUV",
                    displayName: "Iris UV",
                    tooltip: "Normalized UV coordinates that can be used to sample an iris texture"
                ),
                new ParameterUIDescriptor(
                    name: "PupilRadius",
                    displayName: "Pupil Radius",
                    tooltip: "Radius of the pupil in the iris texture as a percentage"
                ),
                new ParameterUIDescriptor(
                    name: "PupilAperture",
                    displayName: "Pupil Aperture",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "MinPupilAperture",
                    displayName: "Min Pupil Aperture",
                    tooltip: "The minimum size of the pupil aperture"
                ),
                new ParameterUIDescriptor(
                    name: "MaxPupilAperture",
                    displayName: "Max Pupil Aperture",
                    tooltip: "The maximum size of the pupil aperture"
                ),
                new ParameterUIDescriptor(
                    name: "AnimatedIrisUV",
                    displayName: "Animated Iris UV",
                    tooltip: "Normalized iris UV coordinates with animation for pupil expansion/contraction"
                )
            }
        );
    }
}
