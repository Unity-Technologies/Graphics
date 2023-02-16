using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class HDSampleBufferNode : IStandardNode
    {
        public static string Name => "HDSampleBuffer";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "NormalWorldSpace",
@"   uint2 pixelCoords = uint2(UV.xy * _ScreenSize.xy);
   NormalData normalData;
   DecodeFromNormalBuffer(pixelCoords, normalData);
   Out = normalData.normalWS;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl\"",
                        "\"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl\""
                    }
                ),
                new (
                    "Smoothness",
@"   uint2 pixelCoords = uint2(UV.xy * _ScreenSize.xy);
   NormalData normalData;
   DecodeFromNormalBuffer(pixelCoords, normalData);
   Out = IsSky(pixelCoords) ? 1 : RoughnessToPerceptualSmoothness(PerceptualRoughnessToRoughness(normalData.perceptualRoughness));",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl\"",
                        "\"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl\""
                    }
                ),
                new (
                    "MotionVectors",
@"   uint2 pixelCoords = uint2(UV.xy * _ScreenSize.xy);
   float4 motionVecBufferSample = LOAD_TEXTURE2D_X_LOD(_CameraMotionVectorsTexture, pixelCoords, 0);
   float2 motionVec;
   DecodeMotionVector(motionVecBufferSample, motionVec);
   Out = motionVec;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl\"",
                        "\"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl\""
                    }
                ),
                new (
                    "IsSky",
@"   Out = IsSky(UV.xy) ? 1 : 0;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl\"",
                        "\"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl\""
                    }
                ),
                new (
                    "PostProcessInput",
//The first two lines of body code live outside the function in SG1.  Does that matter?
//In SG1, _CustomPostProcessInput is a dynamic variable. How do we do that?
@"   TEXTURE2D_X(_CustomPostProcessInput);
   SAMPLER(sampler_CustomPostProcessInput);
   uint2 pixelCoords = uint2(UV.xy * _ScreenSize.xy);
   Out = LOAD_TEXTURE2D_X_LOD(_CustomPostProcessInput, pixelCoords, 0);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Out", TYPE.Vec4, Usage.Out)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl\"",
                        "\"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl\""
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "HD Sample Buffer",
            tooltip: "Gets data from the selected buffer.",
            category: "Input/HDRP",
            synonyms: new string[2] { "screen", "buffer" },
            description: "pkg://Documentation~/previews/HDSampleBuffer.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "NormalWorldSpace", "Normal World Space" },
                { "Smoothness", "Smoothness" },
                { "MotionVectors", "Motion Vectors" },
                { "IsSky", "Is Sky" },
                { "PostProcessInput", "Post Process Input" }
            },
            hasModes: true,
            functionSelectorLabel: "Source Buffer",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "The screen coordinates to use for the sample",
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
