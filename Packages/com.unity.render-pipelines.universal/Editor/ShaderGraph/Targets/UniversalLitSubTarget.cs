using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;
using UnityEngine.Assertions;
using static UnityEditor.Rendering.Universal.ShaderGraph.SubShaderUtils;
using UnityEngine.Rendering.Universal;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalLitSubTarget : UniversalSubTarget, ILegacyTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("d6c78107b64145745805d963de80cc17"); // UniversalLitSubTarget.cs

        public override int latestVersion => 2;

        [SerializeField]
        WorkflowMode m_WorkflowMode = WorkflowMode.Metallic;

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace = NormalDropOffSpace.Tangent;

        [SerializeField]
        bool m_ClearCoat = false;

        [SerializeField]
        bool m_BlendModePreserveSpecular = true;

        public UniversalLitSubTarget()
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

        public bool blendModePreserveSpecular
        {
            get => m_BlendModePreserveSpecular;
            set => m_BlendModePreserveSpecular = value;
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);

            var universalRPType = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset);
            if (!context.HasCustomEditorForRenderPipeline(universalRPType))
            {
                var gui = typeof(ShaderGraphLitGUI);
#if HAS_VFX_GRAPH
                if (TargetsVFX())
                    gui = typeof(VFXShaderGraphLitGUI);
#endif
                context.AddCustomEditorForRenderPipeline(gui.FullName, universalRPType);
            }

            // Process SubShaders
            context.AddSubShader(PostProcessSubShader(SubShaders.LitSubShader(target, workflowMode, target.renderType, target.renderQueue, target.disableBatching, complexLit, blendModePreserveSpecular)));
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.allowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                material.SetFloat(Property.SpecularWorkflowMode, (float)workflowMode);
                material.SetFloat(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.ReceiveShadows, target.receiveShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.SurfaceType, (float)target.surfaceType);
                material.SetFloat(Property.BlendMode, (float)target.alphaMode);
                material.SetFloat(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                material.SetFloat(Property.CullMode, (int)target.renderFace);
                material.SetFloat(Property.ZWriteControl, (float)target.zWriteControl);
                material.SetFloat(Property.ZTest, (float)target.zTestMode);
            }

            // We always need these properties regardless of whether the material is allowed to override
            // Queue control & offset enable correct automatic render queue behavior
            // Control == 0 is automatic, 1 is user-specified render queue
            material.SetFloat(Property.QueueOffset, 0.0f);
            material.SetFloat(Property.QueueControl, (float)BaseShaderGUI.QueueControl.Auto);

            // call the full unlit material setup function
            ShaderGraphLitGUI.UpdateMaterial(material, MaterialUpdateType.CreatedNewMaterial);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            var descs = context.blocks.Select(x => x.descriptor);

            // Lit -- always controlled by subtarget
            context.AddField(UniversalFields.NormalDropOffOS, normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(UniversalFields.NormalDropOffTS, normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(UniversalFields.NormalDropOffWS, normalDropOffSpace == NormalDropOffSpace.World);
            context.AddField(UniversalFields.Normal, descs.Contains(BlockFields.SurfaceDescription.NormalOS) ||
                descs.Contains(BlockFields.SurfaceDescription.NormalTS) ||
                descs.Contains(BlockFields.SurfaceDescription.NormalWS));
            // Complex Lit

            // Template Predicates
            //context.AddField(UniversalFields.PredicateClearCoat, clearCoat);
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
            context.AddBlock(BlockFields.SurfaceDescription.Specular, (workflowMode == WorkflowMode.Specular) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic, (workflowMode == WorkflowMode.Metallic) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha, (target.surfaceType == SurfaceType.Transparent || target.alphaClip) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, (target.alphaClip) || target.allowMaterialOverride);

            // always controlled by subtarget clearCoat checkbox (no Material control)
            context.AddBlock(BlockFields.SurfaceDescription.CoatMask, clearCoat);
            context.AddBlock(BlockFields.SurfaceDescription.CoatSmoothness, clearCoat);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // if using material control, add the material property to control workflow mode
            if (target.allowMaterialOverride)
            {
                collector.AddFloatProperty(Property.SpecularWorkflowMode, (float)workflowMode);
                collector.AddFloatProperty(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.ReceiveShadows, target.receiveShadows ? 1.0f : 0.0f);

                // setup properties using the defaults
                collector.AddFloatProperty(Property.SurfaceType, (float)target.surfaceType);
                collector.AddFloatProperty(Property.BlendMode, (float)target.alphaMode);
                collector.AddFloatProperty(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.BlendModePreserveSpecular, blendModePreserveSpecular ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.SrcBlend, 1.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddFloatProperty(Property.DstBlend, 0.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddToggleProperty(Property.ZWrite, (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.ZWriteControl, (float)target.zWriteControl);
                collector.AddFloatProperty(Property.ZTest, (float)target.zTestMode);    // ztest mode is designed to directly pass as ztest
                collector.AddFloatProperty(Property.CullMode, (float)target.renderFace);    // render face enum is designed to directly pass as a cull mode

                bool enableAlphaToMask = (target.alphaClip && (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.AlphaToMask, enableAlphaToMask ? 1.0f : 0.0f);
            }

            // We always need these properties regardless of whether the material is allowed to override other shader properties.
            // Queue control & offset enable correct automatic render queue behavior.  Control == 0 is automatic, 1 is user-specified.
            // We initialize queue control to -1 to indicate to UpdateMaterial that it needs to initialize it properly on the material.
            collector.AddFloatProperty(Property.QueueOffset, 0.0f);
            collector.AddFloatProperty(Property.QueueControl, -1.0f);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var universalTarget = (target as UniversalTarget);
            universalTarget.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);

            context.AddProperty("Workflow Mode", new EnumField(WorkflowMode.Metallic) { value = workflowMode }, (evt) =>
            {
                if (Equals(workflowMode, evt.newValue))
                    return;

                registerUndo("Change Workflow");
                workflowMode = (WorkflowMode)evt.newValue;
                onChange();
            });

            universalTarget.AddDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo, showReceiveShadows: true);

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

            if (target.surfaceType == SurfaceType.Transparent)
            {
                if (target.alphaMode == AlphaMode.Alpha || target.alphaMode == AlphaMode.Additive)
                    context.AddProperty("Preserve Specular Lighting", new Toggle() { value = blendModePreserveSpecular }, (evt) =>
                    {
                        if (Equals(blendModePreserveSpecular, evt.newValue))
                            return;

                        registerUndo("Change Preserve Specular");
                        blendModePreserveSpecular = evt.newValue;
                        onChange();
                    });
            }
        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            int hash = base.ComputeMaterialNeedsUpdateHash();
            hash = hash * 23 + target.allowMaterialOverride.GetHashCode();
            return hash;
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

        internal override void OnAfterParentTargetDeserialized()
        {
            Assert.IsNotNull(target);

            if (this.sgVersion < latestVersion)
            {
                // Upgrade old incorrect Premultiplied blend into
                // equivalent Alpha + Preserve Specular blend mode.
                if (this.sgVersion < 1)
                {
                    if (target.alphaMode == AlphaMode.Premultiply)
                    {
                        target.alphaMode = AlphaMode.Alpha;
                        blendModePreserveSpecular = true;
                    }
                    else
                        blendModePreserveSpecular = false;
                }
                ChangeVersion(latestVersion);
            }
        }

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor LitSubShader(UniversalTarget target, WorkflowMode workflowMode, string renderType, string renderQueue, string disableBatchingTag, bool complexLit, bool blendModePreserveSpecular)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = complexLit ? UniversalTarget.kComplexLitMaterialTypeTag : UniversalTarget.kLitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    disableBatchingTag = disableBatchingTag,
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                if (complexLit)
                    result.passes.Add(LitPasses.ForwardOnly(target, workflowMode, complexLit, blendModePreserveSpecular, CoreBlockMasks.Vertex, LitBlockMasks.FragmentComplexLit, CorePragmas.Forward, LitKeywords.Forward));
                else
                    result.passes.Add(LitPasses.Forward(target, workflowMode, blendModePreserveSpecular, CorePragmas.Forward, LitKeywords.Forward));

                // ForwardOnly ComplexLit fills GBuffer too for potential custom usage of the GBuffer.
                result.passes.Add(LitPasses.GBuffer(target, workflowMode, blendModePreserveSpecular));

                // cull the shadowcaster pass if we know it will never be used
                if (target.castShadows || target.allowMaterialOverride)
                    result.passes.Add(PassVariant(CorePasses.ShadowCaster(target), CorePragmas.Instanced));

                if (target.mayWriteDepth)
                    result.passes.Add(PassVariant(CorePasses.DepthOnly(target), CorePragmas.Instanced));

                if (complexLit)
                    result.passes.Add(PassVariant(LitPasses.DepthNormalOnly(target), CorePragmas.Instanced));
                else
                    result.passes.Add(PassVariant(LitPasses.DepthNormal(target), CorePragmas.Instanced));

                result.passes.Add(PassVariant(LitPasses.Meta(target), CorePragmas.Default));

                // Currently neither of these passes (selection/picking) can be last for the game view for
                // UI shaders to render correctly. Verify [1352225] before changing this order.
                result.passes.Add(PassVariant(CorePasses.SceneSelection(target), CorePragmas.Default));
                result.passes.Add(PassVariant(CorePasses.ScenePicking(target), CorePragmas.Default));
                result.passes.Add(PassVariant(LitPasses._2D(target), CorePragmas.Default));

                return result;
            }
        }
        #endregion

        #region Passes
        static class LitPasses
        {
            static void AddWorkflowModeControlToPass(ref PassDescriptor pass, UniversalTarget target, WorkflowMode workflowMode)
            {
                if (target.allowMaterialOverride)
                    pass.keywords.Add(LitDefines.SpecularSetup);
                else if (workflowMode == WorkflowMode.Specular)
                    pass.defines.Add(LitDefines.SpecularSetup, 1);
            }

            static void AddReceiveShadowsControlToPass(ref PassDescriptor pass, UniversalTarget target, bool receiveShadows)
            {
                if (target.allowMaterialOverride)
                    pass.keywords.Add(LitKeywords.ReceiveShadowsOff);
                else if (!receiveShadows)
                    pass.defines.Add(LitKeywords.ReceiveShadowsOff, 1);
            }

            public static PassDescriptor Forward(
                UniversalTarget target,
                WorkflowMode workflowMode,
                bool blendModePreserveSpecular,
                PragmaCollection pragmas,
                KeywordCollection keywords)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Universal Forward",
                    referenceName = "SHADERPASS_FORWARD",
                    lightMode = "UniversalForward",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target, blendModePreserveSpecular),
                    pragmas = pragmas ?? CorePragmas.Forward,     // NOTE: SM 2.0 only GL
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection() { keywords },
                    includes = LitIncludes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target, blendModePreserveSpecular);
                CorePasses.AddAlphaToMaskControlToPass(ref result, target);
                AddWorkflowModeControlToPass(ref result, target, workflowMode);
                AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor ForwardOnly(
                UniversalTarget target,
                WorkflowMode workflowMode,
                bool complexLit,
                bool blendModePreserveSpecular,
                BlockFieldDescriptor[] vertexBlocks,
                BlockFieldDescriptor[] pixelBlocks,
                PragmaCollection pragmas,
                KeywordCollection keywords)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Universal Forward Only",
                    referenceName = "SHADERPASS_FORWARDONLY",
                    lightMode = "UniversalForwardOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = vertexBlocks,
                    validPixelBlocks = pixelBlocks,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target, blendModePreserveSpecular),
                    pragmas = pragmas,
                    defines = new DefineCollection { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection { keywords },
                    includes = new IncludeCollection { LitIncludes.Forward },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                if (complexLit)
                    result.defines.Add(LitDefines.ClearCoat, 1);

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target, blendModePreserveSpecular);
                CorePasses.AddAlphaToMaskControlToPass(ref result, target);
                AddWorkflowModeControlToPass(ref result, target, workflowMode);
                AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }

            // Deferred only in SM4.5, MRT not supported in GLES2
            public static PassDescriptor GBuffer(UniversalTarget target, WorkflowMode workflowMode, bool blendModePreserveSpecular)
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
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.GBuffer,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target, blendModePreserveSpecular),
                    pragmas = CorePragmas.GBuffer,
                    defines = new DefineCollection { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection { LitKeywords.GBuffer },
                    includes = new IncludeCollection { LitIncludes.GBuffer },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target, blendModePreserveSpecular);
                AddWorkflowModeControlToPass(ref result, target, workflowMode);
                AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor Meta(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Meta",
                    referenceName = "SHADERPASS_META",
                    lightMode = "Meta",

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
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
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection() { CoreKeywordDescriptors.EditorVisualization },
                    includes = LitIncludes.Meta,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor _2D(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    referenceName = "SHADERPASS_2D",
                    lightMode = "Universal2D",

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                    // Fields
                    structs = CoreStructCollections.Default,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = LitIncludes._2D,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor DepthNormal(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "DepthNormals",
                    referenceName = "SHADERPASS_DEPTHNORMALS",
                    lightMode = "DepthNormals",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentDepthNormals,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = CoreRequiredFields.DepthNormals,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthNormalsOnly(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = new IncludeCollection { CoreIncludes.DepthNormalsOnly },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor DepthNormalOnly(UniversalTarget target)
            {
                var result = new PassDescriptor()
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
                    validPixelBlocks = CoreBlockMasks.FragmentDepthNormals,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = CoreRequiredFields.DepthNormals,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthNormalsOnly(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = new IncludeCollection { CoreIncludes.DepthNormalsOnly },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
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
        }
        #endregion

        #region RequiredFields
        static class LitRequiredFields
        {
            public static readonly FieldCollection Forward = new FieldCollection()
            {
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static readonly FieldCollection GBuffer = new FieldCollection()
            {
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static readonly FieldCollection Meta = new FieldCollection()
            {
                StructFields.Attributes.positionOS,
                StructFields.Attributes.normalOS,
                StructFields.Attributes.uv0,                            //
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Attributes.uv2,                            // needed for meta UVs
                StructFields.Attributes.instanceID,                     // needed for rendering instanced terrain
                StructFields.Varyings.positionCS,
                StructFields.Varyings.texCoord0,                        // needed for meta UVs
                StructFields.Varyings.texCoord1,                        // VizUV
                StructFields.Varyings.texCoord2,                        // LightCoord
            };
        }
        #endregion

        #region Defines
        static class LitDefines
        {
            public static readonly KeywordDescriptor ClearCoat = new KeywordDescriptor()
            {
                displayName = "Clear Coat",
                referenceName = "_CLEARCOAT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment
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
            public static readonly KeywordDescriptor ReceiveShadowsOff = new KeywordDescriptor()
            {
                displayName = "Receive Shadows Off",
                referenceName = ShaderKeywordStrings._RECEIVE_SHADOWS_OFF,
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordCollection Forward = new KeywordCollection
            {
                { CoreKeywordDescriptors.ScreenSpaceAmbientOcclusion },
                { CoreKeywordDescriptors.StaticLightmap },
                { CoreKeywordDescriptors.DynamicLightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.AdditionalLights },
                { CoreKeywordDescriptors.AdditionalLightShadows },
                { CoreKeywordDescriptors.ReflectionProbeBlending },
                { CoreKeywordDescriptors.ReflectionProbeBoxProjection },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { CoreKeywordDescriptors.DBuffer },
                { CoreKeywordDescriptors.LightLayers },
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywordDescriptors.LightCookies },
                { CoreKeywordDescriptors.ForwardPlus },
            };

            public static readonly KeywordCollection GBuffer = new KeywordCollection
            {
                { CoreKeywordDescriptors.StaticLightmap },
                { CoreKeywordDescriptors.DynamicLightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.ReflectionProbeBlending },
                { CoreKeywordDescriptors.ReflectionProbeBoxProjection },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { CoreKeywordDescriptors.MixedLightingSubtractive },
                { CoreKeywordDescriptors.DBuffer },
                { CoreKeywordDescriptors.GBufferNormalsOct },
                { CoreKeywordDescriptors.RenderPassEnabled },
                { CoreKeywordDescriptors.DebugDisplay },
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
                { CoreIncludes.DOTSPregraph },
                { CoreIncludes.WriteRenderLayersPregraph },
                { CoreIncludes.CorePregraph },
                { kShadows, IncludeLocation.Pregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kForwardPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection GBuffer = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.DOTSPregraph },
                { CoreIncludes.WriteRenderLayersPregraph },
                { CoreIncludes.CorePregraph },
                { kShadows, IncludeLocation.Pregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },

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
