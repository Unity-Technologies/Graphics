using UnityEngine;
using UnityEditor.Graphing;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Parallax Occlusion Mapping")]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.ParallaxOcclusionMappingNode")]
    [FormerName("UnityEditor.Rendering.HighDefinition.ParallaxOcclusionMappingNode")]
    class ParallaxOcclusionMappingNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireViewDirection, IMayRequireMeshUV
    {
        public ParallaxOcclusionMappingNode()
        {
            name = "Parallax Occlusion Mapping";
            UpdateNodeAfterDeserialization();
        }

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
            RemoveSlotsNameNotMatching(new[]
            {
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

        string GetFunctionName() => $"Unity_ParallaxOcclusionMapping{GetVariableNameForNode()}_{concretePrecision.ToShaderString()}";

        public override void Setup()
        {
            base.Setup();
            var textureSlot = FindInputSlot<Texture2DInputMaterialSlot>(kHeightmapSlotId);
            textureSlot.defaultType = Texture2DShaderProperty.DefaultType.Black;
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            string perPixelDisplacementInclude = @"#include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl""";

            // Texture sample inputs
            var samplerSlot = FindInputSlot<MaterialSlot>(kHeightmapSamplerSlotId);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);
            var heightmap = GetSlotValue(kHeightmapSlotId, generationMode);

            // We first generate components that can be used by multiple POM node
            registry.ProvideFunction("Unique", s =>
            {
                s.AppendLine("struct PerPixelHeightDisplacementParam");
                using (s.BlockSemicolonScope())
                {
                    s.AppendLine("$precision2 uv;");
                }
                s.AppendNewLine();
                s.AppendLine($"$precision3 GetDisplacementObjectScale()");
                using (s.BlockScope())
                {
                    s.AppendLines(@"
float3 objectScale = float3(1.0, 1.0, 1.0);
float4x4 worldTransform = GetWorldToObjectMatrix();

objectScale.x = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
objectScale.z = length(float3(worldTransform._m20, worldTransform._m21, worldTransform._m22));

return objectScale;");
                }
            });

            // Then we add the functions that are specific to this node
            registry.ProvideFunction(GetFunctionName(), s =>
            {
                s.AppendLine("// Required struct and function for the ParallaxOcclusionMapping function:");
                s.AppendLine($"$precision ComputePerPixelHeightDisplacement_{GetVariableNameForNode()}($precision2 texOffsetCurrent, $precision lod, PerPixelHeightDisplacementParam param, TEXTURE2D_PARAM(heightTexture, heightSampler))");
                using (s.BlockScope())
                {
                    s.AppendLine("return SAMPLE_TEXTURE2D_LOD(heightTexture, heightSampler, param.uv + texOffsetCurrent, lod).r;");
                }
                // heightmap,
                // edgesSampler.Any() ? GetSlotValue(kHeightmapSamplerSlotId, generationMode) : "sampler" + heightmap);

                s.AppendLine($"#define ComputePerPixelHeightDisplacement ComputePerPixelHeightDisplacement_{GetVariableNameForNode()}");
                s.AppendLine($"#define POM_NAME_ID {GetVariableNameForNode()}");
                s.AppendLine($"#define POM_USER_DATA_PARAMETERS , TEXTURE2D_PARAM(heightTexture, samplerState)");
                s.AppendLine($"#define POM_USER_DATA_ARGUMENTS , TEXTURE2D_ARGS(heightTexture, samplerState)");
                s.AppendLine(perPixelDisplacementInclude);
                s.AppendLine($"#undef ComputePerPixelHeightDisplacement");
                s.AppendLine($"#undef POM_NAME_ID");
                s.AppendLine($"#undef POM_USER_DATA_PARAMETERS");
                s.AppendLine($"#undef POM_USER_DATA_ARGUMENTS");
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string amplitude = GetSlotValue(kAmplitudeSlotId, generationMode);
            string steps = GetSlotValue(kStepsSlotId, generationMode);
            string uvs = GetSlotValue(kUVsSlotId, generationMode);
            string lod = GetSlotValue(kLodSlotId, generationMode);
            string lodThreshold = GetSlotValue(kLodThresholdSlotId, generationMode);
            string heightmap = GetSlotValue(kHeightmapSlotId, generationMode);
            string sampler = GetSlotValue(kHeightmapSamplerSlotId, generationMode);

            string tmpPOMParam = GetVariableNameForNode() + "_POM";
            string tmpViewDir = GetVariableNameForNode() + "_ViewDir";
            string tmpNdotV = GetVariableNameForNode() + "_NdotV";
            string tmpMaxHeight = GetVariableNameForNode() + "_MaxHeight";
            string tmpViewDirUV = GetVariableNameForNode() + "_ViewDirUV";
            string tmpOutHeight = GetVariableNameForNode() + "_OutHeight";

            sb.AppendLines($@"
$precision3 {tmpViewDir} = IN.{CoordinateSpace.Tangent.ToVariableName(InterpolatorType.ViewDirection)} * GetDisplacementObjectScale().xzy;
$precision {tmpNdotV} = {tmpViewDir}.z;
$precision {tmpMaxHeight} = {amplitude} * 0.01; // cm in the interface so we multiply by 0.01 in the shader to convert in meter

// Transform the view vector into the UV space.
$precision3 {tmpViewDirUV}    = normalize($precision3({tmpViewDir}.xy * {tmpMaxHeight}, {tmpViewDir}.z)); // TODO: skip normalize

PerPixelHeightDisplacementParam {tmpPOMParam};
{tmpPOMParam}.uv = {uvs};");

            sb.AppendLines($@"
$precision {tmpOutHeight};
$precision2 {GetVariableNameForSlot(kParallaxUVsOutputSlotId)} = {uvs} + ParallaxOcclusionMapping{GetVariableNameForNode()}({lod}, {lodThreshold}, {steps}, {tmpViewDirUV}, {tmpPOMParam}, {tmpOutHeight}, TEXTURE2D_ARGS({heightmap}.tex, {sampler}.samplerstate));

$precision {GetVariableNameForSlot(kPixelDepthOffsetOutputSlotId)} = ({tmpMaxHeight} - {tmpOutHeight} * {tmpMaxHeight}) / max({tmpNdotV}, 0.0001);
");
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
