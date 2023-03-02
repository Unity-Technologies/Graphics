using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SceneDepthDifferenceNode : IStandardNode
    {
        public static string Name => "SceneDepthDifference";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "Linear01",
@"    dist = Remap01(length(PositionWS), _ProjectionParams.y, _ProjectionParams.z);
#if defined(UNITY_REVERSED_Z)
    Out = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams) - dist;
#else
    Out = dist - Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
#endif",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("PositionWS", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("dist", TYPE.Float, Usage.Local)
                    }
                ),
                new (
                    "Raw",
@"    deviceDepth = ComputeNormalizedDeviceCoordinatesWithZ(PositionWS, GetWorldToHClipMatrix()).z;
#if defined(UNITY_REVERSED_Z)
    Out = deviceDepth - SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy);
#else
    Out = SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy) - deviceDepth;
#endif",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("PositionWS", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("deviceDepth", TYPE.Float, Usage.Local)
                    }
                ),
                new (
                    "Eye",
@"    if (IsPerspectiveProjection())
    {
#if defined(UNITY_REVERSED_Z)
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V) - length(PositionWS);
#else
        Out = length(PositionWS) - LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
#endif
    }
    else
    {
#if defined(UNITY_REVERSED_Z)
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams) - length(PositionWS);
#else
        Out = length(PositionWS) - LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
#endif
    }",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("PositionWS", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    }
                ),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Scene Depth Difference",
            tooltip: "Gets the distance from the current pixel to the closest pixel behind it.",
            category: "Input/HDRP",
            synonyms: new string[3] { "distance", "zbuffer", "zdepth" },
            description: "pkg://Documentation~/previews/SceneDepthDifference.md",
            selectableFunctions: new Dictionary<string, string>
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
                    name: "PositionWS",
                    displayName: "Position WS",
                    tooltip: "The position of the pixel in world space",
                    options: REF.OptionList.Positions
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the distance between the current pixel and the closest pixel behind it."
                )
            }
        );
    }
}
