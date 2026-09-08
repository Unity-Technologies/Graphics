using System;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using Unity.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalLight2DSubTarget : UniversalSubTarget
    {
        static readonly UnityEngine.GUID kSourceCodeGuid = new UnityEngine.GUID("a7f3c81b4d5e67891234567890abcdef"); // UniversalLight2DSubTarget.cs

        public UniversalLight2DSubTarget()
        {
            displayName = "Light2D";
        }

        protected override ShaderUtils.ShaderID shaderID => ShaderUtils.ShaderID.SG_Light2D;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddSubShader(PostProcessSubShader(SubShaders.Light2D(target)));
        }

        // Compile-time marker for the volumetric pass. Injected only into the
        // volumetric PassDescriptor's defines, so #if defined(LIGHT2D_VOLUMETRIC_PASS)
        // is a per-pass signal — unlike USE_VOLUMETRIC (a multi_compile keyword that
        // variants every pass). Used in Light2DPass.hlsl to select between the
        // Light2DColor and Light2DVolumetricColor SurfaceDescription fields, since
        // each pass has only one of the two (per the split validPixelBlocks masks).
        internal static readonly KeywordDescriptor kVolumetricPassDefine = new KeywordDescriptor()
        {
            displayName = "Light2D Volumetric Pass",
            referenceName = "LIGHT2D_VOLUMETRIC_PASS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            scope = KeywordScope.Local,
        };

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);
            context.AddField(UniversalFields.SurfaceTransparent);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Only Position is exposed on the vertex stage. Light2D's pass reads
            // positionWS + screen-space normal map (_NormalMap) at the fragment stage,
            // so per-vertex Normal/Tangent ports would be unused sink pins.
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(UniversalBlockFields.SurfaceDescription.Light2DColor);
            context.AddBlock(UniversalBlockFields.SurfaceDescription.Light2DShadowColor);
            // Volumetric-pass counterpart. Registering it here makes the port visible
            // in the master node; the per-pass validPixelBlocks masks below decide which
            // pass actually consumes it.
            context.AddBlock(UniversalBlockFields.SurfaceDescription.Light2DVolumetricColor);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo) { }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            collector.AddFloatProperty(Property.SrcBlend, 1.0f);
            collector.AddFloatProperty(Property.DstBlend, 0.0f);
            // Separate blend properties for the volumetric pass (pass 1) so both passes can
            // live on the same material instance without last-writer-wins conflict at draw time.
            collector.AddFloatProperty(Property.VolSrcBlend, 5.0f);  // SrcAlpha
            collector.AddFloatProperty(Property.VolDstBlend, 1.0f);  // One
        }

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor Light2D(UniversalTarget target)
            {
                return new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kUnlitMaterialTypeTag,
                    renderType = $"{RenderType.Transparent}",
                    renderQueue = $"{UnityEditor.ShaderGraph.RenderQueue.Transparent}",
                    generatesPreview = true,
                    passes = new PassCollection
                    {
                        // Pass 0: non-volumetric — blend driven by _SrcBlend/_DstBlend.
                        { Light2DPasses.Light2D(target) },
                        // Pass 1: volumetric — blend driven by _VolSrcBlend/_VolDstBlend so both
                        // passes can share a single material instance without overwriting each other's
                        // blend state (material.SetFloat is immediate; cmd.DrawMesh holds a reference).
                        { Light2DPasses.Light2DVolumetric(target) },
                    },
                };
            }
        }
        #endregion

        #region Passes
        static class Light2DPasses
        {
            public static PassDescriptor Light2D(UniversalTarget target)
            {
                return new PassDescriptor()
                {
                    displayName = "Light2D",
                    referenceName = "SHADERPASS_SPRITEUNLIT",
                    useInPreview = true,

                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    validVertexBlocks = Light2DBlockMasks.Vertex,
                    validPixelBlocks = Light2DBlockMasks.Fragment,

                    structs = CoreStructCollections.Default,
                    requiredFields = Light2DRequiredFields.Light2D,
                    fieldDependencies = CoreFieldDependencies.Default,

                    renderStates = Light2DRenderStates.Light2D,
                    pragmas = CorePragmas._2DDefault,
                    keywords = Light2DKeywords.Light2D,
                    includes = Light2DIncludes.Light2D,

                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
            }

            public static PassDescriptor Light2DVolumetric(UniversalTarget target)
            {
                // LIGHT2D_VOLUMETRIC_PASS marker so Light2DPass.hlsl picks the correct
                // SurfaceDescription fields for this pass.
                var volumetricDefines = new DefineCollection { { kVolumetricPassDefine, 1 } };

                return new PassDescriptor()
                {
                    displayName = "Light2DVolumetric",
                    referenceName = "SHADERPASS_SPRITEUNLIT",
                    useInPreview = false,

                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    validVertexBlocks = Light2DBlockMasks.Vertex,
                    validPixelBlocks = Light2DBlockMasks.FragmentVolumetric,

                    structs = CoreStructCollections.Default,
                    requiredFields = Light2DRequiredFields.Light2D,
                    fieldDependencies = CoreFieldDependencies.Default,

                    renderStates = Light2DRenderStates.Light2DVolumetric,
                    pragmas = CorePragmas._2DDefault,
                    defines = volumetricDefines,
                    keywords = Light2DKeywords.Light2D,
                    includes = Light2DIncludes.Light2D,

                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
            }
        }
        #endregion

        #region PortMasks
        private static class Light2DBlockMasks
        {
            public static readonly BlockFieldDescriptor[] Vertex = new BlockFieldDescriptor[]
            {
                BlockFields.VertexDescription.Position,
            };

            public static readonly BlockFieldDescriptor[] Fragment = new BlockFieldDescriptor[]
            {
                UniversalBlockFields.SurfaceDescription.Light2DColor,
                UniversalBlockFields.SurfaceDescription.Light2DShadowColor,
            };

            // Volumetric pass sees only its own Color block. Splitting the mask
            // per-pass — rather than sharing one mask and branching in HLSL — means each
            // pass's SurfaceDescription only pulls in the graph subtree feeding its
            // own ports, so nodes wired to non-volumetric Color never compile
            // into the volumetric variant (and vice versa).
            public static readonly BlockFieldDescriptor[] FragmentVolumetric = new BlockFieldDescriptor[]
            {
                UniversalBlockFields.SurfaceDescription.Light2DVolumetricColor,
            };
        }
        #endregion

        #region RequiredFields
        private static class Light2DRequiredFields
        {
            public static readonly FieldCollection Light2D = new FieldCollection()
            {
                StructFields.Attributes.color,
                // Attributes.uv0 / Varyings.texCoord0 are only meaningful for Sprite lights
                // (_L2D_LIGHT_TYPE == 2), which sample _CookieTex at the mesh UV — mirroring
                // Hidden/Light2D's vert_shape_shared setting `o.uv = a.uv` for type 2. The
                // other shape types bake their falloff U into vertex color alpha and don't
                // need uv0, but interpolating it unconditionally keeps a single vertex layout
                // rather than a per-type variant, matching how the built-in shader declares
                // uv0 in its shared Attributes struct.
                StructFields.Attributes.uv0,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.color,
                StructFields.Varyings.texCoord0,
            };
        }
        #endregion

        #region RenderStates
        private static class Light2DRenderStates
        {
            public static readonly RenderStateCollection Light2D = new RenderStateCollection
            {
                { RenderState.Blend($"Blend [{Property.SrcBlend}] [{Property.DstBlend}]") },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.ZTest("Off") },
                { RenderState.Cull(Cull.Off) },
            };

            public static readonly RenderStateCollection Light2DVolumetric = new RenderStateCollection
            {
                { RenderState.Blend($"Blend [{Property.VolSrcBlend}] [{Property.VolDstBlend}]") },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.ZTest("Off") },
                { RenderState.Cull(Cull.Off) },
            };
        }
        #endregion

        #region Keywords
        private static class Light2DKeywords
        {
            static readonly KeywordDescriptor UseNormalMap = new KeywordDescriptor()
            {
                displayName = "Use Normal Map",
                referenceName = "USE_NORMAL_MAP",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
            };

            static readonly KeywordDescriptor UseAdditiveBlending = new KeywordDescriptor()
            {
                displayName = "Use Additive Blending",
                referenceName = "USE_ADDITIVE_BLENDING",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
            };

            static readonly KeywordDescriptor UseVolumetric = new KeywordDescriptor()
            {
                displayName = "Use Volumetric",
                referenceName = "USE_VOLUMETRIC",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
            };

            static readonly KeywordDescriptor UsePointLightCookies = new KeywordDescriptor()
            {
                displayName = "Use Point Light Cookies",
                referenceName = "USE_POINT_LIGHT_COOKIES",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
            };

            static readonly KeywordDescriptor LightQualityFast = new KeywordDescriptor()
            {
                displayName = "Light Quality Fast",
                referenceName = "LIGHT_QUALITY_FAST",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordCollection Light2D = new KeywordCollection
            {
                { CoreKeywordDescriptors.ShapeLightType0 },
                { CoreKeywordDescriptors.ShapeLightType1 },
                { CoreKeywordDescriptors.ShapeLightType2 },
                { CoreKeywordDescriptors.ShapeLightType3 },
                { UseNormalMap },
                { UseAdditiveBlending },
                { UseVolumetric },
                { UsePointLightCookies },
                { LightQualityFast },
                { CoreKeywordDescriptors.LightLayers },
            };
        }
        #endregion

        #region Includes
        private static class Light2DIncludes
        {
            const string kLightingUtility = "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl";
            const string kCommonLighting = "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl";
            const string kLight2DGlobals = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/Light2DGlobals.hlsl";
            const string kLight2DPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/Light2DPass.hlsl";

            public static readonly IncludeCollection Light2D = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kLightingUtility, IncludeLocation.Pregraph },
                { kCommonLighting, IncludeLocation.Pregraph },
                { kLight2DGlobals, IncludeLocation.Pregraph },
                { CoreIncludes.CorePostgraph },
                { kLight2DPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
