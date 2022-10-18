using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class HDSceneColorNode : IStandardNode
    {
        public static string Name => "HDSceneColor";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
//GetInverseCurrentExposureMultiplier() isn't found?
@"    exposureMultiplier = GetInverseCurrentExposureMultiplier();
    if (Exposure) exposureMultiplier = 1.0;
#if defined(REQUIRE_OPAQUE_TEXTURE) && defined(_SURFACE_TYPE_TRANSPARENT) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)
     && (SHADERPASS != SHADERPASS_PATH_TRACING) && (SHADERPASS != SHADERPASS_RAYTRACING_VISIBILITY) && (SHADERPASS != SHADERPASS_RAYTRACING_FORWARD)
       Out = SampleCameraColor(UV.xy, Lod) * exposureMultiplier;
#elif defined(REQUIRE_OPAQUE_TEXTURE) && defined(CUSTOM_PASS_SAMPLING_HLSL) && defined(SHADERPASS)
     && (SHADERPASS == SHADERPASS_DRAWPROCEDURAL || SHADERPASS == SHADERPASS_BLIT)
       Out = CustomPassSampleCameraColor(UV.xy, Lod) * exposureMultiplier;
#else
       Out = float3(0.0, 0.0, 0.0);
#endif",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                new ParameterDescriptor("Lod", TYPE.Float, Usage.In),
                new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("Exposure", TYPE.Bool, Usage.Static),
                new ParameterDescriptor("exposureMultiplier", TYPE.Float, Usage.Local)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "HD Scene Color",
            tooltip: "Gets the camera's color buffer.",
            category: "Input/HDRP",
            synonyms: new string[2] { "screen", "buffer" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "The screen coordinates to use for the sample",
                    options: REF.OptionList.ScreenPositions
                ),
                new ParameterUIDescriptor(
                    name: "Lod",
                    tooltip: "The mip level to sample"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the sample of the scene color buffer"
                )
            }
        );
    }
}
