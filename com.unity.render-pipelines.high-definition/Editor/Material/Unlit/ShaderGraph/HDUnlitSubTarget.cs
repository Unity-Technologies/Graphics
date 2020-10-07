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
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/",
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/"
        };

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Unlit;
        protected override string renderType => HDRenderTypeTags.HDUnlitShader.ToString();
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override string customInspector => "Rendering.HighDefinition.HDUnlitGUI";
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

        public static FieldDescriptor EnableShadowMatte =        new FieldDescriptor(string.Empty, "EnableShadowMatte", "_ENABLE_SHADOW_MATTE");

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            if (unlitData.distortionOnly && builtinData.distortion)
            {
                return new SubShaderDescriptor
                {
                    generatesPreview = true,
                    passes = new PassCollection{ { HDShaderPasses.GenerateDistortionPass(false), new FieldCondition(TransparentDistortion, true) } }
                };
            }
            else
            {
                var descriptor = base.GetSubShaderDescriptor();

                // We need to add includes for shadow matte as it's a special case (Lighting includes in an unlit pass)
                var forwardUnlit = descriptor.passes.FirstOrDefault(p => p.descriptor.lightMode == "ForwardOnly");

                forwardUnlit.descriptor.includes.Add(CoreIncludes.kHDShadow, IncludeLocation.Pregraph, new FieldCondition(EnableShadowMatte, true));
                forwardUnlit.descriptor.includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph, new FieldCondition(EnableShadowMatte, true));
                forwardUnlit.descriptor.includes.Add(CoreIncludes.kPunctualLightCommon, IncludeLocation.Pregraph, new FieldCondition(EnableShadowMatte, true));
                forwardUnlit.descriptor.includes.Add(CoreIncludes.kHDShadowLoop, IncludeLocation.Pregraph, new FieldCondition(EnableShadowMatte, true));

                return descriptor;
            }
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            base.CollectPassKeywords(ref pass);

            if (pass.IsForward())
                pass.keywords.Add(CoreKeywordDescriptors.Shadow, new FieldCondition(HDUnlitSubTarget.EnableShadowMatte, true));
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
            HDSubShaderUtilities.AddStencilShaderProperties(collector, systemData, null, false);
        }
    }
}
