using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    sealed class BuiltInLitSubTarget : BuiltInSubTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("8c2d5b55aa47443878a55a05f4294270"); // BuiltInLitSubTarget.cs

        [SerializeField]
        WorkflowMode m_WorkflowMode = WorkflowMode.Metallic;

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace = NormalDropOffSpace.Tangent;

        public BuiltInLitSubTarget()
        {
            displayName = "Lit";
        }

        protected override ShaderID shaderID => ShaderID.SG_Lit;

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

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);

            if (!context.HasCustomEditorForRenderPipeline("")  && String.IsNullOrEmpty(target.customEditorGUI))
                context.AddCustomEditorForRenderPipeline(typeof(BuiltInLitGUI).FullName, "");

            // Process SubShaders
            context.AddSubShader(SubShaders.Lit(target, target.renderType, target.renderQueue));
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.allowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                material.SetFloat(Property.SpecularWorkflowMode(), (float)workflowMode);
                material.SetFloat(Property.Surface(), (float)target.surfaceType);
                material.SetFloat(Property.Blend(), (float)target.alphaMode);
                material.SetFloat(Property.AlphaClip(), target.alphaClip ? 1.0f : 0.0f);
                material.SetFloat(Property.Cull(), (int)target.renderFace);
                material.SetFloat(Property.ZWriteControl(), (float)target.zWriteControl);
                material.SetFloat(Property.ZTest(), (float)target.zTestMode);
            }

            // We always need these properties regardless of whether the material is allowed to override
            // Queue control & offset enable correct automatic render queue behavior
            // Control == 0 is automatic, 1 is user-specified render queue
            material.SetFloat(Property.QueueOffset(), 0.0f);
            material.SetFloat(Property.QueueControl(), (float)BuiltInBaseShaderGUI.QueueControl.Auto);

            // call the full unlit material setup function
            BuiltInLitGUI.UpdateMaterial(material);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);

            // Lit
            context.AddField(BuiltInFields.NormalDropOffOS, normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(BuiltInFields.NormalDropOffTS, normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(BuiltInFields.NormalDropOffWS, normalDropOffSpace == NormalDropOffSpace.World);
            context.AddField(BuiltInFields.SpecularSetup, workflowMode == WorkflowMode.Specular);
            context.AddField(BuiltInFields.Normal, descs.Contains(BlockFields.SurfaceDescription.NormalOS) ||
                descs.Contains(BlockFields.SurfaceDescription.NormalTS) ||
                descs.Contains(BlockFields.SurfaceDescription.NormalWS));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.NormalOS, normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS, normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS, normalDropOffSpace == NormalDropOffSpace.World);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);

            // when the surface options are material controlled, we must show all of these blocks
            // when target controlled, we can cull the unnecessary blocks
            // NOTE: Specular workflow is not supported so we need to not have this check now,
            // otherwise allowMaterialOverride will show a block that does nothing.
            //context.AddBlock(BlockFields.SurfaceDescription.Specular, (workflowMode == WorkflowMode.Specular) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic, (workflowMode == WorkflowMode.Metallic) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha, (target.surfaceType == SurfaceType.Transparent || target.alphaClip) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, (target.alphaClip) || target.allowMaterialOverride);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (target.allowMaterialOverride)
            {
                base.CollectShaderProperties(collector, generationMode);

                // setup properties using the defaults
                collector.AddFloatProperty(Property.Surface(), (float)target.surfaceType);
                collector.AddFloatProperty(Property.Blend(), (float)target.alphaMode);
                collector.AddFloatProperty(Property.AlphaClip(), target.alphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.SrcBlend(), 1.0f);    // always set by material inspector (TODO : get src/dst blend and set here?)
                collector.AddFloatProperty(Property.DstBlend(), 0.0f);    // always set by material inspector
                collector.AddFloatProperty(Property.ZWrite(), (target.surfaceType == SurfaceType.Opaque) ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.ZWriteControl(), (float)target.zWriteControl);
                collector.AddFloatProperty(Property.ZTest(), (float)target.zTestMode);    // ztest mode is designed to directly pass as ztest
                collector.AddFloatProperty(Property.Cull(), (float)target.renderFace);    // render face enum is designed to directly pass as a cull mode
            }

            // We always need these properties regardless of whether the material is allowed to override other shader properties.
            // Queue control & offset enable correct automatic render queue behavior.  Control == 0 is automatic, 1 is user-specified.
            // We initialize queue control to -1 to indicate to UpdateMaterial that it needs to initialize it properly on the material.
            collector.AddFloatProperty(Property.QueueOffset(), 0.0f);
            collector.AddFloatProperty(Property.QueueControl(), -1.0f);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            // Temporarily remove the workflow mode until specular is supported
            //context.AddProperty("Workflow", new EnumField(WorkflowMode.Metallic) { value = workflowMode }, (evt) =>
            //{
            //    if (Equals(workflowMode, evt.newValue))
            //        return;

            //    registerUndo("Change Workflow");
            //    workflowMode = (WorkflowMode)evt.newValue;
            //    onChange();
            //});

            // show the target default surface properties
            var builtInTarget = (target as BuiltInTarget);
            builtInTarget?.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);
            builtInTarget?.GetDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo);

            context.AddProperty("Fragment Normal Space", new EnumField(NormalDropOffSpace.Tangent) { value = normalDropOffSpace }, (evt) =>
            {
                if (Equals(normalDropOffSpace, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
                onChange();
            });
        }

        #region SubShader
        static class SubShaders
        {
            // Overloads to do inline PassDescriptor modifications
            // NOTE: param order should match PassDescriptor field order for consistency
            #region PassVariant
            private static PassDescriptor PassVariant(in PassDescriptor source, PragmaCollection pragmas)
            {
                var result = source;
                result.pragmas = pragmas;
                return result;
            }

            private static PassDescriptor PassVariant(in PassDescriptor source, BlockFieldDescriptor[] vertexBlocks, BlockFieldDescriptor[] pixelBlocks, PragmaCollection pragmas, DefineCollection defines)
            {
                var result = source;
                result.validVertexBlocks = vertexBlocks;
                result.validPixelBlocks = pixelBlocks;
                result.pragmas = pragmas;
                result.defines = defines;
                return result;
            }

            #endregion

            // SM 2.0
            public static SubShaderDescriptor Lit(BuiltInTarget target, string renderType, string renderQueue)
            {
                var result = new SubShaderDescriptor()
                {
                    //pipelineTag = BuiltInTarget.kPipelineTag,
                    customTags = BuiltInTarget.kLitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection(),
                };

                result.passes.Add(LitPasses.Forward(target));
                result.passes.Add(LitPasses.ForwardAdd(target));
                result.passes.Add(LitPasses.Deferred(target));
                result.passes.Add(CorePasses.ShadowCaster(target));

                if (target.mayWriteDepth)
                    result.passes.Add(CorePasses.DepthOnly(target));

                result.passes.Add(LitPasses.Meta(target));
                result.passes.Add(CorePasses.SceneSelection(target));
                result.passes.Add(CorePasses.ScenePicking(target));

                return result;
            }
        }
        #endregion

        #region Passes
        static class LitPasses
        {
            public static PassDescriptor Forward(BuiltInTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "BuiltIn Forward",
                    referenceName = "SHADERPASS_FORWARD",
                    lightMode = "ForwardBase",
                    useInPreview = true,

                    // Template
                    passTemplatePath = BuiltInTarget.kTemplatePath,
                    sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Default(target),
                    pragmas = CorePragmas.Forward,     // NOTE: SM 2.0 only GL
                    defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
                    keywords = new KeywordCollection { LitKeywords.Forward },
                    includes = LitIncludes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
                AddForwardSurfaceControlsToPass(ref result, target);
                return result;
            }

            public static PassDescriptor ForwardAdd(BuiltInTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "BuiltIn ForwardAdd",
                    referenceName = "SHADERPASS_FORWARD_ADD",
                    lightMode = "ForwardAdd",
                    useInPreview = true,

                    // Template
                    passTemplatePath = BuiltInTarget.kTemplatePath,
                    sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.ForwardAdd(target),
                    pragmas = CorePragmas.ForwardAdd,     // NOTE: SM 2.0 only GL
                    defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
                    keywords = new KeywordCollection { LitKeywords.ForwardAdd },
                    includes = LitIncludes.ForwardAdd,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                AddForwardAddControlsToPass(ref result, target);
                return result;
            }

            public static PassDescriptor ForwardOnly(BuiltInTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "BuiltIn Forward Only",
                    referenceName = "SHADERPASS_FORWARDONLY",
                    lightMode = "BuiltInForwardOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = BuiltInTarget.kTemplatePath,
                    sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Default(target),
                    pragmas = CorePragmas.Forward,    // NOTE: SM 2.0 only GL
                    defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
                    keywords = new KeywordCollection { LitKeywords.Forward },
                    includes = LitIncludes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
                AddForwardOnlyControlsToPass(ref result, target);
                return result;
            }

            public static PassDescriptor Deferred(BuiltInTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "BuiltIn Deferred",
                    referenceName = "SHADERPASS_DEFERRED",
                    lightMode = "Deferred",
                    useInPreview = true,

                    // Template
                    passTemplatePath = BuiltInTarget.kTemplatePath,
                    sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Default(target),
                    pragmas = CorePragmas.Deferred,    // NOTE: SM 2.0 only GL
                    defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
                    keywords = new KeywordCollection { LitKeywords.Deferred },
                    includes = LitIncludes.Deferred,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
                AddDeferredControlsToPass(ref result, target);
                return result;
            }

            public static PassDescriptor Meta(BuiltInTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Meta",
                    referenceName = "SHADERPASS_META",
                    lightMode = "Meta",

                    // Template
                    passTemplatePath = BuiltInTarget.kTemplatePath,
                    sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

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
                    defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
                    keywords = new KeywordCollection { LitKeywords.Meta },
                    includes = LitIncludes.Meta,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                AddMetaControlsToPass(ref result, target);
                return result;
            }

            internal static void AddForwardSurfaceControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
            {
                CorePasses.AddTargetSurfaceControlsToPass(ref pass, target);
            }

            internal static void AddForwardAddControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
            {
                CorePasses.AddSurfaceTypeControlToPass(ref pass, target);
                CorePasses.AddAlphaClipControlToPass(ref pass, target);
            }

            internal static void AddForwardOnlyControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
            {
                CorePasses.AddTargetSurfaceControlsToPass(ref pass, target);
            }

            internal static void AddDeferredControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
            {
                CorePasses.AddTargetSurfaceControlsToPass(ref pass, target);
            }

            internal static void AddMetaControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
            {
                CorePasses.AddSurfaceTypeControlToPass(ref pass, target);
                CorePasses.AddAlphaClipControlToPass(ref pass, target);
            }
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
                BuiltInStructFields.Varyings.lightmapUV,
                BuiltInStructFields.Varyings.sh,
                BuiltInStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                BuiltInStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static readonly FieldCollection GBuffer = new FieldCollection()
            {
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                BuiltInStructFields.Varyings.lightmapUV,
                BuiltInStructFields.Varyings.sh,
                BuiltInStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                BuiltInStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
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

        #endregion

        #region Keywords
        static class LitKeywords
        {
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
            };

            public static readonly KeywordCollection ForwardAdd = new KeywordCollection
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
            };

            public static readonly KeywordCollection Deferred = new KeywordCollection
            {
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.MixedLightingSubtractive },
                { GBufferNormalsOct },
            };

            public static readonly KeywordCollection Meta = new KeywordCollection
            {
                { CoreKeywordDescriptors.SmoothnessChannel },
            };
        }
        #endregion

        #region Includes
        static class LitIncludes
        {
            const string kShadows = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shadows.hlsl";
            const string kMetaInput = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/MetaInput.hlsl";
            const string kForwardPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl";
            const string kForwardAddPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRForwardAddPass.hlsl";
            const string kDeferredPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRDeferredPass.hlsl";
            const string kGBuffer = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/UnityGBuffer.hlsl";
            const string kPBRGBufferPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl";
            const string kLightingMetaPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl";

            public static readonly IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kForwardPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection ForwardAdd = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kForwardAddPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection Deferred = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kDeferredPass, IncludeLocation.Postgraph },
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

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kLightingMetaPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
