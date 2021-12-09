using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Legacy;

using static UnityEngine.Rendering.HighDefinition.HDMaterial;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class FogVolumeSubTarget : SurfaceSubTarget, IRequiresData<FogVolumeData>
    {
        public FogVolumeSubTarget() => displayName = "Fog Volume";

        public readonly static string k_VoxelizePassName = "FogVolumeVoxelize";

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("0e63daeaa7722194d90b56081aa39ae6");  // FogVolumeSubTarget.cs
        static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/FogVolume/ShaderGraph/"
        };

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override string customInspector => "Rendering.HighDefinition.FogVolumeShaderGUI";
        internal override MaterialResetter setupMaterialKeywordsAndPassFunc => ShaderGraphAPI.ValidateFogVolumeMaterial;
        protected override string renderType => HDRenderTypeTags.HDFogVolumeShader.ToString();
        protected override ShaderID shaderID => ShaderID.SG_FogVolume;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Fog Volume Subshader", "");
        protected override string subShaderInclude => "Packages/com.unity.render-pipelines.high-definition/Editor/Material/FogVolume/FogVolumeShaderGraph.hlsl";
        protected override bool supportRaytracing => false;
        protected override bool supportPathtracing => false;

        // Material Data
        FogVolumeData m_FogVolumeData;

        // Interface Properties
        FogVolumeData IRequiresData<FogVolumeData>.data
        {
            get => m_FogVolumeData;
            set => m_FogVolumeData = value;
        }

        // Public properties
        public FogVolumeData fogVolumeData
        {
            get => m_FogVolumeData;
            set => m_FogVolumeData = value;
        }

        protected override IEnumerable<SubShaderDescriptor> EnumerateSubShaders()
        {
            var subShader = new SubShaderDescriptor()
            {
                generatesPreview = true,
                passes = new PassCollection
                {
                    { FogVolume.ScenePicking },
                    { FogVolume.Voxelize },
                },
            };
            yield return PostProcessSubShader(subShader);
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            // pass.keywords.Add(CoreKeywordDescriptors.AlphaTest, new FieldCondition(Fields.AlphaTest, true));

            // // Emissive pass only have the emission keyword
            // if (!(pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalProjectorForwardEmissive] ||
            //       pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalMeshForwardEmissive]))
            // {
            //     if (decalData.affectsAlbedo)
            //         pass.keywords.Add(FogVolumeDefines.Albedo);
            //     if (decalData.affectsNormal)
            //         pass.keywords.Add(FogVolumeDefines.Normal);
            //     if (decalData.affectsMaskmap)
            //         pass.keywords.Add(FogVolumeDefines.Maskmap);
            // }

            // if (pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalMeshForwardEmissive] ||
            //     pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh])
            // {
            //     pass.keywords.Add(CoreKeywordDescriptors.LodFadeCrossfade, new FieldCondition(Fields.LodCrossFade, true));
            // }
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            // Decal properties
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Decal
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            // blockList.AddPropertyBlock(new DecalPropertyBlock(decalData));
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
        }

        #region Passes
        public static class FogVolume
        {
            public static PassDescriptor ScenePicking = new PassDescriptor()
            {
                // Definition
                displayName = "ScenePickingPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "Picking",
                useInPreview = false,

                // Collections
                structs = CoreStructCollections.Basic,
                renderStates = FogVolumeRenderStates.ScenePicking,
                pragmas = CorePragmas.Basic,
                defines = CoreDefines.ScenePicking,
                includes = FogVolumeIncludes.ScenePicking,
                customInterpolators = CoreCustomInterpolators.Common,
            };

            public static PassDescriptor Voxelize = new PassDescriptor()
            {
                // Definition
                displayName = k_VoxelizePassName,
                referenceName = "SHADERPASS_FOGVOLUME_VOXELIZATION",
                lightMode = k_VoxelizePassName,
                useInPreview = false,

                // Port mask
                validPixelBlocks = FogVolumeBlocks.FragmentDefault,

                structs = CoreStructCollections.Basic,
                renderStates = FogVolumeRenderStates.Voxelize,
                pragmas = CorePragmas.Basic,
                // keywords = FogVolumeDefines.Decals,
                includes = FogVolumeIncludes.Default,
                customInterpolators = CoreCustomInterpolators.Common,
            };
        }
        #endregion

        #region BlockMasks
        static class FogVolumeBlocks
        {
            public static BlockFieldDescriptor[] FragmentDefault = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
            };
        }
        #endregion

        #region RenderStates
        static class FogVolumeRenderStates
        {
            public static RenderStateCollection ScenePicking = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Back) },
            };

            public static RenderStateCollection Voxelize = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
                // { RenderState.Stencil(new StencilDescriptor()
                // {
                //     WriteMask = $"[{k_FogVolumeStencilWriteMask}]",
                //     Ref = $"[{k_FogVolumeStencilRef}]",
                //     Comp = "Always",
                //     Pass = "Replace",
                // }) },
            };
        }
        #endregion

        #region Includes
        static class FogVolumeIncludes
        {
            const string kPacking = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl";
            const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
            const string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
            // const string k_FogVolume = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/FogVolume.hlsl";
            // TODO: depth write pass!
            const string k_VoxelizePass = "Packages/com.unity.render-pipelines.high-definition/Editor/Material/FogVolume/ShaderGraph/ShaderPassVoxelize.hlsl";

            public static IncludeCollection Default = new IncludeCollection
            {
                { kPacking, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.MinimalCorePregraph },
                // { k_FogVolume, IncludeLocation.Pregraph },
                { k_VoxelizePass, IncludeLocation.Postgraph }, // TODO: scene selection pass!
            };

            public static IncludeCollection ScenePicking = new IncludeCollection
            {
                { kPacking, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.MinimalCorePregraph },
                { CoreIncludes.kPickingSpaceTransforms, IncludeLocation.Pregraph },
                // { k_FogVolume, IncludeLocation.Pregraph },
                { CoreIncludes.kUnlit, IncludeLocation.Postgraph }, // For the scene picking, we can use the unlit template
            };
        }
        #endregion
    }
}
