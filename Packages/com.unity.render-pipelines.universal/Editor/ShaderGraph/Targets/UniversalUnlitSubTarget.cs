using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;
using UnityEngine.UIElements;
using static UnityEditor.Rendering.Universal.ShaderGraph.SubShaderUtils;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalUnlitSubTarget : UniversalSubTarget, ILegacyTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("97c3f7dcb477ec842aa878573640313a"); // UniversalUnlitSubTarget.cs

        public override int latestVersion => 2;

        [SerializeField]
        bool m_KeepLightingVariants = false;

        [SerializeField]
        bool m_DefaultDecalBlending = true;

        [SerializeField]
        bool m_DefaultSSAO = true;

        [SerializeField]
        bool m_ScreenSpaceReflectionsContributeTransparent = true;

        [SerializeField]
        bool m_WritesColor = true;

        public bool keepLightingVariants
        {
            get => m_KeepLightingVariants;
            set => m_KeepLightingVariants = value;
        }

        public bool defaultDecalBlending
        {
            get => m_DefaultDecalBlending;
            set => m_DefaultDecalBlending = value;
        }

        public bool defaultSSAO
        {
            get => m_DefaultSSAO;
            set => m_DefaultSSAO = value;
        }

        public bool screenSpaceReflectionsContributeTransparent
        {
            get => m_ScreenSpaceReflectionsContributeTransparent;
            set => m_ScreenSpaceReflectionsContributeTransparent = value;
        }

        public bool writesColor
        {
            get => m_WritesColor;
            set => m_WritesColor = value;
        }

        public UniversalUnlitSubTarget()
        {
            displayName = "Unlit";
        }

        protected override ShaderID shaderID => ShaderID.SG_Unlit;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);

            var universalRPType = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset);
            if (!context.HasCustomEditorForRenderPipeline(universalRPType))
            {
                var gui = typeof(ShaderGraphUnlitGUI);
#if HAS_VFX_GRAPH
                if (TargetsVFX())
                    gui = typeof(VFXShaderGraphUnlitGUI);
#endif
                context.AddCustomEditorForRenderPipeline(gui.FullName, universalRPType);
            }
            // Process SubShaders
            context.AddSubShader(PostProcessSubShader(SubShaders.Unlit(target, target.renderType, target.renderQueue, target.disableBatching)));
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.allowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                material.SetFloat(Property.SurfaceType, (float)target.surfaceType);
                material.SetFloat(Property.BlendMode, (float)target.alphaMode);
                material.SetFloat(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                material.SetFloat(Property.CullMode, (int)target.renderFace);
                material.SetFloat(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.ZWriteControl, (float)target.zWriteControl);
                material.SetFloat(Property.ZTest, (float)target.zTestMode);
            }

            // We always need these properties regardless of whether the material is allowed to override
            // Queue control & offset enable correct automatic render queue behavior
            // Control == 0 is automatic, 1 is user-specified render queue
            material.SetFloat(Property.QueueOffset, 0.0f);
            material.SetFloat(Property.QueueControl, (float)BaseShaderGUI.QueueControl.Auto);

            if (IsSpacewarpSupported())
            {
                material.SetFloat(Property.XrMotionVectorsPass, 1.0f);
            }

            // call the full unlit material setup function
            ShaderGraphUnlitGUI.UpdateMaterial(material, MaterialUpdateType.CreatedNewMaterial);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);
        }

        internal override bool supportsStencilOverride => true;

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            if (writesColor)
                context.AddBlock(UniversalBlockFields.VertexDescription.MotionVector, target.additionalMotionVectorMode == AdditionalMotionVectorMode.Custom);

            bool showAlpha = !writesColor
                ? target.alphaClip || target.allowMaterialOverride
                : (target.surfaceType == SurfaceType.Transparent || target.alphaClip) || target.allowMaterialOverride;

            context.AddBlock(BlockFields.SurfaceDescription.Alpha, showAlpha);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, target.alphaClip || target.allowMaterialOverride);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (target.allowMaterialOverride)
            {
                collector.AddFloatProperty(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.SurfaceType, (float)target.surfaceType);
                collector.AddFloatProperty(Property.BlendMode, (float)target.alphaMode);
                collector.AddFloatProperty(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.SrcBlend, 1.0f);    // always set by material inspector
                collector.AddFloatProperty(Property.DstBlend, 0.0f);    // always set by material inspector
                collector.AddFloatProperty(Property.SrcBlendAlpha, 1.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddFloatProperty(Property.DstBlendAlpha, 0.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddToggleProperty(Property.ZWrite, (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.ZWriteControl, (float)target.zWriteControl);
                collector.AddFloatProperty(Property.ZTest, (float)target.zTestMode);
                collector.AddFloatProperty(Property.CullMode, (float)target.renderFace);    // render face enum is designed to directly pass as a cull mode
                if (target.overrideStencilState)
                {
                    collector.AddFloatProperty(Property.StencilRef, target.stencilReference);
                    collector.AddFloatProperty(Property.StencilReadMask, target.stencilReadMask);
                    collector.AddFloatProperty(Property.StencilWriteMask, target.stencilWriteMask);
                    collector.AddFloatProperty(Property.StencilCompFunc, (float)target.stencilCompareFunction);
                    collector.AddFloatProperty(Property.StencilPassOp, (float)target.stencilPassOperation);
                    collector.AddFloatProperty(Property.StencilFailOp, (float)target.stencilFailOperation);
                    collector.AddFloatProperty(Property.StencilZFailOp, (float)target.stencilZFailOperation);
                    collector.AddFloatProperty(Property.StencilCompFuncBack, (float)target.stencilCompareFunctionBack);
                    collector.AddFloatProperty(Property.StencilPassOpBack, (float)target.stencilPassOperationBack);
                    collector.AddFloatProperty(Property.StencilFailOpBack, (float)target.stencilFailOperationBack);
                    collector.AddFloatProperty(Property.StencilZFailOpBack, (float)target.stencilZFailOperationBack);
                    UniversalLitSubTarget.AddStencilDefaultProperties(collector);
                }

                bool enableAlphaToMask = (target.alphaClip && (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.AlphaToMask, enableAlphaToMask ? 1.0f : 0.0f);
            }

            // Always emit _Cull so the engine can detect two-pass rendering (BackToFront/FrontToBack)
            if (!target.allowMaterialOverride)
                collector.AddFloatProperty(Property.CullMode, (float)target.renderFace);

            collector.AddFloatProperty(Property.WritesColor, writesColor ? 1.0f : 0.0f);

            // Marker for the material UI: this shader bakes stencil writes into the ShadowCaster pass.
            // BaseShaderGUI uses this to show a warning when the URP renderer's Shadowmap Stencil is off.
            if (target.overrideStencilState && target.depthStencilPassMask.Has(DepthStencilPassMask.ShadowPass))
                collector.AddFloatProperty(Property.StencilUsesShadowPass, 1.0f);

            // Material-UI marker (Property.StencilUsesPrepass); condition mirrors needsDepthOnlyPass.
            if ((target.overrideStencilState || target.allowMaterialOverride) && target.depthStencilPassMask.Has(DepthStencilPassMask.Prepass))
                collector.AddFloatProperty(Property.StencilUsesPrepass, 1.0f);

            // We always need these properties regardless of whether the material is allowed to override other shader properties.
            // Queue control & offset enable correct automatic render queue behavior.  Control == 0 is automatic, 1 is user-specified.
            // We initialize queue control to -1 to indicate to UpdateMaterial that it needs to initialize it properly on the material.
            collector.AddFloatProperty(Property.QueueOffset, 0.0f);
            collector.AddFloatProperty(Property.QueueControl, -1.0f);

            // Emitted unconditionally so UpdateScreenSpaceReflectionContributeTransparentPassState
            // can read the sub-target default at material-update time to configure the runtime
            // pass state.
            collector.AddToggleProperty(Property.ScreenSpaceReflectionsContributeTransparent, screenSpaceReflectionsContributeTransparent);

            if (IsSpacewarpSupported())
            {
                collector.AddFloatProperty(Property.XrMotionVectorsPass, 1.0f);
            }
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var universalTarget = (target as UniversalTarget);
            universalTarget.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);

            context.AddProperty("Write Color", "When disabled, the shader does not write color, but may still write to depth / stencil buffers.", 0, new Toggle() { value = writesColor }, (evt) =>
            {
                if (Equals(writesColor, evt.newValue))
                    return;

                registerUndo("Change Write Color");
                writesColor = evt.newValue;
                if (!writesColor)
                    universalTarget.surfaceType = SurfaceType.Opaque;
                onChange();
            });

            universalTarget.AddDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo, showReceiveShadows: false, writesColor: writesColor);

            if (writesColor)
            {
                // Unlit option to add additional realtime lighting variants. Useful for custom lighting.
                context.AddProperty("Keep Lighting Variants", new Toggle() { value = keepLightingVariants }, (evt) =>
                {
                    if (Equals(keepLightingVariants, evt.newValue))
                        return;

                    registerUndo("Change Keep Lighting Variants");
                    keepLightingVariants = evt.newValue;
                    onChange();
                });

                // Unlit option to disable default decal blending behaviour.
                context.AddProperty("Default Decal Blending", new Toggle() { value = defaultDecalBlending }, (evt) =>
                {
                    if (Equals(defaultDecalBlending, evt.newValue))
                        return;

                    registerUndo("Change Default Decal Blending");
                    defaultDecalBlending = evt.newValue;
                    onChange();
                });

                // Unlit option to disable default SSAO behaviour.
                context.AddProperty("Default SSAO", new Toggle() { value = defaultSSAO }, (evt) =>
                {
                    if (Equals(defaultSSAO, evt.newValue))
                        return;

                    registerUndo("Change Default SSAO");
                    defaultSSAO = evt.newValue;
                    onChange();
                });

                if (target.surfaceType == SurfaceType.Transparent)
                {
                    context.AddProperty("Screen Space Reflections Contribute Transparent", new Toggle() { value = screenSpaceReflectionsContributeTransparent }, (evt) =>
                    {
                        if (Equals(screenSpaceReflectionsContributeTransparent, evt.newValue))
                            return;

                        registerUndo("Change Screen Space Reflections Contribute Transparent");
                        screenSpaceReflectionsContributeTransparent = evt.newValue;
                        onChange();
                    });
                }
            }
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if (!(masterNode is UnlitMasterNode1 unlitMasterNode))
                return false;

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 9 },
                { BlockFields.VertexDescription.Normal, 10 },
                { BlockFields.VertexDescription.Tangent, 11 },
                { BlockFields.SurfaceDescription.BaseColor, 0 },
                { BlockFields.SurfaceDescription.Alpha, 7 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 8 },
            };

            return true;
        }

        internal override void OnAfterParentTargetDeserialized()
        {
            Assert.IsNotNull(target);

            if (this.sgVersion < latestVersion)
            {
                // Upgrade old incorrect Premultiplied blend (with alpha multiply in shader) into
                // equivalent Alpha blend mode for backwards compatibility.
                if (this.sgVersion < 1)
                {
                    if (target.alphaMode == AlphaMode.Premultiply)
                    {
                        target.alphaMode = AlphaMode.Alpha;
                    }
                }
                ChangeVersion(latestVersion);
            }
        }

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor Unlit(UniversalTarget target, string renderType, string renderQueue, string disableBatchingTag)
            {
                var unlitSubTarget = target.activeSubTarget as UniversalUnlitSubTarget;
                bool writesColor = unlitSubTarget?.writesColor ?? true;

                var result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kUnlitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    disableBatchingTag = disableBatchingTag,
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                if (writesColor)
                    result.passes.Add(UnlitPasses.Forward(target, UnlitKeywords.Forward));
                else
                    result.passes.Add(UnlitPasses.ForwardNoColor(target));

                if (target.needsDepthOnlyPass)
                    result.passes.Add(PassVariant(CorePasses.DepthOnly(target), CorePragmas.Instanced));

                if (target.alwaysRenderMotionVectors)
                    result.customTags = string.Concat(result.customTags, " ", UniversalTarget.kAlwaysRenderMotionVectorsTag);
                result.passes.Add(PassVariant(CorePasses.MotionVectors(target), CorePragmas.MotionVectors));

                if (IsSpacewarpSupported())
                    result.passes.Add(PassVariant(CorePasses.XRMotionVectors(target), CorePragmas.XRMotionVectors));

                result.passes.Add(PassVariant(UnlitPasses.DepthNormalOnly(target), CorePragmas.Instanced));

                if (target.castShadows || target.allowMaterialOverride)
                    result.passes.Add(PassVariant(CorePasses.ShadowCaster(target), CorePragmas.Instanced));

                if (writesColor)
                {
                    // Fill GBuffer with color and normal for custom GBuffer use cases.
                    result.passes.Add(UnlitPasses.GBuffer(target));
                }

                // Currently neither of these passes (selection/picking) can be last for the game view for
                // UI shaders to render correctly. Verify [1352225] before changing this order.
                result.passes.Add(PassVariant(CorePasses.SceneSelection(target), CorePragmas.Instanced));
                result.passes.Add(PassVariant(CorePasses.ScenePicking(target), CorePragmas.Instanced));

                return result;
            }
        }
        #endregion

        #region Passes
        static class UnlitPasses
        {
            internal static void AddLightingVariantsControlToPass(ref PassDescriptor pass, UniversalUnlitSubTarget unlitSubTarget)
            {
                if (unlitSubTarget.keepLightingVariants)
                {
                    pass.includes.Add(UnlitIncludes.LightingIncludes);
                    pass.keywords.Add(UnlitKeywords.LightingVariants);
                    pass.defines.Add(UnlitDefines.LightingDefine, 1);
                    pass.lightMode = "UniversalForward";
                }
            }

            internal static void AddDefaultDecalBlendingControlToPass(ref PassDescriptor pass, UniversalUnlitSubTarget unlitSubTarget)
            {
                if (unlitSubTarget.defaultDecalBlending)
                {
                    pass.defines.Add(UnlitDefines.DefaultDecalBlendingDefine, 1);
                }
            }

            internal static void AddDefaultSSAOControlToPass(ref PassDescriptor pass, UniversalUnlitSubTarget unlitSubTarget)
            {
                if (unlitSubTarget.defaultSSAO)
                {
                    pass.defines.Add(UnlitDefines.DefaultSSAODefine, 1);
                }
            }

            public static PassDescriptor ForwardNoColor(UniversalTarget target)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Unlit",
                    referenceName = "SHADERPASS_UNLIT",
                    lightMode = "UniversalForwardOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = UnlitRequiredFields.Unlit,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.ForwardNoColor(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = new IncludeCollection { CoreIncludes.DepthOnly },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);
                bool colorOn = target.depthStencilPassMask.Has(DepthStencilPassMask.ColorPass);
                CorePasses.AddDepthStateControlToPass(ref result, target, useDefaults: !colorOn);
                CorePasses.AddStencilStateControlToPass(ref result, target, useDefaults: !colorOn);

                return result;
            }

            public static PassDescriptor Forward(UniversalTarget target, KeywordCollection keywords)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Unlit",
                    referenceName = "SHADERPASS_UNLIT",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = UnlitRequiredFields.Unlit,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = CorePragmas.Forward,
                    defines = new DefineCollection { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection { keywords },
                    includes = new IncludeCollection { UnlitIncludes.Forward },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                CorePasses.AddAlphaToMaskControlToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);
                bool colorOn = target.depthStencilPassMask.Has(DepthStencilPassMask.ColorPass);
                CorePasses.AddDepthStateControlToPass(ref result, target, useDefaults: !colorOn);
                CorePasses.AddStencilStateControlToPass(ref result, target, useDefaults: !colorOn);

                if (target.activeSubTarget is UniversalUnlitSubTarget unlitSubTarget)
                {
                    UnlitPasses.AddLightingVariantsControlToPass(ref result, unlitSubTarget);
                    UnlitPasses.AddDefaultDecalBlendingControlToPass(ref result, unlitSubTarget);
                    UnlitPasses.AddDefaultSSAOControlToPass(ref result, unlitSubTarget);
                }

                return result;
            }

            public static PassDescriptor DepthNormalOnly(UniversalTarget target)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "DepthNormalsOnly",
                    referenceName = "SHADERPASS_DEPTHNORMALSONLY",
                    lightMode = "DepthNormalsOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = UnlitBlockMasks.FragmentDepthNormals,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = UnlitRequiredFields.DepthNormalsOnly,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthNormalsOnly(target),
                    pragmas = CorePragmas.Forward,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection { CoreKeywordDescriptors.GBufferNormalsOct },
                    includes = new IncludeCollection { CoreIncludes.DepthNormalsOnly },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);
                CorePasses.AddStencilStateControlToPass(ref result, target,
                    useDefaults: !target.depthStencilPassMask.Has(DepthStencilPassMask.Prepass));

                return result;
            }

            // Deferred only in SM4.5
            // GBuffer fill for consistency.
            public static PassDescriptor GBuffer(UniversalTarget target)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "GBuffer",
                    referenceName = "SHADERPASS_GBUFFER",
                    lightMode = "UniversalGBuffer",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = UnlitRequiredFields.GBuffer,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = CorePragmas.GBuffer,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection { UnlitKeywords.GBuffer },
                    includes = new IncludeCollection { UnlitIncludes.GBuffer },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);
                CorePasses.AddDepthStateControlToPass(ref result, target, useDefaults: !target.depthStencilPassMask.Has(DepthStencilPassMask.ColorPass));

                if (target.activeSubTarget is UniversalUnlitSubTarget unlitSubTarget)
                {
                    UnlitPasses.AddDefaultDecalBlendingControlToPass(ref result, unlitSubTarget);
                    UnlitPasses.AddDefaultSSAOControlToPass(ref result, unlitSubTarget);
                }

                return result;
            }

            #region PortMasks
            static class UnlitBlockMasks
            {
                public static readonly BlockFieldDescriptor[] FragmentDepthNormals = new BlockFieldDescriptor[]
                {
                    BlockFields.SurfaceDescription.NormalWS,
                    BlockFields.SurfaceDescription.Alpha,
                    BlockFields.SurfaceDescription.AlphaClipThreshold,
                };
            }
            #endregion

            #region RequiredFields
            static class UnlitRequiredFields
            {
                public static readonly FieldCollection Unlit = new FieldCollection()
                {
                    StructFields.Varyings.positionWS,
                    StructFields.Varyings.normalWS
                };

                public static readonly FieldCollection DepthNormalsOnly = new FieldCollection()
                {
                    StructFields.Varyings.normalWS,
                };

                public static readonly FieldCollection GBuffer = new FieldCollection()
                {
                    StructFields.Varyings.positionWS,
                    StructFields.Varyings.normalWS,
                    UniversalStructFields.Varyings.sh,   // Satisfy !LIGHTMAP_ON requirements.
                    UniversalStructFields.Varyings.probeOcclusion,
                };
            }
            #endregion
        }
        #endregion

        #region Defines
        static class UnlitDefines
        {
            public static readonly KeywordDescriptor LightingDefine = new KeywordDescriptor()
            {
                displayName = "Keep Lighting Variants",
                referenceName = "UNLIT_REALTIME_LIGHTING",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Vertex | KeywordShaderStage.Fragment
            };

            public static readonly KeywordDescriptor DefaultDecalBlendingDefine = new KeywordDescriptor()
            {
                displayName = "Default Decal Blending",
                referenceName = "UNLIT_DEFAULT_DECAL_BLENDING",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment
            };

            public static readonly KeywordDescriptor DefaultSSAODefine = new KeywordDescriptor()
            {
                displayName = "Default SSAO",
                referenceName = "UNLIT_DEFAULT_SSAO",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment
            };
        }
        #endregion

        #region Keywords
        static class UnlitKeywords
        {
            public static readonly KeywordCollection Forward = new KeywordCollection()
            {
                // This contains lightmaps because without a proper custom lighting solution in Shadergraph,
                // people start with the unlit then add lightmapping nodes to it.
                // If we removed lightmaps from the unlit target this would ruin a lot of peoples days.
                CoreKeywordDescriptors.StaticLightmap,
                CoreKeywordDescriptors.DirectionalLightmapCombined,
                CoreKeywordDescriptors.UseLegacyLightmaps,
                CoreKeywordDescriptors.LightmapBicubicSampling,
                CoreKeywordDescriptors.DBuffer,
                CoreKeywordDescriptors.DebugDisplay,
                CoreKeywordDescriptors.ScreenSpaceAmbientOcclusion,
            };

            public static readonly KeywordCollection GBuffer = new KeywordCollection
            {
                CoreKeywordDescriptors.DBuffer,
                CoreKeywordDescriptors.ScreenSpaceAmbientOcclusion,
                CoreKeywordDescriptors.RenderPassEnabled,
                CoreKeywordDescriptors.GBufferNormalsOct,
                CoreKeywordDescriptors.ShadowsShadowmask
            };

            public static readonly KeywordCollection LightingVariants = new KeywordCollection()
            {
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.AdditionalLights },
                { CoreKeywordDescriptors.AdditionalLightShadows },
                { CoreKeywordDescriptors.ReflectionProbeBlending },
                { CoreKeywordDescriptors.ReflectionProbeBoxProjection },
                { CoreKeywordDescriptors.ReflectionProbeAtlas },
                { CoreKeywordDescriptors.ReflectionProbeRotation },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { CoreKeywordDescriptors.LightLayers },
                { CoreKeywordDescriptors.LightCookies },
                { CoreKeywordDescriptors.VolumetricFog },
                { CoreKeywordDescriptors.ClusterLightLoop },
            };
        }
        #endregion

        #region Includes
        static class UnlitIncludes
        {
            const string kUnlitPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl";
            const string kUnlitGBufferPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitGBufferPass.hlsl";
            const string kLighting = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl";

            public static IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.DOTSPregraph },
                { CoreIncludes.FogPregraph },
                { CoreIncludes.WriteRenderLayersPregraph },
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },
                { CoreIncludes.WriteRenderLayersPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kUnlitPass, IncludeLocation.Postgraph },
            };

            public static IncludeCollection GBuffer = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.DOTSPregraph },
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },
                { CoreIncludes.WriteRenderLayersPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kUnlitGBufferPass, IncludeLocation.Postgraph },
                { CoreIncludes.GBufferOutputFormat },
            };

            public static IncludeCollection LightingIncludes = new IncludeCollection
            {
                // Pre-graph
                { kLighting, IncludeLocation.Pregraph },
            };
        }
        #endregion
    }
}
