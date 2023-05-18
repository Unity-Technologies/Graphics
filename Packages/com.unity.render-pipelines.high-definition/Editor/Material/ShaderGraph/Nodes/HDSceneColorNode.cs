using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Input", "High Definition Render Pipeline", "HD Scene Color")]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HDSceneColorNode")]
    class HDSceneColorNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireCameraOpaqueTexture, IMayRequireScreenPosition, IMayRequireNDCPosition, IMayRequirePixelPosition
    {
        public HDSceneColorNode()
        {
            name = "HD Scene Color";
            synonyms = new string[] { "screen buffer" };
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => NodeUtils.GetDocumentationString("HD-Scene-Color");

        [SerializeField]
        bool m_Exposure;
        [ToggleControl]
        public ToggleData exposure
        {
            get => new ToggleData(m_Exposure);
            set
            {
                m_Exposure = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        const int kUvInputSlotId = 0;
        const string kUvInputSlotName = "UV";
        const int kLodInputSlotId = 1;
        const string kLodInputSlotName = "Lod";

        const int kColorOutputSlotId = 2;
        const string kColorOutputSlotName = "Output";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(kUvInputSlotId, kUvInputSlotName, kUvInputSlotName, ScreenSpaceType.Default));
            AddSlot(new Vector1MaterialSlot(kLodInputSlotId, kLodInputSlotName, kLodInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new ColorRGBMaterialSlot(kColorOutputSlotId, kColorOutputSlotName, kColorOutputSlotName, SlotType.Output, Color.black, ColorMode.HDR));

            RemoveSlotsNameNotMatching(new[]
            {
                kUvInputSlotId,
                kLodInputSlotId,
                kColorOutputSlotId,
            });
        }

        string GetFunctionName()
        {
            return "Unity_HDRP_SampleSceneColor_$precision";
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
            {
                s.AppendLine("$precision3 {0}($precision2 uv, $precision lod, $precision exposureMultiplier)", GetFunctionName());
                using (s.BlockScope())
                {
                    if (generationMode.IsPreview())
                    {
                        s.AppendLine("// Sampling the scene color is not supported in the preview");
                        s.AppendLine("return $precision3(0.0, 0.0, 0.0);");
                    }
                    else
                    {
                        if (exposure.isOn)
                        {
                            s.AppendLine("exposureMultiplier = 1.0;");
                        }
                        s.AppendLine("#if defined(REQUIRE_OPAQUE_TEXTURE) && defined(_SURFACE_TYPE_TRANSPARENT) && defined(SHADERPASS)" +
                                     " && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT) && (SHADERPASS != SHADERPASS_PATH_TRACING)" +
                                     " && (SHADERPASS != SHADERPASS_RAYTRACING_VISIBILITY) && (SHADERPASS != SHADERPASS_RAYTRACING_FORWARD)");
                        s.AppendLine("return SampleCameraColor(uv, lod) * exposureMultiplier;");
                        s.AppendLine("#endif");

                        s.AppendLine("#if defined(REQUIRE_OPAQUE_TEXTURE) && defined(CUSTOM_PASS_SAMPLING_HLSL) && defined(SHADERPASS) && (SHADERPASS == SHADERPASS_DRAWPROCEDURAL || SHADERPASS == SHADERPASS_BLIT)");
                        s.AppendLine("return CustomPassSampleCameraColor(uv, lod) * exposureMultiplier;");
                        s.AppendLine("#endif");

                        s.AppendLine("return $precision3(0.0, 0.0, 0.0);");
                    }
                }
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string exposureMultiplier = (exposure.isOn || generationMode.IsPreview()) ? "1.0" : "GetInverseCurrentExposureMultiplier()";
            string uv = GetSlotValue(kUvInputSlotId, generationMode);
            string lod = GetSlotValue(kLodInputSlotId, generationMode);

            sb.AppendLine("$precision3 {0} = {1}({2}.xy, {3}, {4});",
                GetVariableNameForSlot(kColorOutputSlotId),
                GetFunctionName(),
                uv,
                lod,
                exposureMultiplier
            );
        }

        public bool RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability)
        {
            return true;
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All) =>
            FindSlot<MaterialSlot>(kUvInputSlotId)?.RequiresScreenPosition(stageCapability) ?? false;

        public bool RequiresNDCPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All) =>
            FindSlot<MaterialSlot>(kUvInputSlotId)?.RequiresNDCPosition(stageCapability) ?? false;

        public bool RequiresPixelPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All) =>
            FindSlot<MaterialSlot>(kUvInputSlotId)?.RequiresPixelPosition(stageCapability) ?? false;

    }
}
