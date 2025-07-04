using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;
using UnityEngine;
using static UnityEngine.Rendering.HighDefinition.HDMaterial;
using static UnityEditor.Rendering.HighDefinition.HDFields;
using static UnityEngine.Rendering.HighDefinition.FogVolumeAPI;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    using BlendMode = UnityEngine.Rendering.BlendMode;
    using BlendOp = UnityEditor.ShaderGraph.BlendOp;

    sealed partial class FogVolumeSubTarget : HDSubTarget, IRequiresData<FogVolumeData>
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
        protected override string renderQueue { get; }
        protected override string disableBatchingTag { get; }
        protected override ShaderID shaderID => ShaderID.SG_FogVolume;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Fog Volume Subshader", "");
        protected override string subShaderInclude => null;
        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/FogVolume/ShaderGraph/Templates/FogVolume.template";
        protected override bool supportRaytracing => false;
        protected override bool supportPathtracing => false;

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
                    GetVoxelizePasses(),
                    ShaderGraphPreviewPass(),
                    ShaderGraphOverdrawDebugPass(),
                }
            };
            yield return PostProcessSubShader(subShader);
        }

        protected override IEnumerable<KernelDescriptor> EnumerateKernels()
        {
            yield break;
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

        public static Blend BlendModeToBlend(BlendMode mode)
        {
            switch (mode)
            {
                case BlendMode.Zero: return Blend.Zero;
                case BlendMode.One: return Blend.One;
                case BlendMode.DstColor: return Blend.DstColor;
                case BlendMode.SrcColor: return Blend.SrcColor;
                case BlendMode.OneMinusDstColor: return Blend.OneMinusDstColor;
                case BlendMode.SrcAlpha: return Blend.SrcAlpha;
                case BlendMode.OneMinusSrcColor: return Blend.OneMinusSrcColor;
                case BlendMode.DstAlpha: return Blend.DstAlpha;
                case BlendMode.OneMinusDstAlpha: return Blend.OneMinusDstAlpha;
                case BlendMode.SrcAlphaSaturate: return Blend.SrcAlpha;
                case BlendMode.OneMinusSrcAlpha: return Blend.OneMinusSrcAlpha;
                default: return Blend.Zero;
            }
        }

        public static BlendOp BlendOpToBlendOp(UnityEngine.Rendering.BlendOp op)
        {
            switch (op)
            {
                case UnityEngine.Rendering.BlendOp.Add: return BlendOp.Add;
                case UnityEngine.Rendering.BlendOp.Subtract: return BlendOp.Sub;
                case UnityEngine.Rendering.BlendOp.ReverseSubtract: return BlendOp.RevSub;
                case UnityEngine.Rendering.BlendOp.Min: return BlendOp.Min;
                case UnityEngine.Rendering.BlendOp.Max: return BlendOp.Max;
                default: return BlendOp.Add;
            }
        }

        public static RenderStateCollection GetRenderState(LocalVolumetricFogBlendingMode mode)
        {
            return new RenderStateCollection{
                { RenderState.Cull(Cull.Off) },
                { RenderState.ZTest("Off") },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.Blend(k_SrcColorBlend, k_DstColorBlend, k_SrcAlphaBlend, k_DstAlphaBlend) },
                { RenderState.BlendOp(k_ColorBlendOp, k_AlphaBlendOp) }
            };
        }

        StructDescriptor GetAttributes() => new StructDescriptor
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

        DefineCollection GetBlendModeDefine(LocalVolumetricFogBlendingMode mode)
        {
            return new DefineCollection
            {
                { new KeywordDescriptor{ displayName = ToStringInvariant(mode), referenceName = $"FOG_VOLUME_BLENDING_{ ToStringInvariant(mode).ToUpper(CultureInfo.InvariantCulture)}" }, 1 },
            };
        }

        private string ToStringInvariant(LocalVolumetricFogBlendingMode value)
        {
            return Enum.GetName(typeof(LocalVolumetricFogBlendingMode), value)?.ToString(CultureInfo.InvariantCulture);
        }

        PassCollection GetVoxelizePasses()
        {
            // Unfortunately we need to generate one pass per blend mode because we can't override the render state for one draw call :(
            var blendModePasses = new PassCollection();

            blendModePasses.Add(new PassDescriptor
            {
                // Definition
                displayName = HDShaderPassNames.s_FogVolumeVoxelizeStr,
                referenceName = "SHADERPASS_FOG_VOLUME_VOXELIZATION",
                lightMode = HDShaderPassNames.s_FogVolumeVoxelizeStr,
                useInPreview = false,

                // Port mask
                validVertexBlocks = Array.Empty<BlockFieldDescriptor>(),
                validPixelBlocks = FogVolumeBlocks.FragmentDefault,

                structs = HDShaderPasses.GenerateStructs(new StructCollection
                {
                    { GetAttributes() },
                    { Varyings },
                }, TargetsVFX(), false),
                pragmas = HDShaderPasses.GeneratePragmas(null, TargetsVFX(), false, false),
                defines = HDShaderPasses.GenerateDefines(GetBlendModeDefine(fogVolumeData.blendMode), TargetsVFX(), false),
                renderStates = GetRenderState(fogVolumeData.blendMode),
                includes = FogVolumeIncludes.Voxelize,
            });

            return blendModePasses;
        }

        PassDescriptor ShaderGraphPreviewPass()
        {
            var pass = new PassDescriptor
            {
                // Definition
                displayName = "ShaderGraphPreview",
                referenceName = "SHADERPASS_FOG_VOLUME_PREVIEW",
                lightMode = "ShaderGraphPreview",
                useInPreview = true,

                // Port mask
                validVertexBlocks = new BlockFieldDescriptor[0],
                validPixelBlocks = FogVolumeBlocks.FragmentDefault,

                structs = HDShaderPasses.GenerateStructs(new StructCollection
                {
                    { GetAttributes() },
                    { Varyings },
                }, TargetsVFX(), false),
                pragmas = HDShaderPasses.GeneratePragmas(null, TargetsVFX(), false, false),
                defines = HDShaderPasses.GenerateDefines(null, TargetsVFX(), false),
                renderStates = GetRenderState(LocalVolumetricFogBlendingMode.Additive), // We can't change the blend mode in ShaderGraph
                includes = FogVolumeIncludes.Preview,
            };

            return pass;
        }

        PassDescriptor ShaderGraphOverdrawDebugPass()
        {
            var pass = new PassDescriptor
            {
                // Definition
                displayName = HDShaderPassNames.s_VolumetricFogVFXOverdrawDebugStr,
                referenceName = "SHADERPASS_FOG_VOLUME_OVERDRAW_DEBUG",
                lightMode = HDShaderPassNames.s_VolumetricFogVFXOverdrawDebugStr,
                useInPreview = true,

                // Port mask
                validVertexBlocks = new BlockFieldDescriptor[0],
                validPixelBlocks = FogVolumeBlocks.FragmentDefault,

                structs = HDShaderPasses.GenerateStructs(new StructCollection
                {
                    { GetAttributes() },
                    { Varyings },
                }, TargetsVFX(), false),
                pragmas = HDShaderPasses.GeneratePragmas(null, TargetsVFX(), false, false),
                defines = HDShaderPasses.GenerateDefines(null, TargetsVFX(), false),
                renderStates = GetRenderState(LocalVolumetricFogBlendingMode.Additive), // Overdraw always uses additive blending
                includes = FogVolumeIncludes.OverdrawDebug,
            };

            return pass;
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

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new FogVolumePropertyBlock(fogVolumeData));
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            ComputeBlendParameters(fogVolumeData.blendMode, out var srcColorBlend, out var srcAlphaBlend, out var dstColorBlend, out var dstAlphaBlend, out var colorBlendOp, out var alphaBlendOp);

            collector.AddEnumProperty(k_BlendModeProperty, fogVolumeData.blendMode, HLSLDeclaration.UnityPerMaterial);
            collector.AddEnumProperty(k_DstColorBlendProperty, dstColorBlend);
            collector.AddEnumProperty(k_SrcColorBlendProperty, srcColorBlend);
            collector.AddEnumProperty(k_DstAlphaBlendProperty, dstAlphaBlend);
            collector.AddEnumProperty(k_SrcAlphaBlendProperty, srcAlphaBlend);
            collector.AddEnumProperty(k_ColorBlendOpProperty, colorBlendOp);
            collector.AddEnumProperty(k_AlphaBlendOpProperty, alphaBlendOp);

            collector.AddShaderProperty(new ColorShaderProperty
            {
                hidden = true,
                value = Color.white,
                overrideReferenceName = k_SingleScatteringAlbedoProperty,
                generatePropertyBlock = true,
            });
            collector.AddFloatProperty(k_FogDistanceProperty, 10, HLSLDeclaration.UnityPerMaterial);
        }

        #region BlockMasks
        static class FogVolumeBlocks
        {
            public static BlockFieldDescriptor[] FragmentDefault =
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
            };
        }
        #endregion

        #region Includes
        static class FogVolumeIncludes
        {
            const string kPacking = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl";
            const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
            const string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
            const string kVoxelizationTransforms = "Packages/com.unity.render-pipelines.high-definition/Editor/Material/FogVolume/ShaderGraph/VoxelizationTransforms.hlsl";
            const string kVoxelizePass = "Packages/com.unity.render-pipelines.high-definition/Editor/Material/FogVolume/ShaderGraph/ShaderPassVoxelize.hlsl";
            const string kPreviewPass = "Packages/com.unity.render-pipelines.high-definition/Editor/Material/FogVolume/ShaderGraph/ShaderPassPreview.hlsl";
            const string kOverdrawPass = "Packages/com.unity.render-pipelines.high-definition/Editor/Material/FogVolume/ShaderGraph/OverdrawDebug.hlsl";

            public static IncludeCollection Voxelize = new IncludeCollection
            {
                { kPacking, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.MinimalCorePregraph },
                { kVoxelizationTransforms, IncludeLocation.Pregraph },
                { kVoxelizePass, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Preview = new IncludeCollection
            {
                { kPacking, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.MinimalCorePregraph },
                { kPreviewPass, IncludeLocation.Postgraph },
            };

            public static IncludeCollection OverdrawDebug = new IncludeCollection
            {
                { kPacking, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.MinimalCorePregraph },
                { kOverdrawPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
