using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;

using System;

using static UnityEngine.Rendering.HighDefinition.HDMaterial;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    using BlendMode = UnityEngine.Rendering.BlendMode;
    using BlendOp = UnityEditor.ShaderGraph.BlendOp;

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

        internal static readonly string k_BlendModeProperty = "_FogVolumeBlendMode";
        internal static readonly string k_SrcColorBlendProperty = "_FogVolumeSrcColorBlend";
        internal static readonly string k_DstColorBlendProperty = "_FogVolumeDstColorBlend";
        internal static readonly string k_SrcAlphaBlendProperty = "_FogVolumeSrcAlphaBlend";
        internal static readonly string k_DstAlphaBlendProperty = "_FogVolumeDstAlphaBlend";
        internal static readonly string k_ColorBlendOpProperty = "_FogVolumeColorBlendOp";
        internal static readonly string k_AlphaBlendOpProperty = "_FogVolumeColorBlendOp";

        static readonly string k_SrcColorBlend = $"[{k_SrcColorBlendProperty}]";
        static readonly string k_DstColorBlend = $"[{k_DstColorBlendProperty}]";
        static readonly string k_SrcAlphaBlend = $"[{k_SrcAlphaBlendProperty}]";
        static readonly string k_DstAlphaBlend = $"[{k_DstAlphaBlendProperty}]";
        static readonly string k_ColorBlendOp = $"[{k_ColorBlendOpProperty}]";
        static readonly string k_AlphaBlendOp = $"[{k_AlphaBlendOpProperty}]";

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
                validVertexBlocks = new BlockFieldDescriptor[0],
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
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddProperty("Blend Mode", new UnityEngine.UIElements.EnumField(fogVolumeData.blendMode) { value = fogVolumeData.blendMode }, (evt) =>
            {
                if (Equals(fogVolumeData.blendMode, evt.newValue))
                    return;

                registerUndo("Change Blend Mode");
                fogVolumeData.blendMode = (LocalVolumetricFogBlendingMode)evt.newValue;
                onChange();
            });
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            BlendMode srcColorBlend, srcAlphaBlend, dstColorBlend, dstAlphaBlend;
            BlendOp colorBlendOp = BlendOp.Add, alphaBlendOp = BlendOp.Add;

            // Patch the default blend values depending on the Blend Mode:
            switch (fogVolumeData.blendMode)
            {
                default:
                case LocalVolumetricFogBlendingMode.Additive:
                    srcColorBlend = BlendMode.SrcAlpha;
                    dstColorBlend = BlendMode.One;
                    srcAlphaBlend = BlendMode.One;
                    dstAlphaBlend = BlendMode.One;
                    break;
                case LocalVolumetricFogBlendingMode.Multiply:
                    srcColorBlend = BlendMode.DstColor;
                    dstColorBlend = BlendMode.Zero;
                    srcAlphaBlend = BlendMode.One;
                    dstAlphaBlend = BlendMode.OneMinusSrcAlpha;
                    break;
                case LocalVolumetricFogBlendingMode.Overwrite:
                    srcColorBlend = BlendMode.One;
                    dstColorBlend = BlendMode.Zero;
                    srcAlphaBlend = BlendMode.One;
                    dstAlphaBlend = BlendMode.Zero;
                    break;
                case LocalVolumetricFogBlendingMode.Subtractive:
                    srcColorBlend = BlendMode.SrcAlpha;
                    dstColorBlend = BlendMode.One;
                    srcAlphaBlend = BlendMode.One;
                    dstAlphaBlend = BlendMode.One;
                    alphaBlendOp = BlendOp.Sub;
                    break;
                case LocalVolumetricFogBlendingMode.Max:
                    srcColorBlend = BlendMode.One;
                    dstColorBlend = BlendMode.One;
                    srcAlphaBlend = BlendMode.One;
                    dstAlphaBlend = BlendMode.One;
                    alphaBlendOp = BlendOp.Max;
                    colorBlendOp = BlendOp.Max;
                    break;
                case LocalVolumetricFogBlendingMode.Min:
                    srcColorBlend = BlendMode.One;
                    dstColorBlend = BlendMode.One;
                    srcAlphaBlend = BlendMode.One;
                    dstAlphaBlend = BlendMode.One;
                    alphaBlendOp = BlendOp.Min;
                    colorBlendOp = BlendOp.Min;
                    break;
            }

            collector.AddEnumProperty(k_BlendModeProperty, fogVolumeData.blendMode);
            collector.AddEnumProperty(k_DstColorBlendProperty, dstColorBlend);
            collector.AddEnumProperty(k_SrcColorBlendProperty, srcColorBlend);
            collector.AddEnumProperty(k_DstAlphaBlendProperty, dstAlphaBlend);
            collector.AddEnumProperty(k_SrcAlphaBlendProperty, srcAlphaBlend);
            collector.AddEnumProperty(k_ColorBlendOpProperty, colorBlendOp);
            collector.AddEnumProperty(k_AlphaBlendOpProperty, alphaBlendOp);
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
                { RenderState.Blend(k_SrcColorBlend, k_DstColorBlend) },
                { RenderState.BlendOp(k_ColorBlendOp, k_AlphaBlendOp) },
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
        }
        #endregion
    }
}
