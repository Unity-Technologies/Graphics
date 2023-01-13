using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy;
using UnityEditor.VFX;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class HDUnlitSubTarget : SurfaceSubTarget, IRequiresData<HDUnlitData>
    {
        public HDUnlitSubTarget() => displayName = "Unlit";

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("4516595d40fa52047a77940183dc8e74");  // HDUnlitSubTarget.cs

        static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/"
        };

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Unlit;
        protected override string renderType => HDRenderTypeTags.HDUnlitShader.ToString();
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override string customInspector => "Rendering.HighDefinition.HDUnlitGUI";
        internal override MaterialResetter setupMaterialKeywordsAndPassFunc => UnlitShaderGraphGUI.SetupUnlitKeywordsAndPass;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Unlit SubShader", "");
        protected override string raytracingInclude => CoreIncludes.kUnlitRaytracing;
        protected override string subShaderInclude => CoreIncludes.kUnlit;

        protected override bool supportDistortion => true;
        protected override bool supportForward => true;
        protected override bool supportPathtracing => true;

        HDUnlitData m_UnlitData;

        HDUnlitData IRequiresData<HDUnlitData>.data
        {
            get => m_UnlitData;
            set => m_UnlitData = value;
        }

        public HDUnlitData unlitData
        {
            get => m_UnlitData;
            set => m_UnlitData = value;
        }

        public static FieldDescriptor EnableShadowMatte = new FieldDescriptor(string.Empty, "EnableShadowMatte", "_ENABLE_SHADOW_MATTE");

        void AddShadowMatteIncludes(ref PassCollection.Item unlitShadowPass, bool rayTracingPass)
        {
            unlitShadowPass.descriptor.includes.Add(CoreIncludes.kHDShadow, IncludeLocation.Pregraph, new FieldCondition(EnableShadowMatte, true));
            unlitShadowPass.descriptor.includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph, new FieldCondition(EnableShadowMatte, true));
            unlitShadowPass.descriptor.includes.Add(CoreIncludes.kPunctualLightCommon, IncludeLocation.Pregraph, new FieldCondition(EnableShadowMatte, true));
            unlitShadowPass.descriptor.includes.Add(rayTracingPass ? CoreIncludes.kHDRaytracingShadowLoop : CoreIncludes.kHDShadowLoop, IncludeLocation.Pregraph, new FieldCondition(EnableShadowMatte, true));

            // If we want shadow matte, we need the tangent to world as it is the way to have the normal.
            if (unlitData.enableShadowMatte)
                unlitShadowPass.descriptor.requiredFields.Add(HDStructFields.FragInputs.tangentToWorld);
        }

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            if (unlitData.distortionOnly && builtinData.distortion)
            {
                return new SubShaderDescriptor
                {
                    generatesPreview = true,
                    passes = new PassCollection { { HDShaderPasses.GenerateDistortionPass(false, TargetsVFX(), systemData.tessellation), new FieldCondition(TransparentDistortion, true) } }
                };
            }
            else
            {
                var descriptor = base.GetSubShaderDescriptor();

                // We need to add includes for shadow matte as it's a special case (Lighting includes in an unlit pass)
                var forwardPass = descriptor.passes.FirstOrDefault(p => p.descriptor.lightMode == "ForwardOnly");
                AddShadowMatteIncludes(ref forwardPass, false);

                if (unlitData.enableShadowMatte)
                {
                    // Shadow matte requires world normal provided from constructed TBN.
                    var depthUnlit = descriptor.passes.FirstOrDefault(p => p.descriptor.lightMode == "DepthForwardOnly");
                    depthUnlit.descriptor.requiredFields.Add(HDStructFields.FragInputs.tangentToWorld);
                }

                return descriptor;
            }
        }

        protected override SubShaderDescriptor GetRaytracingSubShaderDescriptor()
        {
            var descriptor = base.GetRaytracingSubShaderDescriptor();

            var gbufferDXRPass = descriptor.passes.FirstOrDefault(p => p.descriptor.lightMode == "GBufferDXR");
            AddShadowMatteIncludes(ref gbufferDXRPass, true);
            var indirectDXRPass = descriptor.passes.FirstOrDefault(p => p.descriptor.lightMode == "IndirectDXR");
            AddShadowMatteIncludes(ref indirectDXRPass, true);
            var forwardDXRPass = descriptor.passes.FirstOrDefault(p => p.descriptor.lightMode == "ForwardDXR");
            AddShadowMatteIncludes(ref forwardDXRPass, true);

            return descriptor;
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            base.CollectPassKeywords(ref pass);

            if (pass.IsForward())
            {
                pass.keywords.Add(CoreKeywordDescriptors.Shadow, new FieldCondition(HDUnlitSubTarget.EnableShadowMatte, true));
                pass.keywords.Add(CoreKeywordDescriptors.ScreenSpaceShadow, new FieldCondition(HDUnlitSubTarget.EnableShadowMatte, true));
            }
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Unlit specific properties
            context.AddField(EnableShadowMatte, unlitData.enableShadowMatte);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            // Unlit specific blocks
            context.AddBlock(HDBlockFields.SurfaceDescription.ShadowTint, unlitData.enableShadowMatte);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new HDUnlitSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Unlit, unlitData));
            if (systemData.surfaceType == SurfaceType.Transparent)
                blockList.AddPropertyBlock(new HDUnlitDistortionPropertyBlock(unlitData));
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            if (unlitData.enableShadowMatte)
            {
                uint mantissa = ((uint)LightFeatureFlags.Punctual | (uint)LightFeatureFlags.Directional | (uint)LightFeatureFlags.Area) & 0x007FFFFFu;
                uint exponent = 0b10000000u; // 0 as exponent
                collector.AddShaderProperty(new Vector1ShaderProperty
                {
                    hidden = true,
                    value = HDShadowUtils.Asfloat((exponent << 23) | mantissa),
                    overrideReferenceName = HDMaterialProperties.kShadowMatteFilter
                });
            }

            // Stencil state for unlit:
            HDSubShaderUtilities.AddStencilShaderProperties(collector, systemData, null, false, supportForward);
        }
    }
}
