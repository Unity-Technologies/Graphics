using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

using static UnityEngine.Rendering.HighDefinition.HDMaterial;
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class TerrainLitSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<TerrainLitData>
    {
        public TerrainLitSubTarget() => displayName = "TerrainLit";

        private static readonly GUID kSubTargetSourceCodeGuid = new GUID("7771b949c95f4ed9ac018e9db21849e5");

        private static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Terrain/ShaderGraph/"
        };

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override ShaderID shaderID => ShaderID.SG_Terrain;
        protected override string subShaderInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap_Includes.hlsl";
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Terrain SubShader", "");
        protected override string raytracingInclude => CoreIncludes.kTerrainRaytracing;
        protected override bool requireSplitLighting => false;

        public static FieldDescriptor Standard = new FieldDescriptor(kMaterial, "Standard", "_MATERIAL_FEATURE_TRANSMISSION 1");
        public static readonly KeywordDescriptor Terrain = new KeywordDescriptor()
        {
            displayName = "HD Terrain",
            referenceName = "HD_TERRAIN_ENABLED",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Local,
        };

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
            var descriptor = base.GetSubShaderDescriptor();

            var depthOnlyPass = HDShaderPasses.GenerateLitDepthOnly(TargetsVFX(), systemData.tessellation);
            var gbufferPass = HDShaderPasses.GenerateGBuffer(TargetsVFX(), systemData.tessellation);
            var litForwardPass = HDShaderPasses.GenerateLitForward(TargetsVFX(), systemData.tessellation);
            var litRaytracingPrepass = HDShaderPasses.GenerateLitRaytracingPrepass();

            descriptor.passes.Add(depthOnlyPass);
            descriptor.passes.Add(gbufferPass);
            descriptor.passes.Add(litForwardPass);
            descriptor.passes.Add(litRaytracingPrepass);

            return descriptor;
        }

        protected override SubShaderDescriptor GetRaytracingSubShaderDescriptor()
        {
            var descriptor = base.GetRaytracingSubShaderDescriptor();

            return descriptor;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Lit specific properties
            context.AddField(DotsProperties, context.hasDotsProperties);

            // Material
            context.AddField(Standard);

            // Misc
            context.AddField(RayTracing, terrainLitData.rayTracing);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            context.AddBlock(BlockFields.SurfaceDescription.Metallic);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            HDSubShaderUtilities.AddRayTracingProperty(collector, terrainLitData.rayTracing);
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            base.CollectPassKeywords(ref pass);
            pass.keywords.Add(Terrain);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new TerrainLitSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, terrainLitData));
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            return base.ComputeMaterialNeedsUpdateHash();
        }
    }
}
