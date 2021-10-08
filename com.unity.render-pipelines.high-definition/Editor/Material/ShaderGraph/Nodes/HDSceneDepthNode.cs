using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using System.Reflection;

namespace UnityEditor.Rendering.HighDefinition
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Input", "High Definition Render Pipeline", "HD Scene Depth")]
    sealed class HDSceneDepthNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireScreenPosition, IMayRequireDepthTexture
    {
        const string k_ScreenPositionSlotName = "UV";
        const string k_LodInputSlotName = "Lod";
        const string k_OutputSlotName = "Output";

        const int k_ScreenPositionSlotId = 0;
        const int k_LodInputSlotId = 1;
        const int k_OutputSlotId = 2;

        [SerializeField]
        private DepthSamplingMode m_DepthSamplingMode = DepthSamplingMode.Linear01;

        [EnumControl("Sampling Mode")]
        public DepthSamplingMode depthSamplingMode
        {
            get { return m_DepthSamplingMode; }
            set
            {
                if (m_DepthSamplingMode == value)
                    return;

                m_DepthSamplingMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override string documentationURL => Documentation.GetPageLink("SGNode-HD-Scene-Depth");

        public HDSceneDepthNode()
        {
            name = "HD Scene Depth";
            synonyms = new string[] { "hdzbuffer", "hdzdepth" };
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(k_ScreenPositionSlotId, k_ScreenPositionSlotName, k_ScreenPositionSlotName, ScreenSpaceType.Default));
            AddSlot(new Vector1MaterialSlot(k_LodInputSlotId, k_LodInputSlotName, k_LodInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, 0, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(new[]
            {
                k_ScreenPositionSlotId,
                k_LodInputSlotId,
                k_OutputSlotId,
            });
        }

        string GetFunctionName() => "Unity_HDRP_SampleSceneDepth_$precision";

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            // Preview SG doesn't have access to HDRP depth buffer
            if (!generationMode.IsPreview())
            {
                registry.builder.AppendLine("StructuredBuffer<int2>  _DepthPyramidMipLevelOffsets;");
                registry.builder.AppendLine("float4  _DepthPyramidBufferSize;");

                registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("$precision {0}($precision2 uv, $precision lod)", GetFunctionName());
                    using (s.BlockScope())
                    {
                        s.AppendLine("#if defined(REQUIRE_DEPTH_TEXTURE) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)");
                        s.AppendLine("float2 uvOffset = _DepthPyramidMipLevelOffsets[int(lod)] * _DepthPyramidBufferSize.zw;");
                        s.AppendLine("$precision2 UVScale = _RTHandleScale.xy * (_ScreenSize.xy / _DepthPyramidBufferSize.xy);");
                        s.AppendLine("$precision lodScale = exp2(uint(lod));");
                        s.AppendLine("$precision2 lodUV = (uv * UVScale) / lodScale;");
                        s.AppendLine("$precision2 halfTextel = _DepthPyramidBufferSize.zw * 0.5;");
                        s.AppendLine("$precision2 lodSize = _DepthPyramidBufferSize.zw * _ScreenSize.xy / lodScale;");
                        s.AppendLine("$precision2 clampedUV = clamp(uvOffset + lodUV, uvOffset + halfTextel, uvOffset + lodSize - halfTextel);");
                        s.AppendLine("return SAMPLE_TEXTURE2D_X(_CameraDepthTexture, s_linear_clamp_sampler, clampedUV).r;");
                        s.AppendLine("#endif");

                        s.AppendLine("return 0.0;");
                    }
                });
            }
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
            {
                sb.AppendLine("$precision3 {0} = 0.0;", GetVariableNameForSlot(k_OutputSlotId));
            }
            else
            {
                string uv = GetSlotValue(k_ScreenPositionSlotId, generationMode);
                string lod = GetSlotValue(k_LodInputSlotId, generationMode);
                string depth = $"{GetFunctionName()}({uv}.xy, {lod})";

                if (depthSamplingMode == DepthSamplingMode.Eye)
                    depth = $"LinearEyeDepth({depth}, _ZBufferParams)";
                if (depthSamplingMode == DepthSamplingMode.Linear01)
                    depth = $"Linear01Depth({depth}, _ZBufferParams)";

                sb.AppendLine($"$precision3 {GetVariableNameForSlot(k_OutputSlotId)} = {depth};");
            }
        }

        public bool RequiresDepthTexture(ShaderStageCapability stageCapability) => true;

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All) => true;
    }
}
