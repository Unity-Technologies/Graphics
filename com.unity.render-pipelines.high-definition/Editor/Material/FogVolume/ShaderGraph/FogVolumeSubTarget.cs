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

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("0e63daeaa7722194d90b56081aa39ae6");  // FogVolumeSubTarget.cs

        protected override string[] templateMaterialDirectories => new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/FogVolume/ShaderGraph/"
        };
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override string customInspector => "Rendering.HighDefinition.FogVolumeShaderGUI";
        internal override MaterialResetter setupMaterialKeywordsAndPassFunc => ShaderGraphAPI.ValidateFogVolumeMaterial;
        protected override string renderType => HDRenderTypeTags.HDFogVolumeShader.ToString();
        protected override ShaderID shaderID => ShaderID.SG_FogVolume;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Fog Volume Subshader", "");
        protected override string subShaderInclude => null;
        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/FogVolume/ShaderGraph/Templates/FogVolume.template";
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
                    // TODO: Scene picking and scene selection passes
                    // { FogVolume.ScenePicking },
                    // { GetWriteDepthPass() },
                    { GetVoxelizePass() },
                },
            };
            yield return PostProcessSubShader(subShader);
        }

        public static StructDescriptor Varyings = new StructDescriptor()
        {
            name = "Varyings",
            packFields = true,
            populateWithCustomInterpolators = false,
            fields = new FieldDescriptor[]
            {
                StructFields.Varyings.positionCS,
                StructFields.Varyings.instanceID, // We always need both instanceId and vertexId
                StructFields.Attributes.vertexID,
            }
        };

        // PassDescriptor GetWriteDepthPass()
        // {
        //     return new PassDescriptor()
        //     {
        //         // Definition
        //         displayName = HDShaderPassNames.s_FogVolumeDepthPassStr,
        //         referenceName = "SHADERPASS_FOGVOLUME_WRITE_DEPTH",
        //         lightMode = HDShaderPassNames.s_FogVolumeDepthPassStr,
        //         useInPreview = false,

        //         // Port mask
        //         validPixelBlocks = FogVolumeBlocks.FragmentDefault,
        //         requiredFields = new FieldCollection { StructFields.SurfaceDescriptionInputs.FaceSign },

        //         // structs = HDShaderPasses.GenerateStructs(new StructCollection
        //         // {
        //         //     { Structs.SurfaceDescriptionInputs },
        //         //     { Varyings },
        //         //     { Structs.VertexDescriptionInputs },
        //         // }, TargetsVFX(), false),
        //         structs = HDShaderPasses.GenerateStructs(null, TargetsVFX(), false),
        //         pragmas = HDShaderPasses.GeneratePragmas(null, TargetsVFX(), false),
        //         defines = HDShaderPasses.GenerateDefines(null, TargetsVFX(), false),
        //         renderStates = FogVolumeRenderStates.DepthPass,
        //         includes = FogVolumeIncludes.WriteDepth,
        //     };
        // }

        PassDescriptor GetVoxelizePass()
        {
            var attributes = new StructDescriptor
            {
                name = "Attributes",
                packFields = false,
                fields = new FieldDescriptor[]
                {
                    StructFields.Attributes.instanceID,
                    StructFields.Attributes.vertexID,
                    StructFields.Attributes.positionOS,
                }
            };

            return new PassDescriptor()
            {
                // Definition
                displayName = HDShaderPassNames.s_FogVolumeVoxelizeStr,
                referenceName = "SHADERPASS_FOGVOLUME_VOXELIZATION",
                lightMode = HDShaderPassNames.s_FogVolumeVoxelizeStr,
                useInPreview = false,

                // Port mask
                validVertexBlocks = null,
                validPixelBlocks = FogVolumeBlocks.FragmentDefault,

                structs = HDShaderPasses.GenerateStructs(new StructCollection
                {
                    { attributes },
                    { Varyings },
                }, TargetsVFX(), false),
                pragmas = HDShaderPasses.GeneratePragmas(null, TargetsVFX(), false),
                defines = HDShaderPasses.GenerateDefines(null, TargetsVFX(), false),
                renderStates = FogVolumeRenderStates.Voxelize,
                includes = FogVolumeIncludes.Voxelize,
            };
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            base.CollectPassKeywords(ref pass);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            context.AddField(Fields.GraphVertex);
            context.AddField(HDStructFields.FragInputs.positionRWS);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
        }

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
            // public static RenderStateCollection ScenePicking = new RenderStateCollection
            // {
            //     { RenderState.Cull(Cull.Back) },
            // };

            public static RenderStateCollection DepthPass = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off) },
                { RenderState.ZTest(ZTest.Always) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.ZClip("Off") },
            };

            public static RenderStateCollection Voxelize = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off) }, // When we do the ray marching, we don't want the camera to clip in the geometry
                { RenderState.ZTest(ZTest.Always) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha) }, // TODO: blend mode options
            };
        }
        #endregion

        #region Includes
        static class FogVolumeIncludes
        {
            const string kPacking = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl";
            const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
            const string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
            const string kVoxelizePass = "Packages/com.unity.render-pipelines.high-definition/Editor/Material/FogVolume/ShaderGraph/ShaderPassVoxelize.hlsl";
            const string kWriteDepthPass = "Packages/com.unity.render-pipelines.high-definition/Editor/Material/FogVolume/ShaderGraph/ShaderPassWriteDepth.hlsl";

            public static IncludeCollection Voxelize = new IncludeCollection
            {
                { kPacking, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.MinimalCorePregraph },
                { kVoxelizePass, IncludeLocation.Postgraph }, // TODO: scene selection pass!
            };

            // public static IncludeCollection WriteDepth = new IncludeCollection
            // {
            //     { kPacking, IncludeLocation.Pregraph },
            //     { kColor, IncludeLocation.Pregraph },
            //     { kFunctions, IncludeLocation.Pregraph },
            //     { CoreIncludes.MinimalCorePregraph },
            //     { kWriteDepthPass, IncludeLocation.Postgraph }, // TODO: scene selection pass!
            // };
        }
        #endregion
    }
}
