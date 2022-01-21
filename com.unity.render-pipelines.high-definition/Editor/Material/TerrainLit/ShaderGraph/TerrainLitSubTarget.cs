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

        static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/TerrainLit/ShaderGraph/"
        };

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

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            return new SubShaderDescriptor
            {
                generatesPreview = true,
                passes = GetPasses(),
            };

            PassCollection GetPasses()
            {
                // terrain won't be supported for VFX
                bool allowsVFX = false;

                var passes = new PassCollection()
                {
                    HDShaderPasses.GenerateShadowCaster(supportLighting, allowsVFX, systemData.tessellation),
                    HDShaderPasses.GenerateMETA(supportLighting, allowsVFX),
                    HDShaderPasses.GenerateScenePicking(allowsVFX, systemData.tessellation),
                    HDShaderPasses.GenerateSceneSelection(supportLighting, allowsVFX, systemData.tessellation),
                };

                if (supportForward)
                {
                    passes.Add(HDShaderPasses.GenerateDepthForwardOnlyPass(supportLighting, allowsVFX, systemData.tessellation));
                    passes.Add(HDShaderPasses.GenerateForwardOnlyPass(supportLighting, allowsVFX, systemData.tessellation));
                }

                passes.Add(HDShaderPasses.GenerateLitDepthOnly(TargetsVFX(), systemData.tessellation));
                passes.Add(HDShaderPasses.GenerateGBuffer(TargetsVFX(), systemData.tessellation));
                passes.Add(HDShaderPasses.GenerateLitForward(TargetsVFX(), systemData.tessellation));
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
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            context.AddBlock(BlockFields.SurfaceDescription.NormalOS, terrainLitData.normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS, terrainLitData.normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS, terrainLitData.normalDropOffSpace == NormalDropOffSpace.World);

            context.AddBlock(BlockFields.SurfaceDescription.Metallic);

            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
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

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new TerrainLitSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, terrainLitData));
        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            return base.ComputeMaterialNeedsUpdateHash();
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
        }
        #endregion

        #region Defines
        static class TerrainDefines
        {
            public static DefineCollection Base = new DefineCollection
            {
                { TerrainKeywordDescriptors.TerrainEnabled, 1 },
            };
        }
        #endregion
    }
}
