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
        public static readonly string k_StencilWaterReadMaskGBuffer = "_StencilWaterReadMaskGBuffer";
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
            public const string kPassWaterGBuffer = "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/ShaderPassWaterGBuffer.hlsl";
            public const string kPassWaterMask = "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/ShaderPassWaterMask.hlsl";
        }

        [GenerateBlocks]
        public struct WaterBlocks
        {
            // Water specific block descriptors
            public static BlockFieldDescriptor LowFrequencyNormalWS = new BlockFieldDescriptor(kMaterial, "LowFrequencyNormalWS", "Low Frequency Normal (World Space)", "SURFACEDESCRIPTION_LOWFREQUENCYNORMALWS", new Vector3Control(Vector3.zero), ShaderStage.Fragment);
            public static BlockFieldDescriptor Foam = new BlockFieldDescriptor(kMaterial, "Foam", "Foam", "SURFACEDESCRIPTION_FOAM", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor TipThickness = new BlockFieldDescriptor(kMaterial, "TipThickness", "Tip Thickness", "SURFACEDESCRIPTIONTIP_THICKNESS", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Caustics = new BlockFieldDescriptor(kMaterial, "Caustics", "Caustics", "SURFACEDESCRIPTION_CAUSTICS", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor RefractedPositionWS = new BlockFieldDescriptor(kMaterial, "RefractedPositionWS", "Refracted Position", "SURFACEDESCRIPTIONTIP_REFRACTED_POSITION_WS", new Vector3Control(Vector3.zero), ShaderStage.Fragment);
        }

        #region Structs

        public static StructDescriptor AttributesMesh = new StructDescriptor()
        {
            name = "AttributesMesh",
            packFields = false,
            fields = new FieldDescriptor[]
            {
                HDStructFields.AttributesMesh.positionOS,
                HDStructFields.AttributesMesh.normalOS,
                HDStructFields.AttributesMesh.uv0,
                HDStructFields.AttributesMesh.color,
                HDStructFields.AttributesMesh.instanceID,
            }
        };

        public static StructDescriptor VaryingsMeshToDS = new StructDescriptor()
        {
            name = "VaryingsMeshToDS",
            packFields = true,
            populateWithCustomInterpolators = true,
            fields = new FieldDescriptor[]
            {
                HDStructFields.VaryingsMeshToDS.positionRWS,
                HDStructFields.VaryingsMeshToDS.tessellationFactor,
                HDStructFields.VaryingsMeshToDS.normalWS,
                HDStructFields.VaryingsMeshToDS.texCoord0,
                HDStructFields.VaryingsMeshToDS.texCoord1,
                HDStructFields.VaryingsMeshToDS.color,
                HDStructFields.VaryingsMeshToDS.instanceID,
            }
        };

        public static StructDescriptor VaryingsMeshToPS = new StructDescriptor()
        {
            name = "VaryingsMeshToPS",
            packFields = true,
            populateWithCustomInterpolators = true,
            fields = new FieldDescriptor[]
            {
                HDStructFields.VaryingsMeshToPS.positionCS,
                HDStructFields.VaryingsMeshToPS.normalWS,
                HDStructFields.VaryingsMeshToPS.texCoord0,
                HDStructFields.VaryingsMeshToPS.texCoord1,
                HDStructFields.VaryingsMeshToPS.instanceID,
            }
        };

        public static StructCollection GenerateStructs(StructCollection input, bool useVFX, bool useTessellation)
        {
            if (useTessellation)
            {
                return new StructCollection
                {
                    { AttributesMesh },
                    { VaryingsMeshToDS },
                    { VaryingsMeshToPS },
                    { Structs.VertexDescriptionInputs },
                    { Structs.SurfaceDescriptionInputs },
                };
            }
            else
            {
                return new StructCollection
                {
                    { AttributesMesh },
                    { VaryingsMeshToPS },
                    { Structs.VertexDescriptionInputs },
                    { Structs.SurfaceDescriptionInputs },
                };
            }
        }
        #endregion

        #region Pragmas
        public static PragmaCollection WaterPragmas = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target50) },
            { Pragma.Vertex("Vert") },
            { Pragma.Fragment("Frag") },
            { Pragma.OnlyRenderers(PragmaRenderers.GetHighEndPlatformArray()) },
            { new PragmaDescriptor { value = "instancing_options procedural:SetupInstanceID"}},
        };

        static PragmaCollection GeneratePragmas(bool useTessellation, bool useDebugSymbols)
        {
            PragmaCollection pragmas = new PragmaCollection { WaterPragmas };

            if (useTessellation)
            {
                pragmas.Add(Pragma.Hull("Hull"));
                pragmas.Add(Pragma.Domain("Domain"));
            }

            if (useDebugSymbols && Unsupported.IsDeveloperMode())
                pragmas.Add(Pragma.DebugSymbols);

            return pragmas;
        }
        #endregion

        #region Keywords
        public static KeywordDescriptor WaterSurfaceGBuffer = new KeywordDescriptor()
        {
            displayName = "WaterSurfaceGBuffer",
            referenceName = "WATER_SURFACE_GBUFFER",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor DecalSurfaceGradient = new KeywordDescriptor()
        {
            displayName = "DecalSurfaceGradient",
            referenceName = "DECAL_SURFACE_GRADIENT",
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

        public static KeywordDescriptor WaterSurfaceCurrent = new KeywordDescriptor()
        {
            displayName = "Water Local Current",
            referenceName = "WATER_LOCAL_CURRENT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Default,
        };

        public static KeywordDescriptor WaterDecalWorkflow = new KeywordDescriptor()
        {
            displayName = "Water Decal Workflow",
            referenceName = "WATER_DECAL",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "PARTIAL", referenceName = "PARTIAL" },
                new KeywordEntry() { displayName = "COMPLETE", referenceName = "COMPLETE" },
            },
            stages = KeywordShaderStage.Default,
        };

        public static KeywordDescriptor WaterDisplacement = new KeywordDescriptor()
        {
            displayName = "WaterDisplacement",
            referenceName = "WATER_DISPLACEMENT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Default,
        };
        #endregion

        #region Defines
        public static DefineCollection WaterGBufferDefines = new DefineCollection
        {
            { CoreKeywordDescriptors.SupportBlendModePreserveSpecularLighting, 1 },
            { HasRefraction, 1 },
            // Required for things such as decals
            { WaterSurfaceGBuffer, 1},
            { DecalSurfaceGradient, 1},
            { UseClusturedLightList, 1},
            { CoreKeywordDescriptors.PunctualShadow, 0 },
            { CoreKeywordDescriptors.DirectionalShadow, 0 },
            { CoreKeywordDescriptors.AreaShadow, 0 },
            { RayTracingQualityNode.GetRayTracingQualityKeyword(), 0 },
        };

        public static DefineCollection WaterDebugDefines = new DefineCollection
        {
            { CoreKeywordDescriptors.SupportBlendModePreserveSpecularLighting, 1 },
            { CoreKeywordDescriptors.HasLightloop, 1 },
            { CoreKeywordDescriptors.PunctualShadow, 0 },
            { CoreKeywordDescriptors.DirectionalShadow, 0 },
            { CoreKeywordDescriptors.AreaShadow, 0 },
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
                ReadMask = $"[{k_StencilWaterReadMaskGBuffer}]",
                Ref = $"[{k_StencilWaterRefGBuffer}]",
                Comp = "Equal",
                Pass = "Replace",
                Fail = "Keep",
            }) },
        };

        public static FieldCollection BasicWaterGBuffer = new FieldCollection()
        {
            HDStructFields.FragInputs.texCoord0,
            HDStructFields.FragInputs.texCoord1,
            HDStructFields.FragInputs.IsFrontFace,
        };

        public static DefineCollection GenerateDefines(DefineCollection input, bool useVFX, bool useTessellation, bool lowRes)
        {
            DefineCollection defines = HDShaderPasses.GenerateDefines(input, useVFX, useTessellation);

            if (!lowRes)
                defines.Add(WaterDisplacement, 1);

            return defines;
        }

        public static PassDescriptor GenerateWaterGBufferPass(bool lowRes, bool useTessellation, bool useDebugSymbols)
        {
            string passName = lowRes ? WaterSystem.k_LowResGBufferPass: WaterSystem.k_WaterGBufferPass;
            if (useTessellation)
                passName += WaterSystem.k_TessellationPass;

            return new PassDescriptor
            {
                // Definition
                displayName = passName,
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = passName,
                useInPreview = false,

                // Collections
                structs = GenerateStructs(null, false, useTessellation),
                requiredFields = BasicWaterGBuffer,
                renderStates = WaterGBuffer,
                pragmas = GeneratePragmas(useTessellation, useDebugSymbols),
                includes = GenerateIncludes(),
                defines = GenerateDefines(WaterGBufferDefines, false, useTessellation, lowRes),

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

        #region MaskWater
        public static RenderStateCollection WaterDebug = new RenderStateCollection
        {
            { RenderState.Cull($"[{k_CullWaterMask}]") },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.ZTest(ZTest.LEqual) },
        };

        public static PassDescriptor GenerateWaterDebugPass(bool lowRes, bool useTessellation, bool useDebugSymbols)
        {
            string passName = WaterSystem.k_WaterDebugPass;
            if (lowRes)
                passName += WaterSystem.k_LowResGBufferPass;
            if (useTessellation)
                passName += WaterSystem.k_TessellationPass;

            return new PassDescriptor
            {
                // Definition
                displayName = passName,
                referenceName = "SHADERPASS_WATER_MASK",
                lightMode = passName,
                useInPreview = false,

                // Collections
                structs = GenerateStructs(null, false, useTessellation),
                requiredFields = BasicWaterGBuffer,
                renderStates = WaterDebug,
                pragmas = GeneratePragmas(useTessellation, useDebugSymbols),
                defines = GenerateDefines(WaterDebugDefines, false, useTessellation, lowRes),
                includes = GenerateIncludes(),
                fieldDependencies = CoreFieldDependencies.Default,

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
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kLightLoop, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(WaterIncludes.kPassWaterMask, IncludeLocation.Postgraph);

                return includes;
            }
        }
        #endregion

        #region Descriptors
        public struct VertexDescriptionInputs
        {
            public static string name = "VertexDescriptionInputs";
            public static FieldDescriptor LowFrequencyHeight = new FieldDescriptor(name, "LowFrequencyHeight", "", ShaderValueType.Float, subscriptOptions: StructFieldOptions.Static);
            public static FieldDescriptor Displacement = new FieldDescriptor(name, "Displacement", "", ShaderValueType.Float3, subscriptOptions: StructFieldOptions.Static);
        }

        [GenerateBlocks]
        public struct VertexDescription
        {
            public static string name = "VertexDescription";
            public static BlockFieldDescriptor LowFrequencyHeight = new BlockFieldDescriptor(name, "LowFrequencyHeight", "", new FloatControl(0.0f), ShaderStage.Vertex);
            public static BlockFieldDescriptor Displacement = new BlockFieldDescriptor(name, "Displacement", "", new Vector3Control(Vector3.zero), ShaderStage.Vertex);
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
                    // Generate the water GBuffer passes
                    // We generate one with tessellation and one without to allow control from the water surface
                    GenerateWaterGBufferPass(false, false, systemData.debugSymbols),
                    GenerateWaterGBufferPass(false, true, systemData.debugSymbols),
                    // Low res gbuffer
                    GenerateWaterGBufferPass(true, false, systemData.debugSymbols),
                    // Debug pass, never use tessellation to reduce variants
                    GenerateWaterDebugPass(false, false, systemData.debugSymbols),
                    GenerateWaterDebugPass(true, false, systemData.debugSymbols),
                };
                return passes;
            }
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            bool legacyGraph = context.connectedBlocks.Contains(HDBlockFields.VertexDescription.UV0) ||
                context.connectedBlocks.Contains(HDBlockFields.VertexDescription.UV1);

            // Water specific properties
            context.AddField(StructFields.VertexDescriptionInputs.WorldSpacePosition);
            context.AddField(StructFields.VertexDescriptionInputs.WorldSpaceNormal);
            context.AddField(StructFields.SurfaceDescriptionInputs.FaceSign);

            context.AddField(StructFields.VertexDescriptionInputs.uv0, legacyGraph);
            context.AddField(StructFields.VertexDescriptionInputs.uv1, legacyGraph);

            context.AddField(VertexDescriptionInputs.Displacement, !legacyGraph);
            context.AddField(VertexDescriptionInputs.LowFrequencyHeight, !legacyGraph);

            if (context.pass.displayName.EndsWith("Tessellation"))
                context.AddField(HDFields.GraphTessellation);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex shader
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(HDBlockFields.VertexDescription.UV0);
            context.AddBlock(HDBlockFields.VertexDescription.UV1);
            context.AddBlock(VertexDescription.LowFrequencyHeight);
            context.AddBlock(VertexDescription.Displacement);

            // Fragment shader
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS);
            context.AddBlock(WaterBlocks.LowFrequencyNormalWS);
            context.AddBlock(WaterBlocks.RefractedPositionWS);
            context.AddBlock(WaterBlocks.TipThickness);
            context.AddBlock(WaterBlocks.Caustics);
            context.AddBlock(WaterBlocks.Foam);
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            if (pass.displayName.StartsWith(WaterSystem.k_LowResGBufferPass))
                return;

            pass.keywords.Add(WaterBandCount);
            pass.keywords.Add(WaterSurfaceCurrent);
            pass.keywords.Add(WaterDecalWorkflow);
            pass.keywords.Add(CoreKeywordDescriptors.ProceduralInstancing);
            pass.keywords.Add(CoreKeywordDescriptors.StereoInstancing);

            // The following keywords/multicompiles are only required for the gbuffer pass
            if (pass.displayName.StartsWith(WaterSystem.k_WaterGBufferPass))
            {
                if (lightingData.receiveDecals)
                    pass.keywords.Add(CoreKeywordDescriptors.Decals);
            }
            else if (pass.displayName.StartsWith(WaterSystem.k_WaterDebugPass))
            {
                pass.keywords.Add(CoreKeywordDescriptors.DebugDisplay);
            }
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new WaterSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, waterData));
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

            Vector1ShaderProperty stencilReadMaskWaterVar = new Vector1ShaderProperty();
            stencilReadMaskWaterVar.overrideReferenceName = k_StencilWaterReadMaskGBuffer;
            stencilReadMaskWaterVar.displayName = "Stencil Water Read Mask GBuffer";
            stencilReadMaskWaterVar.hidden = true;
            stencilReadMaskWaterVar.floatType = FloatType.Default;
            stencilReadMaskWaterVar.value = (int)StencilUsage.WaterSurface;
            stencilReadMaskWaterVar.overrideHLSLDeclaration = true;
            stencilReadMaskWaterVar.hlslDeclarationOverride = HLSLDeclaration.Global;
            stencilReadMaskWaterVar.generatePropertyBlock = false;
            collector.AddShaderProperty(stencilReadMaskWaterVar);

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
