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
            if (!context.HasCustomEditorForRenderPipeline(null))
                context.customEditorForRenderPipelines.Add((typeof(BuiltInLitGUI).FullName, ""));

            // Process SubShaders
            SubShaderDescriptor[] litSubShaders = { SubShaders.Lit };

            SubShaderDescriptor[] subShaders = litSubShaders;
            for (int i = 0; i < subShaders.Length; i++)
            {
                // Update Render State
                subShaders[i].renderType = target.renderType;
                subShaders[i].renderQueue = target.renderQueue;

                context.AddSubShader(subShaders[i]);
            }
        }

        public override void ProcessPreviewMaterial(Material material)
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

            // call the full unlit material setup function
            BuiltInLitGUI.UpdateMaterial(material);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);
            // Surface Type & Blend Mode
            // These must be set per SubTarget as Sprite SubTargets override them
            context.AddField(BuiltInFields.SurfaceOpaque,       target.surfaceType == SurfaceType.Opaque);
            context.AddField(BuiltInFields.SurfaceTransparent,  target.surfaceType != SurfaceType.Opaque);
            context.AddField(BuiltInFields.BlendAdd,            target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Additive);
            context.AddField(Fields.BlendAlpha,                 target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Alpha);
            context.AddField(BuiltInFields.BlendMultiply,       target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Multiply);
            context.AddField(BuiltInFields.BlendPremultiply,    target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Premultiply);

            // Lit
            context.AddField(BuiltInFields.NormalDropOffOS,     normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(BuiltInFields.NormalDropOffTS,     normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(BuiltInFields.NormalDropOffWS,     normalDropOffSpace == NormalDropOffSpace.World);
            context.AddField(BuiltInFields.SpecularSetup,       workflowMode == WorkflowMode.Specular);
            context.AddField(BuiltInFields.Normal,              descs.Contains(BlockFields.SurfaceDescription.NormalOS) ||
                descs.Contains(BlockFields.SurfaceDescription.NormalTS) ||
                descs.Contains(BlockFields.SurfaceDescription.NormalWS));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.NormalOS,           normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS,           normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS,           normalDropOffSpace == NormalDropOffSpace.World);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
            context.AddBlock(BlockFields.SurfaceDescription.Specular,           workflowMode == WorkflowMode.Specular);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic,           workflowMode == WorkflowMode.Metallic);

            // TODO: these should be predicated on transparency and alpha clip ONLY when those values are locked
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);                 // ,              target.surfaceType == SurfaceType.Transparent || target.alphaClip);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold);    //, target.alphaClip);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
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
            collector.AddFloatProperty(Property.QueueOffset(), 0.0f);
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
            public readonly static SubShaderDescriptor Lit = new SubShaderDescriptor()
            {
                //pipelineTag = BuiltInTarget.kPipelineTag,
                customTags = BuiltInTarget.kLitMaterialTypeTag,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { LitPasses.Forward },
                    { LitPasses.ForwardAdd },
                    { LitPasses.Deferred },
                    { CorePasses.ShadowCaster },
                    { CorePasses.DepthOnly },
                    { LitPasses.Meta },
                    { CorePasses.SceneSelection },
                    { CorePasses.ScenePicking },
                },
            };
        }
        #endregion

        #region Passes
        static class LitPasses
        {
            public static PassDescriptor Forward = new PassDescriptor
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
                renderStates = CoreRenderStates.Forward,
                pragmas  = CorePragmas.Forward,     // NOTE: SM 2.0 only GL
                defines = CoreDefines.BuiltInTargetAPI,
                keywords = LitKeywords.Forward,
                includes = LitIncludes.Forward,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            public static PassDescriptor ForwardAdd = new PassDescriptor
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
                renderStates = CoreRenderStates.ForwardAdd,
                pragmas  = CorePragmas.ForwardAdd,     // NOTE: SM 2.0 only GL
                defines = CoreDefines.BuiltInTargetAPI,
                keywords = LitKeywords.ForwardAdd,
                includes = LitIncludes.ForwardAdd,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            public static PassDescriptor ForwardOnly = new PassDescriptor
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
                renderStates = CoreRenderStates.Default,
                pragmas  = CorePragmas.Forward,    // NOTE: SM 2.0 only GL
                defines = CoreDefines.BuiltInTargetAPI,
                keywords = LitKeywords.Forward,
                includes = LitIncludes.Forward,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            public static PassDescriptor Deferred = new PassDescriptor
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
                renderStates = CoreRenderStates.Default,
                pragmas  = CorePragmas.Deferred,    // NOTE: SM 2.0 only GL
                defines = CoreDefines.BuiltInTargetAPI,
                keywords = LitKeywords.Deferred,
                includes = LitIncludes.Deferred,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            public static PassDescriptor Meta = new PassDescriptor()
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
                defines = CoreDefines.BuiltInTargetAPI,
                keywords = LitKeywords.Meta,
                includes = LitIncludes.Meta,

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
                StructFields.Varyings.viewDirectionWS,
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
                CoreKeywordDescriptors.AlphaClip,
                CoreKeywordDescriptors.AlphaTestOn,
                CoreKeywordDescriptors.SurfaceTypeTransparent,
                CoreKeywordDescriptors.AlphaPremultiplyOn,
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
                CoreKeywordDescriptors.AlphaClip,
                CoreKeywordDescriptors.AlphaTestOn,
                CoreKeywordDescriptors.SurfaceTypeTransparent,
            };

            public static readonly KeywordCollection Deferred = new KeywordCollection
            {
                { CoreKeywordDescriptors.Lightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.MixedLightingSubtractive },
                { GBufferNormalsOct },
                CoreKeywordDescriptors.AlphaClip,
                CoreKeywordDescriptors.AlphaTestOn,
                CoreKeywordDescriptors.SurfaceTypeTransparent,
                CoreKeywordDescriptors.AlphaPremultiplyOn,
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
                CoreKeywordDescriptors.AlphaClip,
                CoreKeywordDescriptors.AlphaTestOn,
                CoreKeywordDescriptors.SurfaceTypeTransparent,
            };

            public static readonly KeywordCollection Meta = new KeywordCollection
            {
                { CoreKeywordDescriptors.SmoothnessChannel },
                CoreKeywordDescriptors.AlphaClip,
                CoreKeywordDescriptors.AlphaTestOn,
                CoreKeywordDescriptors.SurfaceTypeTransparent,
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
