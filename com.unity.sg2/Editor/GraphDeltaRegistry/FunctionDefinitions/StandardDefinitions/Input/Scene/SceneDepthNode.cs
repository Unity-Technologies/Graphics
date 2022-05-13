using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SceneDepthNode : IStandardNode
    {
        public static string Name = "SceneDepth";
        public static int Version = 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "SceneDepthLinear01",
                    "Out = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);",
                    new ParameterDescriptor("UV", TYPE.Vec4, GraphType.Usage.In, REF.ScreenPosition_Default),
                    new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "SceneDepthRaw",
                    "Out = SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy);",
                    new ParameterDescriptor("UV", TYPE.Vec4, GraphType.Usage.In, REF.ScreenPosition_Default),
                    new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "SceneDepthEye",
@"if (unity_OrthoParams.w == 1.0)
{
Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
}
else
{
Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
}",
                    new ParameterDescriptor("UV", TYPE.Vec4, GraphType.Usage.In, REF.ScreenPosition_Default),
                    new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the camera's depth buffer.",
            categories: new string[2] { "Input", "Scene" },
            synonyms: new string[2] { "zbuffer", "zdepth" },
            displayName: "Scene Depth",
            selectableFunctions: new()
            {
                { "SceneDepthLinear01", "Linear01" },
                { "SceneDepthRaw", "Raw" },
                { "SceneDepthEye", "Eye" }
            },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "Normalized screen coordinates",
                    options: REF.OptionList.ScreenPositions
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "Output value"
                )
            }
        );
    }
}
