using System;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

using static UnityEngine.Rendering.HighDefinition.HDMaterial;
using static UnityEditor.Rendering.HighDefinition.HDFields;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class WaterSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<WaterData>
    {
        public WaterSubTarget() => displayName = "Water";

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("7dd29427652f2a348be0e480ab69597c");  // WaterSubTarget.cs

        static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Water/ShaderGraph/"
        };
        public static readonly string k_StencilWaterWriteMaskGBuffer = "_StencilWaterWriteMaskGBuffer";
        public static readonly string k_StencilWaterRefGBuffer = "_StencilWaterRefGBuffer";

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override ShaderID shaderID => ShaderID.SG_Water;
        protected override string subShaderInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Water/Water.hlsl";
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Water SubShader", "");
        protected override bool requireSplitLighting => false;
        protected override bool supportRaytracing => false;

        // As this is a procedural geometry, we need to provide a custom Vertex.template.hlsl and the way to do it now is to
        // provide with a higher priority an include that holds the Vertex.template.hlsl we want.
        protected override string[] sharedTemplatePath => new string[]{
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/",
            $"{HDUtils.GetVFXPath()}/Editor/ShaderGraph/Templates",
            $"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Water/",
        };

        WaterData m_WaterData;

        WaterData IRequiresData<WaterData>.data
        {
            get => m_WaterData;
            set => m_WaterData = value;
        }

        public WaterData waterData
        {
            get => m_WaterData;
            set => m_WaterData = value;
        }

        public class WaterIncludes
        {
            public const string kPassWaterForward = "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/ShaderPassWaterForward.hlsl";
            public const string kPassWaterGBuffer = "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/ShaderPassWaterGBuffer.hlsl";
        }

        public static FieldDescriptor Water = new FieldDescriptor(kMaterial, "Water", "_MATERIAL_FEATURE_WATER 1");

        [GenerateBlocks]
        public struct WaterBlocks
        {
            // Water specific block descriptors
            public static BlockFieldDescriptor LowFrequencyNormalWS = new BlockFieldDescriptor(kMaterial, "LowFrequencyNormalWS", "Low Frequency Normal (World Space)", "SURFACEDESCRIPTION_LOWFREQUENCYNORMALWS", new Vector3Control(Vector3.zero), ShaderStage.Fragment);
            public static BlockFieldDescriptor Foam = new BlockFieldDescriptor(kMaterial, "Foam", "Foam", "SURFACEDESCRIPTION_FOAM", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor TipThickness = new BlockFieldDescriptor(kMaterial, "TipThickness", "Tip Thickness", "SURFACEDESCRIPTIONTIP_THICKNESS", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor RefractionColor = new BlockFieldDescriptor(kMaterial, "RefractionColor", "Refraction Color", "SURFACEDESCRIPTION_REFRACTIONCOLOR", new ColorControl(new Color(0.0f, 0.0f, 0.0f), false), ShaderStage.Fragment);
        }

        #region Keywords
        public static KeywordDescriptor HighResolutionWater = new KeywordDescriptor()
        {
            displayName = "HighResolutionWater",
            referenceName = "HIGH_RESOLUTION_WATER",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Default
        };

        public static KeywordDescriptor WaterProceduralGeometry = new KeywordDescriptor()
        {
            displayName = "WaterProceduralGeometry",
            referenceName = "WATER_PROCEDURAL_GEOMETRY",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor HasRefraction = new KeywordDescriptor()
        {
            displayName = "HasRefraction",
            referenceName = "HAS_REFRACTION",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
        };
        #endregion

        #region Defines
        public static DefineCollection WaterForwardDefinesProcedural = new DefineCollection
        {
            { CoreKeywordDescriptors.SupportBlendModePreserveSpecularLighting, 1 },
            { CoreKeywordDescriptors.HasLightloop, 1 },
            { WaterProceduralGeometry, 1 },
            { HasRefraction, 1 },
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
        };

        public static DefineCollection WaterForwardDefinesMesh = new DefineCollection
        {
            { CoreKeywordDescriptors.SupportBlendModePreserveSpecularLighting, 1 },
            { CoreKeywordDescriptors.HasLightloop, 1 },
            { HasRefraction, 1 },
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
        };
        #endregion

        #region ForwardWater
        public static RenderStateCollection WaterForward = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Back) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ColorMask("ColorMask 0 1") },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = CoreRenderStates.Uniforms.stencilWriteMask,
                Ref = CoreRenderStates.Uniforms.stencilRef,
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static PassDescriptor GenerateWaterForwardPassProcedural()
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "ForwardOnlyProcedural",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnlyProcedural",
                useInPreview = true,

                // Collections
                structs = CoreStructCollections.BasicProcedural,
                // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = WaterForward,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, false, false),
                defines = HDShaderPasses.GenerateDefines(WaterForwardDefinesProcedural, false, false),
                includes = GenerateIncludes(),

                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLightLoop, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(WaterIncludes.kPassWaterForward, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static PassDescriptor GenerateWaterForwardPassMesh()
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "ForwardOnlyMesh",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnlyMesh",
                useInPreview = true,

                // Collections
                structs = CoreStructCollections.Basic,
                // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = WaterForward,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, false, false),
                defines = HDShaderPasses.GenerateDefines(WaterForwardDefinesMesh, false, false),
                includes = GenerateIncludes(),

                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLightLoop, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(WaterIncludes.kPassWaterForward, IncludeLocation.Postgraph);

                return includes;
            }
        }
        #endregion

        #region GBufferWater
        public static RenderStateCollection WaterGBuffer = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Back) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.Stencil(new StencilDescriptor()
            {
                WriteMask = $"[{k_StencilWaterWriteMaskGBuffer}]",
                Ref = $"[{k_StencilWaterRefGBuffer}]",
                Comp = "Always",
                Pass = "Replace",
            }) },
        };

        public static PassDescriptor GenerateWaterGBufferPassProcedural()
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "GBufferProcedural",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBufferProcedural",
                useInPreview = true,

                // Collections
                structs = CoreStructCollections.BasicProcedural,
                // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = WaterGBuffer,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, false, false),
                defines = HDShaderPasses.GenerateDefines(WaterForwardDefinesProcedural, false, false),
                includes = GenerateIncludes(),

                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLightLoop, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(WaterIncludes.kPassWaterGBuffer, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static PassDescriptor GenerateWaterGBufferPassMesh()
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "GBufferMesh",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBufferMesh",
                useInPreview = true,

                // Collections
                structs = CoreStructCollections.Basic,
                // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = WaterGBuffer,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, false, false),
                defines = HDShaderPasses.GenerateDefines(WaterForwardDefinesMesh, false, false),
                includes = GenerateIncludes(),

                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLightLoop, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(WaterIncludes.kPassWaterGBuffer, IncludeLocation.Postgraph);

                return includes;
            }
        }
        #endregion

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            return new SubShaderDescriptor
            {
                generatesPreview = false,
                passes = GetWaterPasses()
            };

            PassCollection GetWaterPasses()
            {
                var passes = new PassCollection
                {
                    // Generate the water forward pass
                    GenerateWaterForwardPassProcedural(),
                    GenerateWaterForwardPassMesh(),
                    GenerateWaterGBufferPassProcedural(),
                    GenerateWaterGBufferPassMesh(),
                };
                return passes;
            }
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Water specific properties
            context.AddField(StructFields.VertexDescriptionInputs.uv0);
            context.AddField(StructFields.VertexDescriptionInputs.uv1);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex shader
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.UV0);
            context.AddBlock(BlockFields.VertexDescription.UV1);

            // Fragment shader
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS);
            context.AddBlock(WaterBlocks.LowFrequencyNormalWS);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(WaterBlocks.Foam);
            context.AddBlock(WaterBlocks.TipThickness);
            context.AddBlock(WaterBlocks.RefractionColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            base.CollectPassKeywords(ref pass);
            pass.keywords.Add(HighResolutionWater);
            pass.keywords.Add(CoreKeywordDescriptors.Shadow);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new WaterSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, waterData));
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            Vector1ShaderProperty stencilRefWaterVar = new Vector1ShaderProperty();
            stencilRefWaterVar.overrideReferenceName = k_StencilWaterRefGBuffer;
            stencilRefWaterVar.displayName = "Stencil Water Ref GBuffer";
            stencilRefWaterVar.hidden = true;
            stencilRefWaterVar.floatType = FloatType.Default;
            stencilRefWaterVar.value = (int)StencilUsage.WaterSurface;
            stencilRefWaterVar.overrideHLSLDeclaration = true;
            stencilRefWaterVar.hlslDeclarationOverride = HLSLDeclaration.Global;
            stencilRefWaterVar.generatePropertyBlock = false;
            collector.AddShaderProperty(stencilRefWaterVar);

            Vector1ShaderProperty stencilWriteMaskWaterVar = new Vector1ShaderProperty();
            stencilWriteMaskWaterVar.overrideReferenceName = k_StencilWaterWriteMaskGBuffer;
            stencilWriteMaskWaterVar.displayName = "Stencil Water Write Mask GBuffer";
            stencilWriteMaskWaterVar.hidden = true;
            stencilWriteMaskWaterVar.floatType = FloatType.Default;
            stencilWriteMaskWaterVar.value = (int)StencilUsage.WaterSurface;
            stencilWriteMaskWaterVar.overrideHLSLDeclaration = true;
            stencilWriteMaskWaterVar.hlslDeclarationOverride = HLSLDeclaration.Global;
            stencilWriteMaskWaterVar.generatePropertyBlock = false;
            collector.AddShaderProperty(stencilWriteMaskWaterVar);
        }
    }
}
