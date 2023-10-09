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
        public static readonly string k_CullWaterMask = "_CullWaterMask";
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
            public const string kPassWaterGBuffer = "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/ShaderPassWaterGBuffer.hlsl";
        }

        [GenerateBlocks]
        public struct WaterBlocks
        {
            // Water specific block descriptors
            public static BlockFieldDescriptor LowFrequencyNormalWS = new BlockFieldDescriptor(kMaterial, "LowFrequencyNormalWS", "Low Frequency Normal (World Space)", "SURFACEDESCRIPTION_LOWFREQUENCYNORMALWS", new Vector3Control(Vector3.zero), ShaderStage.Fragment);
            public static BlockFieldDescriptor Foam = new BlockFieldDescriptor(kMaterial, "Foam", "Foam", "SURFACEDESCRIPTION_FOAM", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor TipThickness = new BlockFieldDescriptor(kMaterial, "TipThickness", "Tip Thickness", "SURFACEDESCRIPTIONTIP_THICKNESS", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Caustics = new BlockFieldDescriptor(kMaterial, "Caustics", "Caustics", "SURFACEDESCRIPTION_CAUSTICS", new FloatControl(0.0f), ShaderStage.Fragment);
        }

        public static PragmaCollection WaterTessellationInstanced = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target50) },
            { Pragma.Vertex("Vert") },
            { Pragma.Fragment("Frag") },
            { Pragma.Hull("Hull") },
            { Pragma.Domain("Domain") },
            { Pragma.OnlyRenderers(PragmaRenderers.GetHighEndPlatformArray()) },
            { new PragmaDescriptor { value = "instancing_options procedural:SetupInstanceID"}},
        };

        #region Keywords
        public static KeywordDescriptor WaterSurfaceGBuffer = new KeywordDescriptor()
        {
            displayName = "WaterSurfaceGBuffer",
            referenceName = "WATER_SURFACE_GBUFFER",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor WaterBandCount = new KeywordDescriptor()
        {
            displayName = "WaterBand",
            referenceName = "WATER",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
    {
                new KeywordEntry() { displayName = "ONE_BAND", referenceName = "ONE_BAND" },
                new KeywordEntry() { displayName = "TWO_BANDS", referenceName = "TWO_BANDS" },
                new KeywordEntry() { displayName = "THREE_BANDS", referenceName = "THREE_BANDS" },
    },
            stages = KeywordShaderStage.Default,
        };

        public static KeywordDescriptor HasRefraction = new KeywordDescriptor()
        {
            displayName = "HasRefraction",
            referenceName = "HAS_REFRACTION",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor UseClusturedLightList = new KeywordDescriptor()
        {
            displayName = "UseClusturedLightList",
            referenceName = "USE_CLUSTERED_LIGHTLIST",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
        };
        #endregion

        #region Defines
        public static DefineCollection WaterGBufferDefines = new DefineCollection
        {
            { CoreKeywordDescriptors.SupportBlendModePreserveSpecularLighting, 1 },
            { HasRefraction, 1 },
            // Required for things such as decals
            { WaterSurfaceGBuffer, 1},
            { UseClusturedLightList, 1},
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
        };
        #endregion

        #region GBufferWater
        public static RenderStateCollection WaterGBuffer = new RenderStateCollection
        {
            { RenderState.Cull($"[{k_CullWaterMask}]") },
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

        public static FieldCollection BasicWaterGBuffer = new FieldCollection()
        {
            HDStructFields.FragInputs.positionRWS,
            HDStructFields.FragInputs.tangentToWorld,
            HDStructFields.FragInputs.texCoord0,
            HDStructFields.FragInputs.texCoord1,
            HDStructFields.FragInputs.IsFrontFace,
        };

        public static PassDescriptor GenerateWaterGBufferPassTesselation()
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "GBufferTesselation",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBufferTesselation",
                useInPreview = true,

                // Collections
                structs = CoreStructCollections.BasicTessellation,
                requiredFields = BasicWaterGBuffer,
                renderStates = WaterGBuffer,
                pragmas = WaterTessellationInstanced,
                defines = HDShaderPasses.GenerateDefines(WaterGBufferDefines, false, true),
                includes = GenerateIncludes(),

                virtualTextureFeedback = false,
                customInterpolators = CoreCustomInterpolators.Common
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
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
                    // Generate the water GBuffer pass
                    GenerateWaterGBufferPassTesselation(),
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
            context.AddField(StructFields.VertexDescriptionInputs.WorldSpacePosition);
            context.AddField(HDFields.GraphTessellation);
            context.AddField(HDFields.TessellationFactor);
            context.AddField(StructFields.SurfaceDescriptionInputs.FaceSign);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex shader
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(HDBlockFields.VertexDescription.UV0);
            context.AddBlock(HDBlockFields.VertexDescription.UV1);

            // Fragment shader
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS);
            context.AddBlock(WaterBlocks.LowFrequencyNormalWS);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(WaterBlocks.Foam);
            context.AddBlock(WaterBlocks.TipThickness);
            context.AddBlock(WaterBlocks.Caustics);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            base.CollectPassKeywords(ref pass);
            pass.keywords.Add(WaterBandCount);
            pass.keywords.Add(CoreKeywordDescriptors.Decals);
            pass.keywords.Add(CoreKeywordDescriptors.Shadow);
            pass.keywords.Add(CoreKeywordDescriptors.AreaShadow);
            pass.keywords.Add(CoreKeywordDescriptors.DebugDisplay);
            pass.keywords.Add(CoreKeywordDescriptors.ProceduralInstancing);
            pass.keywords.Add(CoreKeywordDescriptors.StereoInstancing);
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
            stencilRefWaterVar.value = (int)(StencilUsage.WaterSurface | StencilUsage.ExcludeFromTUAndAA);
            stencilRefWaterVar.overrideHLSLDeclaration = true;
            stencilRefWaterVar.hlslDeclarationOverride = HLSLDeclaration.Global;
            stencilRefWaterVar.generatePropertyBlock = false;
            collector.AddShaderProperty(stencilRefWaterVar);

            Vector1ShaderProperty stencilWriteMaskWaterVar = new Vector1ShaderProperty();
            stencilWriteMaskWaterVar.overrideReferenceName = k_StencilWaterWriteMaskGBuffer;
            stencilWriteMaskWaterVar.displayName = "Stencil Water Write Mask GBuffer";
            stencilWriteMaskWaterVar.hidden = true;
            stencilWriteMaskWaterVar.floatType = FloatType.Default;
            stencilWriteMaskWaterVar.value = (int)(StencilUsage.WaterSurface | StencilUsage.ExcludeFromTUAndAA);
            stencilWriteMaskWaterVar.overrideHLSLDeclaration = true;
            stencilWriteMaskWaterVar.hlslDeclarationOverride = HLSLDeclaration.Global;
            stencilWriteMaskWaterVar.generatePropertyBlock = false;
            collector.AddShaderProperty(stencilWriteMaskWaterVar);

            Vector1ShaderProperty cullingModeWaterVar = new Vector1ShaderProperty();
            cullingModeWaterVar.overrideReferenceName = k_CullWaterMask;
            cullingModeWaterVar.displayName = "Cull Water Mask";
            cullingModeWaterVar.hidden = true;
            cullingModeWaterVar.floatType = FloatType.Enum;
            cullingModeWaterVar.value = (int)Cull.Off;
            cullingModeWaterVar.overrideHLSLDeclaration = true;
            cullingModeWaterVar.hlslDeclarationOverride = HLSLDeclaration.Global;
            cullingModeWaterVar.generatePropertyBlock = false;
            collector.AddShaderProperty(cullingModeWaterVar);

            // EmissionColor is a required shader property even if it is not used in this case
            collector.AddShaderProperty(new ColorShaderProperty()
            {
                overrideReferenceName = "_EmissionColor",
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.UnityPerMaterial,
                value = new Color(1.0f, 1.0f, 1.0f, 1.0f)
            });
        }
    }
}
