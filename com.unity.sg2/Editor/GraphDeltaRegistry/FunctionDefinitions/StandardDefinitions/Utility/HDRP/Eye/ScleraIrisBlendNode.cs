using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ScleraIrisBlendNode : IStandardNode
    {
        public static string Name => "ScleraIrisBlend";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    osRadius = length(PositionOS.xy);
    outerBlendRegionRadius = IrisRadius + 0.02;
    blendLerpFactor = 1.0 - (osRadius - IrisRadius) / (0.04);
    blendLerpFactor = pow(blendLerpFactor, 8.0);
    blendLerpFactor = 1.0 - blendLerpFactor;
    SurfaceMask = (osRadius > outerBlendRegionRadius) ? 0.0 : ((osRadius < IrisRadius) ? 1.0 : (lerp(1.0, 0.0, blendLerpFactor)));
    EyeColor = lerp(ScleraColor, IrisColor, SurfaceMask);
    DiffuseNormal = lerp(ScleraNormal, IrisNormal, SurfaceMask);
    SpecularNormal = lerp(ScleraNormal, float3(0.0, 0.0, 1.0), SurfaceMask);
    EyeSmoothness = lerp(ScleraSmoothness, CorneaSmoothness, SurfaceMask);
    SurfaceDiffusionProfile = lerp(DiffusionProfileSclera, DiffusionProfileIris, floor(SurfaceMask));",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("ScleraColor", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("ScleraNormal", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("ScleraSmoothness", TYPE.Float, Usage.In),
                new ParameterDescriptor("IrisColor", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("IrisNormal", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("CorneaSmoothness", TYPE.Float, Usage.In),
                new ParameterDescriptor("IrisRadius", TYPE.Float, Usage.In),
                new ParameterDescriptor("PositionOS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("DiffusionProfileSclera", TYPE.Float, Usage.In),
                new ParameterDescriptor("DiffusionProfileIris", TYPE.Float, Usage.In),
                new ParameterDescriptor("EyeColor", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("SurfaceMask", TYPE.Float, Usage.Out),
                new ParameterDescriptor("DiffuseNormal", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("SpecularNormal", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("EyeSmoothness", TYPE.Float, Usage.Out),
                new ParameterDescriptor("SurfaceDiffusionProfile", TYPE.Float, Usage.Out),
                new ParameterDescriptor("osRadius", TYPE.Float, Usage.Local),
                new ParameterDescriptor("outerBlendRegionRadius", TYPE.Float, Usage.Local),
                new ParameterDescriptor("blendLerpFactor", TYPE.Float, Usage.Local),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Sclera Iris Blend",
            tooltip: "combines the seperate components of the eye into unified material parameters",
            category: "Utility/HDRP/Eye",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[16] {
                new ParameterUIDescriptor(
                    name: "ScleraColor",
                    displayName: "Sclera Color",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "ScleraNormal",
                    displayName: "Sclera Normal",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "ScleraSmoothness",
                    displayName: "Sclera Smoothness",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "IrisColor",
                    displayName: "Iris Color",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "IrisNormal",
                    displayName: "Iris Normal",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "CorneaSmoothness",
                    displayName: "Cornea Smoothness",
                    tooltip: ""
                ),
               new ParameterUIDescriptor(
                    name: "IrisRadius",
                    displayName: "Iris Radius",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "PositionOS",
                    displayName: "Position OS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "DiffusionProfileSclera",
                    displayName: "Diffusion Profile Sclera",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "DiffusionProfileIris",
                    displayName: "Diffusion Profile Iris",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "EyeColor",
                    displayName: "Eye Color",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "SurfaceMask",
                    displayName: "Surface Mask",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "DiffuseNormal",
                    displayName: "Diffuse Normal",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "SpecularNormal",
                    displayName: "Specular Normal",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "EyeSmoothness",
                    displayName: "Eye Smoothness",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "SurfaceDiffusionProfile",
                    displayName: "Surface Diffusion Profile",
                    tooltip: ""
                )
            }
        );
    }
}
