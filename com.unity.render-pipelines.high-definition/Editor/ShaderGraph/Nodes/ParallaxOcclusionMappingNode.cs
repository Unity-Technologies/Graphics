using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Rendering.HighDefinition;
using System;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [Title("Utility", "High Definition Render Pipeline", "Parallax Occlusion Mapping")]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.ParallaxOcclusionMappingNode")]
    class ParallaxOcclusionMappingNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireViewDirection, IMayRequireMeshUV
    {
        public ParallaxOcclusionMappingNode()
        {
            name = "Parallax Occlusion Mapping";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("SGNode-Parallax-Occlusion-Mapping");

        // Input slots
        private const int kHeightmapSlotId = 2;
        private const string kHeightmapSlotName = "Heightmap";
        private const int kHeightmapSamplerSlotId = 3;
        private const string kHeightmapSamplerSlotName = "HeightmapSampler";
        private const int kAmplitudeSlotId = 4;
        private const string kAmplitudeSlotName = "Amplitude";
        private const int kStepsSlotId = 5;
        private const string kStepsSlotName = "Steps";
        private const int kUVsSlotId = 6;
        private const string kUVsSlotName = "UVs";
        private const int kLodSlotId = 7;
        private const string kLodSlotName = "Lod";
        private const int kLodThresholdSlotId = 8;
        private const string kLodThresholdSlotName = "LodThreshold";


        // Output slots
        private const int kPixelDepthOffsetOutputSlotId = 0;
        private const string kPixelDepthOffsetOutputSlotName = "PixelDepthOffset";
        private const int kParallaxUVsOutputSlotId = 1;
        private const string kParallaxUVsOutputSlotName = "ParallaxUVs";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture2DInputMaterialSlot(kHeightmapSlotId, kHeightmapSlotName, kHeightmapSlotName, ShaderStageCapability.Fragment));
            AddSlot(new SamplerStateMaterialSlot(kHeightmapSamplerSlotId, kHeightmapSamplerSlotName, kHeightmapSamplerSlotName, SlotType.Input));
            AddSlot(new Vector1MaterialSlot(kAmplitudeSlotId, kAmplitudeSlotName, kAmplitudeSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kStepsSlotId, kStepsSlotName, kStepsSlotName, SlotType.Input, 5.0f, ShaderStageCapability.Fragment));
            AddSlot(new UVMaterialSlot(kUVsSlotId, kUVsSlotName, kUVsSlotName, UVChannel.UV0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kLodSlotId, kLodSlotName, kLodSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kLodThresholdSlotId, kLodThresholdSlotName, kLodThresholdSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));

            AddSlot(new Vector1MaterialSlot(kPixelDepthOffsetOutputSlotId, kPixelDepthOffsetOutputSlotName, kPixelDepthOffsetOutputSlotName, SlotType.Output, 0.0f, ShaderStageCapability.Fragment));
            AddSlot(new Vector2MaterialSlot(kParallaxUVsOutputSlotId, kParallaxUVsOutputSlotName, kParallaxUVsOutputSlotName, SlotType.Output, Vector2.zero, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new[] {
                kPixelDepthOffsetOutputSlotId,
                kParallaxUVsOutputSlotId,
                kHeightmapSlotId,
                kHeightmapSamplerSlotId,
                kAmplitudeSlotId,
                kStepsSlotId,
                kUVsSlotId,
                kLodSlotId,
                kLodThresholdSlotId,
            });
        }

        string GetFunctionName()
        {
            return $"Unity_HDRP_ParallaxOcclusionMapping_{concretePrecision.ToShaderString()}";
        }

        public override void ValidateNode()
        {
            var textureSlot = FindInputSlot<Texture2DInputMaterialSlot>(kHeightmapSlotId);
            textureSlot.defaultType = Texture2DShaderProperty.DefaultType.Black;

            base.ValidateNode();
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            string perPixelDisplacementInclude = @"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl""";

            // Texture sample inputs
            var samplerSlot = FindInputSlot<MaterialSlot>(kHeightmapSamplerSlotId);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);
            var heightmap = GetSlotValue(kHeightmapSlotId, generationMode);

            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("$precision3 GetDisplacementObjectScale()");
                    using (s.BlockScope())
                    {
                        s.AppendLines(@"
float3 objectScale = float3(1.0, 1.0, 1.0);
float4x4 worldTransform = GetWorldToObjectMatrix();

objectScale.x = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
objectScale.z = length(float3(worldTransform._m20, worldTransform._m21, worldTransform._m22));

return objectScale;");
                    }

                    s.AppendLine("// Required struct and function for the ParallaxOcclusionMapping function:");
                    s.AppendLine("struct PerPixelHeightDisplacementParam");
                    using (s.BlockSemicolonScope())
                    {
                        s.AppendLine("$precision2 uv;");
                    }
                    s.AppendLine("$precision ComputePerPixelHeightDisplacement($precision2 texOffsetCurrent, $precision lod, PerPixelHeightDisplacementParam param)");
                    using (s.BlockScope())
                    {
                        s.AppendLine("return SAMPLE_TEXTURE2D_LOD({0}, {1}, param.uv + texOffsetCurrent, lod).r;",
                            heightmap,
                            edgesSampler.Any() ? GetSlotValue(kHeightmapSamplerSlotId, generationMode) : "sampler" + heightmap);
                    }
                    s.Append(perPixelDisplacementInclude);
                });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string amplitude = GetSlotValue(kAmplitudeSlotId, generationMode);
            string steps = GetSlotValue(kStepsSlotId, generationMode);
            string uvs = GetSlotValue(kUVsSlotId, generationMode);
            string lod = GetSlotValue(kLodSlotId, generationMode);
            string lodThreshold = GetSlotValue(kLodThresholdSlotId, generationMode);

            string tmpPOMParam = GetVariableNameForNode() + "_POM";
            string tmpViewDir = GetVariableNameForNode() + "_ViewDir";
            string tmpNdotV = GetVariableNameForNode() + "_NdotV";
            string tmpMaxHeight = GetVariableNameForNode() + "_MaxHeight";
            string tmpViewDirUV = GetVariableNameForNode() + "_ViewDirUV";
            string tmpOutHeight = GetVariableNameForNode() + "_OutHeight";

            sb.AppendLines(String.Format(@"
$precision3 {4} = IN.{2} * GetDisplacementObjectScale().xzy;
$precision {5} = {4}.z;
$precision {6} = {3} * 0.01;

// Transform the view vector into the UV space.
$precision3 {7}    = normalize($precision3({4}.xy * {6}, {4}.z)); // TODO: skip normalize

PerPixelHeightDisplacementParam {0};
{0}.uv = {1};",
                tmpPOMParam,
                uvs,
                CoordinateSpace.Tangent.ToVariableName(InterpolatorType.ViewDirection),
                amplitude, // cm in the interface so we multiply by 0.01 in the shader to convert in meter
                tmpViewDir,
                tmpNdotV,
                tmpMaxHeight,
                tmpViewDirUV
                ));

            sb.AppendLines(String.Format(@"
$precision {10};
$precision2 {0} = {8} + ParallaxOcclusionMapping({1}, {2}, {3}, {4}, {5}, {10});

$precision {6} = ({7} - {10} * {7}) / max({9}, 0.0001);
",
                GetVariableNameForSlot(kParallaxUVsOutputSlotId),
                lod,
                lodThreshold,
                steps,
                tmpViewDirUV,
                tmpPOMParam,
                GetVariableNameForSlot(kPixelDepthOffsetOutputSlotId),
                tmpMaxHeight,
                uvs,
                tmpNdotV,
                tmpOutHeight
                ));
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return NeededCoordinateSpace.Tangent;
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            if (channel != UVChannel.UV0)
                return false;

            if (IsSlotConnected(kUVsSlotId))
                return false;

            return true;
        }
    }
}
