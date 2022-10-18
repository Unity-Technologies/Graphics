using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class HDSceneDepthNode : IStandardNode
    {
        public static string Name => "HDSceneDepth";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "Linear01",
@"#if defined(REQUIRE_DEPTH_TEXTURE) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)
   int2 coord = int2(UV.xy * _ScreenSize.xy);
   int2 mipCoord  = coord.xy >> int(Lod);
   int2 mipOffset = _DepthPyramidMipLevelOffsets[int(Lod)];
   temp = LOAD_TEXTURE2D_X(_CameraDepthTexture, mipOffset + mipCoord).r;
#else
   temp = 0.0;
#endif
	Out = Linear01Depth(temp, _ZBufferParams);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Lod", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("temp", TYPE.Float, Usage.Local)
                    }
                ),
                new (
                    "Raw",
@"#if defined(REQUIRE_DEPTH_TEXTURE) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)
   int2 coord = int2(UV.xy * _ScreenSize.xy);
   int2 mipCoord  = coord.xy >> int(Lod);
   int2 mipOffset = _DepthPyramidMipLevelOffsets[int(Lod)];
   Out = LOAD_TEXTURE2D_X(_CameraDepthTexture, mipOffset + mipCoord).r;
#else
   Out = 0.0;
#endif",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Lod", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    }
                ),
                new (
                    "Eye",
@"#if defined(REQUIRE_DEPTH_TEXTURE) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)
   int2 coord = int2(UV.xy * _ScreenSize.xy);
   int2 mipCoord  = coord.xy >> int(Lod);
   int2 mipOffset = _DepthPyramidMipLevelOffsets[int(Lod)];
   temp = LOAD_TEXTURE2D_X(_CameraDepthTexture, mipOffset + mipCoord).r;
#else
   temp = 0.0;
#endif
	Out = LinearEyeDepth(temp, _ZBufferParams);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Lod", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("temp", TYPE.Float, Usage.Local)
                    }
                ),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "HD Scene Depth",
            tooltip: "Gets the camera's depth buffer.",
            category: "Input/HDRP",
            synonyms: new string[2] { "z", "buffer" },
            selectableFunctions: new()
            {
                { "Linear01", "Linear01" },
                { "Raw", "Raw" },
                { "Eye", "Eye" }
            },
            functionSelectorLabel: "Sampling Mode",
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
                    tooltip: "the sample of the scene depth buffer"
                )
            }
        );
    }
}
