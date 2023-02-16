using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class URPSampleBufferNode : IStandardNode
    {
        public static string Name => "URPSampleBuffer";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "NormalWorldSpace",
                    "Out = SHADERGRAPH_SAMPLE_SCENE_NORMAL(UV.xy);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                    }
                ),
                new (
                    "MotionVectors",
//TODO:Same issue as the HD sample buffer node, not sure where to put the property
@"TEXTURE2D(_MotionVectorTexture);
uint2 pixelCoords = uint2(UV.xy * _ScreenSize.xy);
Out = LOAD_TEXTURE2D_X_LOD(_MotionVectorTexture, pixelCoords, 0).xy;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out)                    }
                ),
                new (
                    "BlitSource",
//TODO: same issue as line 25
@"TEXTURE2D(_BlitTexture);
SAMPLER(sampler_BlitTexture);
uint2 pixelCoords = uint2(UV.xy * _ScreenSize.xy);
Out = LOAD_TEXTURE2D_X_LOD(_BlitTexture, pixelCoords, 0);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Out", TYPE.Vec4, Usage.Out)                    }
                ),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "URP Sample Buffer",
            tooltip: "Return data from the selected buffer.",
            category: "Input/URP",
            synonyms: new string[3] { "normal", "motion vector", "blit" },
            description: "pkg://Documentation~/previews/URPSampleBuffer.md",
            selectableFunctions: new()
            {
                { "NormalWorldSpace", "Normal World Space" },
                { "MotionVectors", "Motion Vectors" },
                { "BlitSource", "Blit Source" }
            },
            hasModes: true,
            functionSelectorLabel: "Source Buffer",
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the screen coordinates to use for the sample",
                    options: REF.OptionList.ScreenPositions
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the sample of the selected buffer"
                )
            }
        );
    }
}
