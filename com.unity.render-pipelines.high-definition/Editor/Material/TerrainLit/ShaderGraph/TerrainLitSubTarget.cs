using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

using static UnityEngine.Rendering.HighDefinition.HDMaterial;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class TerrainLitSubTarget : LightingSubTarget, IRequiresData<TerrainLitData>
    {
        public TerrainLitSubTarget() => displayName = "TerrainLit";

        private static readonly GUID kSubTargetSourceCodeGuid = new GUID("7771b949c95f4ed9ac018e9db21849e5");
        private static string[] passTemplateMaterialDirectories => new [] {$"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/TerrainLit/ShaderGraph/"};

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/TerrainLit/ShaderGraph/ShaderPass.template";
        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override ShaderID shaderID => ShaderID.SG_Terrain;
        protected override string customInspector => "Rendering.HighDefinition.TerrainLitShaderGraphGUI";
        protected override string renderType => HDRenderTypeTags.Opaque.ToString();
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        internal override MaterialResetter setupMaterialKeywordsAndPassFunc => ShaderGraphAPI.ValidateTerrain;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "TerrainLit SubShader", "");
        protected override string subShaderInclude => CoreIncludes.kTerrainLit;
        protected override string postDecalsInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl";
        protected override string raytracingInclude => CoreIncludes.kTerrainRaytracing;

        protected override bool requireSplitLighting => false;
        protected override bool supportForward => false;
        protected override bool supportLighting => true;
        protected override bool supportDistortion => false;
        protected override bool supportRaytracing => false; // TODO : support later
        protected override bool supportPathtracing => false; // TODO : support later

        static readonly GUID kSourceCodeGuid = new GUID("be136c27a7154cd99820c558d8feedb2"); // TerrainLitSubTarget.cs

        private TerrainLitData m_TerrainLitData;

        TerrainLitData IRequiresData<TerrainLitData>.data
        {
            get => m_TerrainLitData;
            set => m_TerrainLitData = value;
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

        protected override IEnumerable<SubShaderDescriptor> EnumerateSubShaders()
        {
            yield return PostProcessSubShader(GetSubShaderDescriptor());
            yield return PostProcessSubShader(GetBaseMapGenSubShaderDescriptor());

            // Always omit DXR SubShader for VFX until DXR support is added.
            if (!TargetsVFX())
            {
                if (supportRaytracing || supportPathtracing)
                    yield return PostProcessSubShader(GetRaytracingSubShaderDescriptor());
            }
        }

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            return new SubShaderDescriptor
            {
                generatesPreview = true,
                customTags = GetTerrainTags(),
                passes = GetPasses(),
                shaderDependencies = GetDependencies(),
            };

            List<string> GetTerrainTags()
            {
                var tagList = new List<string>();
                tagList.Add("\"SplatCount\" = \"8\"");
                tagList.Add("\"TerrainCompatible\" = \"True\"");

                return tagList;
            }

            PassCollection GetPasses()
            {
                var passes = new PassCollection()
                {
                    GenerateShadowCaster(supportLighting, systemData.tessellation),
                    GenerateMETA(supportLighting),
                    GenerateScenePicking(systemData.tessellation),
                    GenerateSceneSelection(supportLighting, systemData.tessellation),
                };

                if (supportForward)
                {
                    passes.Add(GenerateDepthForwardOnlyPass(supportLighting, systemData.tessellation));
                    passes.Add(GenerateForwardOnlyPass(supportLighting, systemData.tessellation));
                }

                passes.Add(GenerateLitDepthOnly(systemData.tessellation));
                passes.Add(GenerateGBuffer(systemData.tessellation));
                passes.Add(GenerateLitForward(systemData.tessellation));
                //if (!systemData.tessellation) // Raytracing don't support tessellation neither VFX
                //    passes.Add(HDShaderPasses.GenerateLitRaytracingPrepass());

                return passes;
            }

            List<ShaderDependency> GetDependencies()
            {
                var dependencyList = new List<ShaderDependency>();
                dependencyList.Add(TerrainDependencies.BaseMapShader());
                dependencyList.Add(TerrainDependencies.BaseMapGenShader());

                return dependencyList;
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
            pass.keywords.Add(TerrainKeywordDescriptors.TerrainNormalmap);
            pass.keywords.Add(TerrainKeywordDescriptors.TerrainMaskmap);
            pass.keywords.Add(TerrainKeywordDescriptors.Terrain8Layers);
            pass.keywords.Add(TerrainKeywordDescriptors.TerrainBlendHeight);

            if (pass.displayName == "MainTex" || pass.displayName == "MetallicTex")
                pass.defines.Add(TerrainKeywordDescriptors.TerrainBaseMapGen, 1);
            else
                pass.keywords.Add(TerrainKeywordDescriptors.TerrainInstancedPerPixelNormal);

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
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            // Common properties to all Lit master nodes
            var descs = context.blocks.Select(x => x.descriptor);

            // Lit specific properties
            context.AddField(DotsProperties, context.hasDotsProperties);

            // Misc
            context.AddField(LightingGI, descs.Contains(HDBlockFields.SurfaceDescription.BakedGI) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedGI));
            context.AddField(BackLightingGI, descs.Contains(HDBlockFields.SurfaceDescription.BakedBackGI) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI));
            context.AddField(HDFields.AmbientOcclusion, context.blocks.Contains((BlockFields.SurfaceDescription.Occlusion, false)) && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.Occlusion));
            context.AddField(RayTracing, terrainLitData.rayTracing); // TODO : raytracing later

            // Specular Occlusion Fields
            context.AddField(SpecularOcclusionFromAO, lightingData.specularOcclusionMode == SpecularOcclusionMode.FromAO);
            context.AddField(SpecularOcclusionFromAOBentNormal, lightingData.specularOcclusionMode == SpecularOcclusionMode.FromAOAndBentNormal);
            context.AddField(SpecularOcclusionCustom, lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Common block between all "surface" master nodes
            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);

            // Surface
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, systemData.alphaTest);

            // Alpha Test
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass, systemData.alphaTest && builtinData.transparentDepthPrepass);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass, systemData.alphaTest && builtinData.transparentDepthPostpass);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow, systemData.alphaTest && builtinData.alphaTestShadow);

            // Misc
            context.AddBlock(HDBlockFields.SurfaceDescription.DepthOffset, builtinData.depthOffset);

            context.AddBlock(HDBlockFields.VertexDescription.CustomVelocity, systemData.customVelocity);

            context.AddBlock(HDBlockFields.VertexDescription.TessellationFactor, systemData.tessellation);
            context.AddBlock(HDBlockFields.VertexDescription.TessellationDisplacement, systemData.tessellation);

            context.AddBlock(BlockFields.SurfaceDescription.Metallic);

            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);

            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularOcclusion, lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom);

            context.AddBlock(BlockFields.SurfaceDescription.NormalOS, lightingData.normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS, lightingData.normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS, lightingData.normalDropOffSpace == NormalDropOffSpace.World);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = terrainLitData.enableHeightBlend,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_EnableHeightBlend",
                displayName = "Enable Height Blend",
            });

            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Slider,
                value = terrainLitData.heightTransition,
                hidden = false,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_HeightTransition",
                displayName = "Height Transition",
            });

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = terrainLitData.enableInstancedPerPixelNormal,
                hidden = false,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_EnableInstancedPerPixelNormal",
                displayName = "Enable Instanced per Pixel Normal",
            });

            collector.AddShaderProperty(new Texture2DShaderProperty
            {
                defaultType = Texture2DShaderProperty.DefaultType.White,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_TerrainHolesTexture",
                displayName = "Holes Map (RGB)",
            });

            collector.AddShaderProperty(new Texture2DShaderProperty
            {
                defaultType = Texture2DShaderProperty.DefaultType.White,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_MainTex",
                displayName = "Albedo",
            });

            collector.AddShaderProperty(new ColorShaderProperty()
            {
                value = new Color(1.0f, 1.0f, 1.0f, 1.0f),
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.UnityPerMaterial,
                overrideReferenceName = "_Color",
                displayName = "Color,"
            });

            collector.AddShaderProperty(new ColorShaderProperty()
            {
                value = new Color(1.0f, 1.0f, 1.0f, 1.0f),
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.UnityPerMaterial,
                overrideReferenceName = "_EmissionColor",
                displayName = "Color,"
            });

            // ShaderGraph only property used to send the RenderQueueType to the material
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                value = (int)systemData.renderQueueType,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_RenderQueueType",
            });

            //See SG-ADDITIONALVELOCITY-NOTE
            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = builtinData.addPrecomputedVelocity,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = kAddPrecomputedVelocity,
            });

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = builtinData.depthOffset,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = kDepthOffsetEnable
            });

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = builtinData.conservativeDepthOffset,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = kConservativeDepthOffsetEnable
            });

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = builtinData.transparentWritesMotionVec,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = kTransparentWritingMotionVec
            });

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = true,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = kZWrite,
            });

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = systemData.transparentZWrite,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = kTransparentZWrite,
            });

            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                value = (int)CullMode.Back,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_CullMode",
            });

            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Integer,
                value = (int)CompareFunction.LessEqual,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_ZTestDepthEqualForOpaque",
            });

            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Default,
                value = 0.0f,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.Global,
                overrideReferenceName = "_DstBlend",
                displayName = "DstBlend",
            });

            HDSubShaderUtilities.AddStencilShaderProperties(collector, systemData, lightingData, requireSplitLighting);
            HDSubShaderUtilities.AddRayTracingProperty(collector, terrainLitData.rayTracing);
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            base.ProcessPreviewMaterial(material);

            material.SetFloat(kReceivesSSR, lightingData.receiveSSR ? 1 : 0);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var gui = new SubTargetPropertiesGUI(context, onChange, registerUndo, systemData, builtinData, lightingData);
            AddInspectorPropertyBlocks(gui);
            context.Add(gui);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new TerrainLitSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, lightingData, terrainLitData));
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            return base.ComputeMaterialNeedsUpdateHash() * 23 + terrainLitData.rayTracing.GetHashCode();
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

            public static KeywordDescriptor TerrainNormalmap = new KeywordDescriptor()
            {
                displayName = "Terrain Normal Map",
                referenceName = "_NORMALMAP",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor TerrainMaskmap = new KeywordDescriptor()
            {
                displayName = "Terrain Mask Map",
                referenceName = "_MASKMAP",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor Terrain8Layers = new KeywordDescriptor()
            {
                displayName = "Terrain 8 Layers",
                referenceName = "_TERRAIN_8_LAYERS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor TerrainBlendHeight = new KeywordDescriptor()
            {
                displayName = "Terrain Blend Height",
                referenceName = "_TERRAIN_BLEND_HEIGHT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor TerrainBaseMapGen = new KeywordDescriptor()
            {
                displayName = "Terrain Base Map Generation",
                referenceName = "_TERRAIN_BASEMAP_GEN",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor TerrainInstancedPerPixelNormal = new KeywordDescriptor()
            {
                displayName = "Instanced PerPixel Normal",
                referenceName = "_TERRAIN_INSTANCED_PERPIXEL_NORMAL",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };
        }
        #endregion

        #region Pragmas
        static class HDTerrainPasses
        {
            public static readonly KeywordDescriptor AlphaTestOn = new KeywordDescriptor()
            {
                displayName = "_ALPHATEST_ON",
                referenceName = "_ALPHATEST_ON",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static PragmaCollection GeneratePragmas(PragmaCollection input, bool useTessellation)
            {
                var pragmas = HDShaderPasses.GeneratePragmas(input, false, useTessellation);

                pragmas.Add(Pragma.InstancingOptions(new []
                {
                    InstancingOptions.AssumeUniformScaling,
                    InstancingOptions.NoMatrices,
                    InstancingOptions.NoLightProbe,
                    InstancingOptions.NoLightmap,
                }));

                return pragmas;
            }
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
        static public PassDescriptor GenerateShadowCaster(bool supportLighting, bool useTessellation)
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
                structs = HDShaderPasses.GenerateStructs(null, false, useTessellation),
                requiredFields = CoreRequiredFields.Basic,
                renderStates = CoreRenderStates.ShadowCaster,
                pragmas = HDTerrainPasses.GeneratePragmas(CorePragmas.DotsInstanced, useTessellation),
                defines = HDShaderPasses.GenerateDefines(null, false, useTessellation),
                keywords = new KeywordCollection() { HDTerrainPasses.AlphaTestOn, },
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

        public static PassDescriptor GenerateMETA(bool supportLighting)
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
                structs = HDShaderPasses.GenerateStructs(null, false, false),
                requiredFields = CoreRequiredFields.Meta,
                renderStates = CoreRenderStates.Meta,
                // Note: no tessellation for meta pass
                pragmas = HDTerrainPasses.GeneratePragmas(CorePragmas.DotsInstanced, false),
                defines = HDShaderPasses.GenerateDefines(CoreDefines.ShaderGraphRaytracingDefault, false, false),
                keywords = new KeywordCollection() { CoreKeywordDescriptors.EditorVisualization, HDTerrainPasses.AlphaTestOn, },
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

        public static PassDescriptor GenerateScenePicking(bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "ScenePickingPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "Picking",
                useInPreview = false,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, false, useTessellation),
                requiredFields = GenerateRequiredFields(),
                renderStates = CoreRenderStates.ScenePicking,
                pragmas = HDTerrainPasses.GeneratePragmas(CorePragmas.DotsInstancedEditorSync, useTessellation),
                defines = HDShaderPasses.GenerateDefines(CoreDefines.ScenePicking, false, useTessellation),
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

        public static PassDescriptor GenerateSceneSelection(bool supportLighting, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, false, useTessellation),
                requiredFields = CoreRequiredFields.Basic,
                renderStates = CoreRenderStates.SceneSelection,
                pragmas = HDTerrainPasses.GeneratePragmas(CorePragmas.DotsInstancedEditorSync, useTessellation),
                defines = HDShaderPasses.GenerateDefines(CoreDefines.SceneSelection, false, useTessellation),
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

        public static PassDescriptor GenerateDepthForwardOnlyPass(bool supportLighting, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, false, useTessellation),
                requiredFields = GenerateRequiredFields(),
                renderStates = GenerateRenderState(),
                pragmas = HDTerrainPasses.GeneratePragmas(CorePragmas.DotsInstanced, useTessellation),
                defines = HDShaderPasses.GenerateDefines(supportLighting ? CoreDefines.DepthForwardOnly : CoreDefines.DepthForwardOnlyUnlit, false, useTessellation),
                keywords = new KeywordCollection() { HDTerrainPasses.AlphaTestOn, },
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

        public static PassDescriptor GenerateForwardOnlyPass(bool supportLighting, bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = supportLighting ? "SHADERPASS_FORWARD" : "SHADERPASS_FORWARD_UNLIT",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, false, useTessellation),
                // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                requiredFields = supportLighting ? CoreRequiredFields.BasicLighting : CoreRequiredFields.BasicMotionVector,
                renderStates = CoreRenderStates.Forward,
                pragmas = HDTerrainPasses.GeneratePragmas(CorePragmas.DotsInstanced, useTessellation),
                defines = HDShaderPasses.GenerateDefines(supportLighting ? CoreDefines.Forward : CoreDefines.ForwardUnlit, false, useTessellation),
                keywords = new KeywordCollection() { HDTerrainPasses.AlphaTestOn, },
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

        public static PassDescriptor GenerateLitDepthOnly(bool useTessellation)
        {
            return new PassDescriptor
            {
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthOnly",
                useInPreview = true,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, false, useTessellation),
                requiredFields = GenerateRequiredFields(),
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = HDTerrainPasses.GeneratePragmas(CorePragmas.DotsInstanced, useTessellation),
                defines = HDShaderPasses.GenerateDefines(CoreDefines.ShaderGraphRaytracingDefault, false, useTessellation),
                keywords = new KeywordCollection() { CoreKeywordDescriptors.WriteNormalBuffer, HDTerrainPasses.AlphaTestOn, },
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

        public static PassDescriptor GenerateGBuffer(bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "GBuffer",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBuffer",
                useInPreview = true,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, false, useTessellation),
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = HDShaderPasses.GBufferRenderState,
                pragmas = HDTerrainPasses.GeneratePragmas(CorePragmas.DotsInstanced, useTessellation),
                defines = HDShaderPasses.GenerateDefines(CoreDefines.ShaderGraphRaytracingDefault, false, useTessellation),
                keywords = new KeywordCollection() { CoreKeywordDescriptors.LightLayers, HDTerrainPasses.AlphaTestOn, },
                includes = GBufferIncludes,
                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common,
            };
        }

        public static PassDescriptor GenerateLitForward(bool useTessellation)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                useInPreview = true,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, false, useTessellation),
                // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                requiredFields = CoreRequiredFields.BasicLighting,
                renderStates = CoreRenderStates.Forward,
                pragmas = HDTerrainPasses.GeneratePragmas(CorePragmas.DotsInstanced, useTessellation),
                defines = HDShaderPasses.GenerateDefines(CoreDefines.ForwardLit, false, useTessellation),
                keywords = new KeywordCollection() { HDTerrainPasses.AlphaTestOn, },
                includes = ForwardIncludes,
                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common,
            };
        }
        #endregion

        #region Dependencies
        static class TerrainDependencies
        {
            public static ShaderDependency BaseMapShader()
            {
                return new ShaderDependency()
                {
                    dependencyName = "BaseMapShader",
                    shaderName = "Hidden/HDRP/TerrainLit_Basemap",
                };
            }
            public static ShaderDependency BaseMapGenShader()
            {
                return new ShaderDependency()
                {
                    dependencyName = "BaseMapGenShader",
                    shaderName = "{Name}_BaseMapGen",
                };
            }
        }
        #endregion
    }
}
