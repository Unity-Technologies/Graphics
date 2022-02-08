using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

using static UnityEngine.Rendering.HighDefinition.HDMaterial;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class TerrainLitSubTarget : SurfaceSubTarget, IRequiresData<TerrainLitData>
    {
        public TerrainLitSubTarget() => displayName = "TerrainLit";

        private static readonly GUID kSubTargetSourceCodeGuid = new GUID("7771b949c95f4ed9ac018e9db21849e5");
        private static string[] passTemplateMaterialDirectories => new [] {$"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/TerrainLit/ShaderGraph/"};

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/TerrainLit/ShaderGraph/ShaderPass.template";
        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override ShaderID shaderID => ShaderID.SG_Terrain;
        protected override string customInspector => "Rendering.HighDefinition.TerrainLitGUI";
        protected override string renderType => HDRenderTypeTags.Opaque.ToString();
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        internal override MaterialResetter setupMaterialKeywordsAndPassFunc => ShaderGraphAPI.ValidateTerrain;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "TerrainLit SubShader", "");
        protected override string subShaderInclude => CoreIncludes.kTerrainLit;
        protected override string postDecalsInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl";
        protected override string raytracingInclude => CoreIncludes.kTerrainRaytracing;

        protected override bool supportForward => true;
        protected override bool supportLighting => true;
        protected override bool supportDistortion => false;
        protected override bool supportRaytracing => false; // TODO : support later
        protected override bool supportPathtracing => false; // TODO : support later

        static readonly GUID kSourceCodeGuid = new GUID("be136c27a7154cd99820c558d8feedb2"); // TerrainLitSubTarget.cs

        private LightingData m_LightingData;
        private TerrainLitData m_TerrainLitData;

        TerrainLitData IRequiresData<TerrainLitData>.data
        {
            get => m_TerrainLitData;
            set => m_TerrainLitData = value;
        }

        public LightingData lightingData
        {
            get => m_LightingData;
            set => m_LightingData = value;
        }

        public TerrainLitData terrainLitData
        {
            get => m_TerrainLitData;
            set => m_TerrainLitData = value;
        }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);
        }

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            return new SubShaderDescriptor
            {
                generatesPreview = true,
                customTags = new List<string>() { "\"TerrainCompatible\" = \"True\"" },
                passes = GetPasses(),
            };

            PassCollection GetPasses()
            {
                // terrain won't be supported for VFX
                bool allowsVFX = false;

                var passes = new PassCollection()
                {
                    GenerateShadowCaster(supportLighting, allowsVFX, systemData.tessellation),
                    GenerateMETA(supportLighting, allowsVFX),
                    GenerateScenePicking(allowsVFX, systemData.tessellation),
                    GenerateSceneSelection(supportLighting, allowsVFX, systemData.tessellation),
                };

                if (supportForward)
                {
                    passes.Add(GenerateDepthForwardOnlyPass(supportLighting, allowsVFX, systemData.tessellation));
                    passes.Add(GenerateForwardOnlyPass(supportLighting, allowsVFX, systemData.tessellation));
                }

                passes.Add(GenerateLitDepthOnly(TargetsVFX(), systemData.tessellation));
                passes.Add(GenerateGBuffer(TargetsVFX(), systemData.tessellation));
                passes.Add(GenerateLitForward(TargetsVFX(), systemData.tessellation));
                //if (!systemData.tessellation) // Raytracing don't support tessellation neither VFX
                //    passes.Add(HDShaderPasses.GenerateLitRaytracingPrepass());

                return passes;
            }
        }

        protected override SubShaderDescriptor GetRaytracingSubShaderDescriptor()
        {
            var descriptor = base.GetRaytracingSubShaderDescriptor();

            // TODO :

            return descriptor;
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            pass.defines.Add(TerrainKeywordDescriptors.TerrainEnabled, 1);
            pass.keywords.Add(TerrainKeywordDescriptors.Terrain8Layers);
#if false
            pass.keywords.Add(CoreKeywordDescriptors.AlphaTest, new FieldCondition(Fields.AlphaTest, true));

            if (pass.IsDepthOrMV())
                pass.keywords.Add(CoreKeywordDescriptors.WriteMsaaDepth);

            pass.keywords.Add(CoreKeywordDescriptors.SurfaceTypeTransparent);
            pass.keywords.Add(CoreKeywordDescriptors.BlendMode);
            pass.keywords.Add(CoreKeywordDescriptors.DoubleSided, new FieldCondition(HDFields.Unlit, false));
            pass.keywords.Add(CoreKeywordDescriptors.DepthOffset, new FieldCondition(HDFields.DepthOffset, true));
            pass.keywords.Add(CoreKeywordDescriptors.ConservativeDepthOffset, new FieldCondition(HDFields.ConservativeDepthOffset, true));

            pass.keywords.Add(CoreKeywordDescriptors.AddPrecomputedVelocity);
            pass.keywords.Add(CoreKeywordDescriptors.TransparentWritesMotionVector);
            pass.keywords.Add(CoreKeywordDescriptors.FogOnTransparent);

            if (pass.NeedsDebugDisplay())
                pass.keywords.Add(CoreKeywordDescriptors.DebugDisplay);

            if (!pass.IsRelatedToRaytracing())
                pass.keywords.Add(CoreKeywordDescriptors.LodFadeCrossfade, new FieldCondition(Fields.LodCrossFade, true));

            if (pass.lightMode == HDShaderPassNames.s_MotionVectorsStr)
            {
                if (supportForward)
                    pass.defines.Add(CoreKeywordDescriptors.WriteNormalBuffer, 1, new FieldCondition(HDFields.Unlit, false));
                else
                    pass.keywords.Add(CoreKeywordDescriptors.WriteNormalBuffer, new FieldCondition(HDFields.Unlit, false));
            }

            if (pass.IsTessellation())
            {
                pass.keywords.Add(CoreKeywordDescriptors.TessellationMode);
            }

            pass.keywords.Add(CoreKeywordDescriptors.DisableDecals);
            pass.keywords.Add(CoreKeywordDescriptors.DisableSSR);
            pass.keywords.Add(CoreKeywordDescriptors.DisableSSRTransparent);
            // pass.keywords.Add(CoreKeywordDescriptors.EnableGeometricSpecularAA);

            if (pass.IsDepthOrMV())
            {
                pass.keywords.Add(CoreKeywordDescriptors.WriteDecalBuffer);
            }

            if (pass.IsLightingOrMaterial())
            {
                pass.keywords.Add(CoreKeywordDescriptors.Lightmap);
                pass.keywords.Add(CoreKeywordDescriptors.DirectionalLightmapCombined);
                pass.keywords.Add(CoreKeywordDescriptors.ProbeVolumes);
                pass.keywords.Add(CoreKeywordDescriptors.DynamicLightmap);

                if (!pass.IsRelatedToRaytracing())
                {
                    pass.keywords.Add(CoreKeywordDescriptors.ShadowsShadowmask);
                    pass.keywords.Add(CoreKeywordDescriptors.Decals);
                    pass.keywords.Add(CoreKeywordDescriptors.DecalSurfaceGradient);
                }
            }

            if (pass.IsForward())
            {
                pass.keywords.Add(CoreKeywordDescriptors.Shadow);
                pass.keywords.Add(CoreKeywordDescriptors.ScreenSpaceShadow);

                if (pass.lightMode == HDShaderPassNames.s_TransparentBackfaceStr)
                    pass.defines.Add(CoreKeywordDescriptors.LightList, 1);
                else
                    pass.keywords.Add(CoreKeywordDescriptors.LightList);
            }
#else
            pass.keywords.Add(CoreKeywordDescriptors.DisableDecals);
            pass.keywords.Add(CoreKeywordDescriptors.DisableSSR);

            if (pass.IsDepthOrMV())
            {
                pass.keywords.Add(CoreKeywordDescriptors.WriteDecalBuffer);
            }

            if (pass.IsLightingOrMaterial())
            {
                pass.keywords.Add(CoreKeywordDescriptors.Lightmap);
                pass.keywords.Add(CoreKeywordDescriptors.DirectionalLightmapCombined);
                pass.keywords.Add(CoreKeywordDescriptors.ProbeVolumes);
                pass.keywords.Add(CoreKeywordDescriptors.DynamicLightmap);

                if (!pass.IsRelatedToRaytracing())
                {
                    pass.keywords.Add(CoreKeywordDescriptors.ShadowsShadowmask);
                    pass.keywords.Add(CoreKeywordDescriptors.Decals);
                    pass.keywords.Add(CoreKeywordDescriptors.DecalSurfaceGradient);
                }
            }

            if (pass.IsForward())
            {
                pass.keywords.Add(CoreKeywordDescriptors.Shadow);
                pass.keywords.Add(CoreKeywordDescriptors.ScreenSpaceShadow);
                pass.keywords.Add(CoreKeywordDescriptors.LightList);
            }

            if (pass.NeedsDebugDisplay())
                pass.keywords.Add(CoreKeywordDescriptors.DebugDisplay);

            if (pass.lightMode == HDShaderPassNames.s_MotionVectorsStr)
            {
                if (supportForward)
                    pass.defines.Add(CoreKeywordDescriptors.WriteNormalBuffer, 1, new FieldCondition(HDFields.Unlit, false));
                else
                    pass.keywords.Add(CoreKeywordDescriptors.WriteNormalBuffer, new FieldCondition(HDFields.Unlit, false));
            }
#endif
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Common properties to all Lit master nodes
            var descs = context.blocks.Select(x => x.descriptor);

            // Lit specific properties
            context.AddField(DotsProperties, context.hasDotsProperties);

            // Misc
            context.AddField(LightingGI, descs.Contains(HDBlockFields.SurfaceDescription.BakedGI) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedGI));
            context.AddField(BackLightingGI, descs.Contains(HDBlockFields.SurfaceDescription.BakedBackGI) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI));
            context.AddField(HDFields.AmbientOcclusion, context.blocks.Contains((BlockFields.SurfaceDescription.Occlusion, false)) && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.Occlusion));
            //context.AddField(RayTracing, terrainLitData.rayTracing); // TODO : raytracing later

            // Specular Occlusion Fields
            context.AddField(SpecularOcclusionFromAO, terrainLitData.specularOcclusionMode == SpecularOcclusionMode.FromAO);
            context.AddField(SpecularOcclusionFromAOBentNormal, terrainLitData.specularOcclusionMode == SpecularOcclusionMode.FromAOAndBentNormal);
            context.AddField(SpecularOcclusionCustom, terrainLitData.specularOcclusionMode == SpecularOcclusionMode.Custom);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            context.AddBlock(BlockFields.SurfaceDescription.Metallic);

            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);

            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularOcclusion, terrainLitData.specularOcclusionMode == SpecularOcclusionMode.Custom);

            context.AddBlock(BlockFields.SurfaceDescription.NormalOS, terrainLitData.normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS, terrainLitData.normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS, terrainLitData.normalDropOffSpace == NormalDropOffSpace.World);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            HDSubShaderUtilities.AddRayTracingProperty(collector, terrainLitData.rayTracing);
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            base.ProcessPreviewMaterial(material);

            material.SetFloat(kReceivesSSR, terrainLitData.receiveSSR ? 1 : 0);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var gui = new SubTargetPropertiesGUI(context, onChange, registerUndo, systemData, null, lightingData);
            AddInspectorPropertyBlocks(gui);
            context.Add(gui);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new TerrainLitSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, terrainLitData));
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            return base.ComputeMaterialNeedsUpdateHash() * 23 + terrainLitData.terrainSurfaceType.GetHashCode();
        }

        #region KeywordDescriptors
        static class TerrainKeywordDescriptors
        {
            public static KeywordDescriptor TerrainEnabled = new KeywordDescriptor()
            {
                displayName = "HD Terrain",
                referenceName = "HD_TERRAIN_ENABLED",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor Terrain8Layers = new KeywordDescriptor()
            {
                displayName = "HD Terrain 8 Layers",
                referenceName = "_TERRAIN_8_LAYERS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };
        }
        #endregion

        #region Includes
        static class TerrainIncludes
        {
            public const string kTerrainLitSurfaceData = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitSurfaceData.hlsl";
            public const string kSplatmap = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap.hlsl";
        }

        public static IncludeCollection DepthOnlyIncludes = new IncludeCollection
        {
            { CoreIncludes.CorePregraph },
            { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
            { CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.CoreUtility },
            { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
            { CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
            { TerrainIncludes.kTerrainLitSurfaceData, IncludeLocation.Pregraph },
            { TerrainIncludes.kSplatmap, IncludeLocation.Pregraph },
            { CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph },
        };

        public static IncludeCollection GBufferIncludes = new IncludeCollection
        {
            { CoreIncludes.CorePregraph },
            { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
            { CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.CoreUtility },
            { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
            { CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
            { TerrainIncludes.kTerrainLitSurfaceData, IncludeLocation.Pregraph },
            { TerrainIncludes.kSplatmap, IncludeLocation.Pregraph },
            { CoreIncludes.kPassGBuffer, IncludeLocation.Postgraph },
        };

        public static IncludeCollection ForwardIncludes = new IncludeCollection
        {
            { CoreIncludes.CorePregraph },
            { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
            { CoreIncludes.kLighting, IncludeLocation.Pregraph },
            { CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph },
            { CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.kLightLoop, IncludeLocation.Pregraph },
            { CoreIncludes.CoreUtility },
            { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
            { CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph },
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
            { TerrainIncludes.kTerrainLitSurfaceData, IncludeLocation.Pregraph },
            { TerrainIncludes.kSplatmap, IncludeLocation.Pregraph },
            { CoreIncludes.kPassForward, IncludeLocation.Postgraph },
        };
        #endregion

        #region Passes
        static public PassDescriptor GenerateShadowCaster(bool supportLighting, bool useVFX, bool useTessellation)
        {
            return new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,
                validPixelBlocks = new BlockFieldDescriptor[]
                {
                    BlockFields.SurfaceDescription.Alpha, BlockFields.SurfaceDescription.AlphaClipThreshold,
                    HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow,
                    HDBlockFields.SurfaceDescription.DepthOffset,
                    HDBlockFields.SurfaceDescription
                        .DiffusionProfileHash // not used, but keeps the UnityPerMaterial cbuffer identical
                },

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, useVFX, useTessellation),
                requiredFields = CoreRequiredFields.Basic,
                renderStates = CoreRenderStates.ShadowCaster,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, useVFX, useTessellation),
                defines = HDShaderPasses.GenerateDefines(null, useVFX, useTessellation),
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }

                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(TerrainIncludes.kTerrainLitSurfaceData, IncludeLocation.Pregraph);
                includes.Add(TerrainIncludes.kSplatmap, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static PassDescriptor GenerateMETA(bool supportLighting, bool useVFX)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // We don't need any vertex inputs on meta pass:
                validVertexBlocks = new BlockFieldDescriptor[0],

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, useVFX, false),
                requiredFields = CoreRequiredFields.Meta,
                renderStates = CoreRenderStates.Meta,
                // Note: no tessellation for meta pass
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, useVFX, false),
                defines = HDShaderPasses.GenerateDefines(CoreDefines.ShaderGraphRaytracingDefault, useVFX, false),
                keywords = new KeywordCollection() { CoreKeywordDescriptors.EditorVisualization },
                includes = GenerateIncludes(),
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(TerrainIncludes.kTerrainLitSurfaceData, IncludeLocation.Pregraph);
                includes.Add(TerrainIncludes.kSplatmap, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassLightTransport, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static PassDescriptor GenerateScenePicking(bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "ScenePickingPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "Picking",
                useInPreview = false,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, useVFX, useTessellation),
                requiredFields = GenerateRequiredFields(),
                renderStates = CoreRenderStates.ScenePicking,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstancedEditorSync, useVFX, useTessellation),
                defines = HDShaderPasses.GenerateDefines(CoreDefines.ScenePicking, useVFX, useTessellation),
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
            };

            FieldCollection GenerateRequiredFields()
            {
                var fieldCollection = new FieldCollection();

                fieldCollection.Add(CoreRequiredFields.Basic);
                fieldCollection.Add(CoreRequiredFields.AddWriteNormalBuffer);

                return fieldCollection;
            }

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.kPickingSpaceTransforms, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CorePregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(TerrainIncludes.kTerrainLitSurfaceData, IncludeLocation.Pregraph);
                includes.Add(TerrainIncludes.kSplatmap, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static PassDescriptor GenerateSceneSelection(bool supportLighting, bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, useVFX, useTessellation),
                requiredFields = CoreRequiredFields.Basic,
                renderStates = CoreRenderStates.SceneSelection,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstancedEditorSync, useVFX, useTessellation),
                defines = HDShaderPasses.GenerateDefines(CoreDefines.SceneSelection, useVFX, useTessellation),
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.kPickingSpaceTransforms, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(TerrainIncludes.kTerrainLitSurfaceData, IncludeLocation.Pregraph);
                includes.Add(TerrainIncludes.kSplatmap, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static PassDescriptor GenerateDepthForwardOnlyPass(bool supportLighting, bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, useVFX, useTessellation),
                requiredFields = GenerateRequiredFields(),
                renderStates = GenerateRenderState(),
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, useVFX, useTessellation),
                defines = HDShaderPasses.GenerateDefines(supportLighting ? CoreDefines.DepthForwardOnly : CoreDefines.DepthForwardOnlyUnlit, useVFX, useTessellation),
                includes = GenerateIncludes(),
                customInterpolators = CoreCustomInterpolators.Common,
            };

            FieldCollection GenerateRequiredFields()
            {
                var fieldCollection = new FieldCollection();

                fieldCollection.Add(supportLighting ? CoreRequiredFields.BasicLighting : CoreRequiredFields.Basic);
                fieldCollection.Add(CoreRequiredFields.AddWriteNormalBuffer);

                return fieldCollection;
            }

            RenderStateCollection GenerateRenderState()
            {
                var renderState = new RenderStateCollection { CoreRenderStates.DepthOnly };
                return renderState;
            }

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(TerrainIncludes.kTerrainLitSurfaceData, IncludeLocation.Pregraph);
                includes.Add(TerrainIncludes.kSplatmap, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static PassDescriptor GenerateForwardOnlyPass(bool supportLighting, bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = supportLighting ? "SHADERPASS_FORWARD" : "SHADERPASS_FORWARD_UNLIT",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, useVFX, useTessellation),
                // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                requiredFields = supportLighting ? CoreRequiredFields.BasicLighting : CoreRequiredFields.BasicMotionVector,
                renderStates = CoreRenderStates.Forward,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, useVFX, useTessellation),
                defines = HDShaderPasses.GenerateDefines(supportLighting ? CoreDefines.Forward : CoreDefines.ForwardUnlit, useVFX, useTessellation),
                includes = GenerateIncludes(),

                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common
            };

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kLightLoop, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                includes.Add(TerrainIncludes.kTerrainLitSurfaceData, IncludeLocation.Pregraph);
                includes.Add(TerrainIncludes.kSplatmap, IncludeLocation.Pregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kPassForward, IncludeLocation.Postgraph);
                else
                    includes.Add(CoreIncludes.kPassForwardUnlit, IncludeLocation.Postgraph);

                return includes;
            }
        }

        public static PassDescriptor GenerateLitDepthOnly(bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthOnly",
                useInPreview = true,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, useVFX, useTessellation),
                requiredFields = GenerateRequiredFields(),
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, useVFX, useTessellation),
                defines = HDShaderPasses.GenerateDefines(CoreDefines.ShaderGraphRaytracingDefault, useVFX, useTessellation),
                keywords = HDShaderPasses.LitDepthOnlyKeywords,
                includes = DepthOnlyIncludes,
                customInterpolators = CoreCustomInterpolators.Common,
            };

            FieldCollection GenerateRequiredFields()
            {
                var fieldCollection = new FieldCollection();

                fieldCollection.Add(CoreRequiredFields.Basic);
                fieldCollection.Add(CoreRequiredFields.AddWriteNormalBuffer);

                return fieldCollection;
            }
        }

        public static PassDescriptor GenerateGBuffer(bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "GBuffer",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBuffer",
                useInPreview = true,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, useVFX, useTessellation),
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = HDShaderPasses.GBufferRenderState,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, useVFX, useTessellation),
                defines = HDShaderPasses.GenerateDefines(CoreDefines.ShaderGraphRaytracingDefault, useVFX, useTessellation),
                keywords = HDShaderPasses.GBufferKeywords,
                includes = GBufferIncludes,
                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common,
            };
        }

        public static PassDescriptor GenerateLitForward(bool useVFX, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                useInPreview = true,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, useVFX, useTessellation),
                // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = CoreRenderStates.Forward,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, useVFX, useTessellation),
                defines = HDShaderPasses.GenerateDefines(CoreDefines.ForwardLit, useVFX, useTessellation),
                includes = ForwardIncludes,
                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common,
            };
        }
        #endregion
    }
}
