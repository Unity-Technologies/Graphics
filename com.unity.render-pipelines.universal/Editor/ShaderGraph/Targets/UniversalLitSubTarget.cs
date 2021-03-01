using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;

using static UnityEditor.Rendering.Universal.ShaderGraph.SubShaderUtils;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalLitSubTarget : UniversalSubTarget, ILegacyTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("d6c78107b64145745805d963de80cc17"); // UniversalLitSubTarget.cs

        [SerializeField]
        WorkflowMode m_WorkflowMode = WorkflowMode.Metallic;

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace = NormalDropOffSpace.Tangent;

        [SerializeField]
        bool m_ClearCoat = false;

        public UniversalLitSubTarget()
        {
            displayName = "Lit";
        }

        public WorkflowMode workflowMode
        {
            get => m_WorkflowMode;
            set => m_WorkflowMode = value;
        }

        public NormalDropOffSpace normalDropOffSpace
        {
            get => m_NormalDropOffSpace;
            set => m_NormalDropOffSpace = value;
        }

        public bool clearCoat
        {
            get => m_ClearCoat;
            set => m_ClearCoat = value;
        }

        private bool complexLit
        {
            get
            {
                // Rules for switching to ComplexLit with forward only pass
                return clearCoat; // && <complex feature>
            }
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);

            if (!context.HasCustomEditorForRenderPipeline(typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)))
            {
                context.AddCustomEditorForRenderPipeline("UnityEditor.URPLitGUI", typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset));
            }

            // Process SubShaders
            context.AddSubShader(SubShaders.LitComputeDotsSubShader(workflowMode, target.renderType, target.renderQueue, complexLit));
            context.AddSubShader(SubShaders.LitGLESSubShader(workflowMode, target.renderType, target.renderQueue, complexLit));
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);
            // Surface Type & Blend Mode
            // These must be set per SubTarget as Sprite SubTargets override them
            context.AddField(UniversalFields.SurfaceOpaque,         target.surfaceType == SurfaceType.Opaque);
            context.AddField(UniversalFields.SurfaceTransparent,    target.surfaceType != SurfaceType.Opaque);
            context.AddField(UniversalFields.BlendAdd,              target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Additive);
            context.AddField(Fields.BlendAlpha,                     target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Alpha);
            context.AddField(UniversalFields.BlendMultiply,         target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Multiply);
            context.AddField(UniversalFields.BlendPremultiply,      target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Premultiply);

            // Lit
            context.AddField(UniversalFields.NormalDropOffOS,       normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(UniversalFields.NormalDropOffTS,       normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(UniversalFields.NormalDropOffWS,       normalDropOffSpace == NormalDropOffSpace.World);
            context.AddField(UniversalFields.Normal,                descs.Contains(BlockFields.SurfaceDescription.NormalOS) ||
                descs.Contains(BlockFields.SurfaceDescription.NormalTS) ||
                descs.Contains(BlockFields.SurfaceDescription.NormalWS));
            // Complex Lit

            // Template Predicates
            //context.AddField(UniversalFields.PredicateClearCoat, clearCoat);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.NormalOS,           normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS,           normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS,           normalDropOffSpace == NormalDropOffSpace.World);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
            context.AddBlock(BlockFields.SurfaceDescription.Specular,           workflowMode != WorkflowMode.Metallic);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic,           workflowMode != WorkflowMode.Specular);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha,              target.surfaceType == SurfaceType.Transparent || target.alphaClip);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, target.alphaClip);
            context.AddBlock(BlockFields.SurfaceDescription.CoatMask,           clearCoat);
            context.AddBlock(BlockFields.SurfaceDescription.CoatSmoothness,     clearCoat);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (workflowMode == WorkflowMode.MaterialChoice)
                collector.AddShaderProperty(Property.WorkflowMode(WorkflowMode.Metallic));   // TODO: setup default correctly here based on subtarget state
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddProperty("Workflow", new EnumField(WorkflowMode.Metallic) { value = workflowMode }, (evt) =>
            {
                if (Equals(workflowMode, evt.newValue))
                    return;

                registerUndo("Change Workflow");
                workflowMode = (WorkflowMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Surface", new EnumField(SurfaceType.Opaque) { value = target.surfaceType }, (evt) =>
            {
                if (Equals(target.surfaceType, evt.newValue))
                    return;

                registerUndo("Change Surface");
                target.surfaceType = (SurfaceType)evt.newValue;
                onChange();
            });

            context.AddProperty("Blend", new EnumField(AlphaMode.Alpha) { value = target.alphaMode }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.alphaMode, evt.newValue))
                    return;

                registerUndo("Change Blend");
                target.alphaMode = (AlphaMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Alpha Clip", new Toggle() { value = target.alphaClip }, (evt) =>
            {
                if (Equals(target.alphaClip, evt.newValue))
                    return;

                registerUndo("Change Alpha Clip");
                target.alphaClip = evt.newValue;
                onChange();
            });

            context.AddProperty("Two Sided", new Toggle() { value = target.twoSided }, (evt) =>
            {
                if (Equals(target.twoSided, evt.newValue))
                    return;

                registerUndo("Change Two Sided");
                target.twoSided = evt.newValue;
                onChange();
            });

            context.AddProperty("Fragment Normal Space", new EnumField(NormalDropOffSpace.Tangent) { value = normalDropOffSpace }, (evt) =>
            {
                if (Equals(normalDropOffSpace, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
                onChange();
            });

            context.AddProperty("Clear Coat", new Toggle() { value = clearCoat }, (evt) =>
            {
                if (Equals(clearCoat, evt.newValue))
                    return;

                registerUndo("Change Clear Coat");
                clearCoat = evt.newValue;
                onChange();
            });
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if (!(masterNode is PBRMasterNode1 pbrMasterNode))
                return false;

            m_WorkflowMode = (WorkflowMode)pbrMasterNode.m_Model;
            m_NormalDropOffSpace = (NormalDropOffSpace)pbrMasterNode.m_NormalDropOffSpace;

            // Handle mapping of Normal block specifically
            BlockFieldDescriptor normalBlock;
            switch (m_NormalDropOffSpace)
            {
                case NormalDropOffSpace.Object:
                    normalBlock = BlockFields.SurfaceDescription.NormalOS;
                    break;
                case NormalDropOffSpace.World:
                    normalBlock = BlockFields.SurfaceDescription.NormalWS;
                    break;
                default:
                    normalBlock = BlockFields.SurfaceDescription.NormalTS;
                    break;
            }

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 9 },
                { BlockFields.VertexDescription.Normal, 10 },
                { BlockFields.VertexDescription.Tangent, 11 },
                { BlockFields.SurfaceDescription.BaseColor, 0 },
                { normalBlock, 1 },
                { BlockFields.SurfaceDescription.Emission, 4 },
                { BlockFields.SurfaceDescription.Smoothness, 5 },
                { BlockFields.SurfaceDescription.Occlusion, 6 },
                { BlockFields.SurfaceDescription.Alpha, 7 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 8 },
            };

            // PBRMasterNode adds/removes Metallic/Specular based on settings
            if (m_WorkflowMode == WorkflowMode.Specular)
                blockMap.Add(BlockFields.SurfaceDescription.Specular, 3);
            else if (m_WorkflowMode == WorkflowMode.Metallic)
                blockMap.Add(BlockFields.SurfaceDescription.Metallic, 2);

            return true;
        }

        #region SubShader
        static class SubShaders
        {
            // SM 4.5, compute with dots instancing
            public static SubShaderDescriptor LitComputeDotsSubShader(WorkflowMode workflowMode, string renderType, string renderQueue, bool complexLit)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kLitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                if (complexLit)
                    result.passes.Add(LitPasses.ForwardOnly(workflowMode, complexLit, CoreBlockMasks.Vertex, LitBlockMasks.FragmentComplexLit, CorePragmas.DOTSForward));
                else
                    result.passes.Add(LitPasses.Forward(workflowMode, CorePragmas.DOTSForward));

                if (!complexLit)
                    result.passes.Add(LitPasses.GBuffer(workflowMode));

                result.passes.Add(PassVariant(CorePasses.ShadowCaster,   CorePragmas.DOTSInstanced));
                result.passes.Add(PassVariant(CorePasses.DepthOnly,      CorePragmas.DOTSInstanced));
                result.passes.Add(PassVariant(LitPasses.DepthNormalOnly, CorePragmas.DOTSInstanced));
                result.passes.Add(PassVariant(LitPasses.Meta,            CorePragmas.DOTSDefault));
                result.passes.Add(PassVariant(LitPasses._2D,             CorePragmas.DOTSDefault));

                return result;
            }

            public static SubShaderDescriptor LitGLESSubShader(WorkflowMode workflowMode, string renderType, string renderQueue, bool complexLit)
            {
                // SM 2.0, GLES

                // ForwardOnly pass is used as complex Lit SM 2.0 fallback for GLES.
                // Drops advanced features and renders materials as Lit.

                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kLitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                if (complexLit)
                    result.passes.Add(LitPasses.ForwardOnly(workflowMode, complexLit));
                else
                    result.passes.Add(LitPasses.Forward(workflowMode));

                result.passes.Add(CorePasses.ShadowCaster);
                result.passes.Add(CorePasses.DepthOnly);
                result.passes.Add(LitPasses.DepthNormalOnly);
                result.passes.Add(LitPasses.Meta);
                result.passes.Add(LitPasses._2D);

                return result;
            }
        }
        #endregion

        #region Passes
        static class LitPasses
        {
            public static PassDescriptor Forward(WorkflowMode workflowMode, PragmaCollection pragmas = null)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Universal Forward",
                    referenceName = "SHADERPASS_FORWARD",
                    lightMode = "UniversalForward",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberDefault,
                    pragmas = pragmas ?? CorePragmas.Forward,     // NOTE: SM 2.0 only GL
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection() { LitKeywords.Forward, LitKeywords.ReceiveShadows },
                    includes = LitIncludes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                // setup defines
                if (workflowMode == WorkflowMode.Specular)
                    result.defines.Add(LitDefines.SpecularSetup, 1);
                else if (workflowMode == WorkflowMode.MaterialChoice)
                    result.keywords.Add(LitDefines.SpecularSetup);

                return result;
            }

            public static PassDescriptor ForwardOnly(
                WorkflowMode workflowMode,
                bool complexLit,
                BlockFieldDescriptor[] vertexBlocks = null,
                BlockFieldDescriptor[] pixelBlocks = null,
                PragmaCollection pragmas = null)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Universal Forward Only",
                    referenceName = "SHADERPASS_FORWARDONLY",
                    lightMode = "UniversalForwardOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = vertexBlocks ?? CoreBlockMasks.Vertex,
                    validPixelBlocks = pixelBlocks ?? LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberDefault,
                    pragmas = pragmas ?? CorePragmas.Forward,    // NOTE: SM 2.0 only GL
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection() { LitKeywords.Forward, LitKeywords.ReceiveShadows },
                    includes = LitIncludes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                // setup defines
                if (complexLit)
                    result.defines.Add(LitDefines.ClearCoat, 1);

                if (workflowMode == WorkflowMode.Specular)
                    result.defines.Add(LitDefines.SpecularSetup, 1);
                else if (workflowMode == WorkflowMode.MaterialChoice)
                    result.keywords.Add(LitDefines.SpecularSetup);

                return result;
            }

            // Deferred only in SM4.5, MRT not supported in GLES2
            public static PassDescriptor GBuffer(WorkflowMode workflowMode)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "GBuffer",
                    referenceName = "SHADERPASS_GBUFFER",
                    lightMode = "UniversalGBuffer",

                    // Template
                    passTemplatePath = UniversalTarget.kTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.GBuffer,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberDefault,
                    pragmas = CorePragmas.DOTSGBuffer,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection() { LitKeywords.GBuffer },
                    includes = LitIncludes.GBuffer,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                if (workflowMode == WorkflowMode.Specular)
                    result.defines.Add(LitDefines.SpecularSetup, 1);
                else if (workflowMode == WorkflowMode.MaterialChoice)
                    result.keywords.Add(LitDefines.SpecularSetup);

                return result;
            }

            public static PassDescriptor Meta = new PassDescriptor()
            {
                // Definition
                displayName = "Meta",
                referenceName = "SHADERPASS_META",
                lightMode = "Meta",

                // Template
                passTemplatePath = UniversalTarget.kTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentMeta,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = LitRequiredFields.Meta,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.Default,
                keywords = LitKeywords.Meta,
                includes = LitIncludes.Meta,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            public static readonly PassDescriptor _2D = new PassDescriptor()
            {
                // Definition
                referenceName = "SHADERPASS_2D",
                lightMode = "Universal2D",

                // Template
                passTemplatePath = UniversalTarget.kTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.UberDefault,
                pragmas = CorePragmas.Instanced,
                keywords = LitKeywords._2D,
                includes = LitIncludes._2D,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            public static readonly PassDescriptor DepthNormalOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthNormals",
                referenceName = "SHADERPASS_DEPTHNORMALSONLY",
                lightMode = "DepthNormals",
                useInPreview = false,

                // Template
                passTemplatePath = UniversalTarget.kTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = LitBlockMasks.FragmentDepthNormals,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = LitRequiredFields.DepthNormals,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.DepthNormalsOnly,
                pragmas = CorePragmas.Instanced,
                keywords = LitKeywords.DepthNormalsOnly,
                includes = CoreIncludes.DepthNormalsOnly,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };
        }
        #endregion

        #region PortMasks
        static class LitBlockMasks
        {
            public static readonly BlockFieldDescriptor[] FragmentLit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Specular,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };

            public static readonly BlockFieldDescriptor[] FragmentComplexLit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Specular,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                BlockFields.SurfaceDescription.CoatMask,
                BlockFields.SurfaceDescription.CoatSmoothness,
            };

            public static readonly BlockFieldDescriptor[] FragmentMeta = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };

            public static readonly BlockFieldDescriptor[] FragmentDepthNormals = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };
        }
        #endregion

        #region RequiredFields
        static class LitRequiredFields
        {
            public static readonly FieldCollection Forward = new FieldCollection()
            {
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                StructFields.Varyings.viewDirectionWS,
                UniversalStructFields.Varyings.lightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static readonly FieldCollection GBuffer = new FieldCollection()
            {
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                StructFields.Varyings.viewDirectionWS,
                UniversalStructFields.Varyings.lightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static readonly FieldCollection DepthNormals = new FieldCollection()
            {
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
            };

            public static readonly FieldCollection Meta = new FieldCollection()
            {
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Attributes.uv2,                            //needed for meta vertex position
            };
        }
        #endregion

        #region Defines
        static class LitDefines
        {
            public static readonly KeywordDescriptor ClearCoat = new KeywordDescriptor()
            {
                displayName = "Clear Coat",
                referenceName = "_CLEARCOAT 1",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordDescriptor SpecularSetup = new KeywordDescriptor()
            {
                displayName = "Specular Setup",
                referenceName = "_SPECULAR_SETUP",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment
            };
        }
        #endregion

        #region Keywords
        static class LitKeywords
        {
            public static readonly KeywordDescriptor ReceiveShadows = new KeywordDescriptor()
            {
                displayName = "Receive Shadows Off",
                referenceName = "_RECEIVE_SHADOWS_OFF",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordDescriptor GBufferNormalsOct = new KeywordDescriptor()
            {
                displayName = "GBuffer normal octaedron encoding",
                referenceName = "_GBUFFER_NORMALS_OCT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static readonly KeywordDescriptor ScreenSpaceAmbientOcclusion = new KeywordDescriptor()
            {
                displayName = "Screen Space Ambient Occlusion",
                referenceName = "_SCREEN_SPACE_OCCLUSION",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static readonly KeywordCollection Forward = new KeywordCollection
            {
                { ScreenSpaceAmbientOcclusion },
                { CoreKeywordDescriptors.Lightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.AdditionalLights },
                { CoreKeywordDescriptors.AdditionalLightShadows },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                CoreKeywordDescriptors.AlphaTestOn,
                CoreKeywordDescriptors.AlphaPremultiplyOn,    // TODO: double check if this is needed
            };

            public static readonly KeywordCollection GBuffer = new KeywordCollection
            {
                { CoreKeywordDescriptors.Lightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.MixedLightingSubtractive },
                { GBufferNormalsOct },
                CoreKeywordDescriptors.AlphaTestOn,
                CoreKeywordDescriptors.AlphaPremultiplyOn,    // TODO: double check if this is needed
            };

            public static readonly KeywordCollection Meta = new KeywordCollection
            {
                // { CoreKeywordDescriptors.SmoothnessChannel },
                CoreKeywordDescriptors.AlphaTestOn,
            };

            public static readonly KeywordCollection _2D = new KeywordCollection
            {
                CoreKeywordDescriptors.AlphaTestOn,
            };

            public static readonly KeywordCollection DepthNormalsOnly = new KeywordCollection
            {
                CoreKeywordDescriptors.AlphaTestOn,
            };
        }
        #endregion

        #region Includes
        static class LitIncludes
        {
            const string kShadows = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl";
            const string kMetaInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl";
            const string kForwardPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl";
            const string kGBuffer = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl";
            const string kPBRGBufferPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl";
            const string kLightingMetaPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl";
            const string k2DPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl";

            public static readonly IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { kShadows, IncludeLocation.Pregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kForwardPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection GBuffer = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { kShadows, IncludeLocation.Pregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kGBuffer, IncludeLocation.Postgraph },
                { kPBRGBufferPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection Meta = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kMetaInput, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kLightingMetaPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection _2D = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { k2DPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
