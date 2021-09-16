using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.ShaderGraph.Serialization;
using UnityEditor.Rendering.BuiltIn;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using BlendMode = UnityEngine.Rendering.BlendMode;
using BlendOp = UnityEditor.ShaderGraph.BlendOp;

namespace UnityEditor.Rendering.Fullscreen.ShaderGraph
{
    sealed class FullscreenTarget : Target, IHasMetadata
    {
        public enum MaterialType
        {
            Blit,
            // DrawProcedural, // TODO
            // CustomRenderTexture,
        }

        public enum FullscreenBlendMode
        {
            Disabled,
            Alpha,
            Premultiply,
            Additive,
            Multiply,
            Custom,
        }

        public override int latestVersion => 0;

        // Constants
        static readonly GUID kSourceCodeGuid = new GUID("11771342b6f6ab840ba9e2274ddd9db3"); // FullscreenTarget.cs
        public static readonly string[] kSharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories().Union(new string[] { "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Templates" }).ToArray();
        public const string kTemplatePath = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Templates/ShaderPass.template";

        // SubTarget
        List<SubTarget> m_SubTargets;
        List<string> m_SubTargetNames;
        int activeSubTargetIndex => m_SubTargets.IndexOf(m_ActiveSubTarget);

        // View
        PopupField<string> m_SubTargetField;
        TextField m_CustomGUIField;

        [SerializeField]
        JsonData<SubTarget> m_ActiveSubTarget;

        // When checked, allows the material to control ALL surface settings (uber shader style)
        [SerializeField]
        bool m_AllowMaterialOverride = false;

        [SerializeField]
        FullscreenBlendMode m_BlendMode = FullscreenBlendMode.Disabled;

        [SerializeField]
        BlendMode m_SrcColorBlendMode = BlendMode.Zero;
        [SerializeField]
        BlendMode m_DstColorBlendMode = BlendMode.One;
        [SerializeField]
        BlendOp m_ColorBlendOperation = BlendOp.Add;

        [SerializeField]
        BlendMode m_SrcAlphaBlendMode = BlendMode.Zero;
        [SerializeField]
        BlendMode m_DstAlphaBlendMode = BlendMode.One;
        [SerializeField]
        BlendOp m_AlphaBlendOperation = BlendOp.Add;

        [SerializeField]
        bool m_EnableStencil = false;
        [SerializeField]
        int m_StencilReference = 0;
        [SerializeField]
        int m_StencilReadMask = 255;
        [SerializeField]
        int m_StencilWriteMask = 255;
        [SerializeField]
        CompareFunction m_StencilCompareFunction = CompareFunction.Always;
        [SerializeField]
        StencilOp m_StencilPassOperation = StencilOp.Keep;
        [SerializeField]
        StencilOp m_StencilFailOperation = StencilOp.Keep;
        [SerializeField]
        StencilOp m_StencilDepthFailOperation = StencilOp.Keep;

        [SerializeField]
        bool m_DepthWrite = false;

        [SerializeField]
        ZTestMode m_DepthTestMode = ZTestMode.Always;

        [SerializeField]
        string m_CustomEditorGUI;

        internal override bool ignoreCustomInterpolators => true;
        internal override int padCustomInterpolatorLimit => 4;

        public FullscreenTarget()
        {
            displayName = "Full-screen";
            m_SubTargets = TargetUtils.GetSubTargets(this);
            m_SubTargetNames = m_SubTargets.Select(x => x.displayName).ToList();
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
        }

        public FullscreenBlendMode blendMode
        {
            get => m_BlendMode;
            set => m_BlendMode = value;
        }

        public BlendMode srcColorBlendMode
        {
            get => m_SrcColorBlendMode;
            set => m_SrcColorBlendMode = value;
        }

        public BlendMode dstColorBlendMode
        {
            get => m_DstColorBlendMode;
            set => m_DstColorBlendMode = value;
        }

        public BlendOp colorBlendOperation
        {
            get => m_ColorBlendOperation;
            set => m_ColorBlendOperation = value;
        }

        public BlendMode srcAlphaBlendMode
        {
            get => m_SrcAlphaBlendMode;
            set => m_SrcAlphaBlendMode = value;
        }

        public BlendMode dstAlphaBlendMode
        {
            get => m_DstAlphaBlendMode;
            set => m_DstAlphaBlendMode = value;
        }

        public BlendOp alphaBlendOperation
        {
            get => m_AlphaBlendOperation;
            set => m_AlphaBlendOperation = value;
        }

        public bool enableStencil
        {
            get => m_EnableStencil;
            set => m_EnableStencil = value;
        }

        public int stencilReference
        {
            get => m_StencilReference;
            set => m_StencilReference = Mathf.Clamp(value, 0, 255);
        }

        public int stencilReadMask
        {
            get => m_StencilReadMask;
            set => m_StencilReadMask = Mathf.Clamp(value, 0, 255);
        }

        public int stencilWriteMask
        {
            get => m_StencilWriteMask;
            set => m_StencilWriteMask = Mathf.Clamp(value, 0, 255);
        }

        public CompareFunction stencilCompareFunction
        {
            get => m_StencilCompareFunction;
            set => m_StencilCompareFunction = value;
        }

        public StencilOp stencilPassOperation
        {
            get => m_StencilPassOperation;
            set => m_StencilPassOperation = value;
        }

        public StencilOp stencilFailOperation
        {
            get => m_StencilFailOperation;
            set => m_StencilFailOperation = value;
        }

        public StencilOp stencilDepthTestFailOperation
        {
            get => m_StencilDepthFailOperation;
            set => m_StencilDepthFailOperation = value;
        }

        public bool depthWrite
        {
            get => m_DepthWrite;
            set => m_DepthWrite = value;
        }

        public SubTarget activeSubTarget
        {
            get => m_ActiveSubTarget.value;
            set => m_ActiveSubTarget = value;
        }

        public bool allowMaterialOverride
        {
            get => m_AllowMaterialOverride;
            set => m_AllowMaterialOverride = value;
        }

        public ZTestMode depthTestMode
        {
            get => m_DepthTestMode;
            set => m_DepthTestMode = value;
        }

        public string customEditorGUI
        {
            get => m_CustomEditorGUI;
            set => m_CustomEditorGUI = value;
        }

        public override bool IsActive() => activeSubTarget.IsActive();

        public override bool IsNodeAllowedByTarget(Type nodeType)
        {
            // TODO: we must remove a lot of nodes :/
            return base.IsNodeAllowedByTarget(nodeType);
        }

        public override void Setup(ref TargetSetupContext context)
        {
            // Setup the Target
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);

            // Setup the active SubTarget
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            if (m_ActiveSubTarget.value == null)
                return;
            m_ActiveSubTarget.value.target = this;
            m_ActiveSubTarget.value.Setup(ref context);

            // Override EditorGUI
            if (!string.IsNullOrEmpty(m_CustomEditorGUI))
            {
                context.SetDefaultShaderGUI(m_CustomEditorGUI);
            }
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            m_ActiveSubTarget.value.target = this;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);
            // Core fields
            // Always force vertex as the shim between built-in cginc files and hlsl files requires this
            context.AddField(Fields.GraphVertex);
            context.AddField(Fields.GraphPixel);

            // SubTarget fields
            m_ActiveSubTarget.value.GetFields(ref context);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Core blocks
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);

            // SubTarget blocks
            m_ActiveSubTarget.value.GetActiveBlocks(ref context);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            activeSubTarget.CollectShaderProperties(collector, generationMode);
            // collector.AddShaderProperty(LightmappingShaderProperties.kLightmapsArray);
            // collector.AddShaderProperty(LightmappingShaderProperties.kLightmapsIndirectionArray);
            // collector.AddShaderProperty(LightmappingShaderProperties.kShadowMasksArray);
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            m_ActiveSubTarget.value.ProcessPreviewMaterial(material);
        }

        public override object saveContext => m_ActiveSubTarget.value?.saveContext;

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            if (m_ActiveSubTarget.value == null)
                return;

            // Core properties
            // m_SubTargetField = new PopupField<string>(m_SubTargetNames, activeSubTargetIndex);
            // context.AddProperty("Material", m_SubTargetField, (evt) =>
            // {
            //     if (Equals(activeSubTargetIndex, m_SubTargetField.index))
            //         return;

            //     registerUndo("Change Material");
            //     m_ActiveSubTarget = m_SubTargets[m_SubTargetField.index];
            //     onChange();
            // });

            context.AddProperty("Allow Material Override", new Toggle() { value = allowMaterialOverride }, (evt) =>
            {
                if (Equals(allowMaterialOverride, evt.newValue))
                    return;

                registerUndo("Change Allow Material Override");
                allowMaterialOverride = evt.newValue;
                onChange();
            });

            GetRenderStatePropertiesGUI(ref context, onChange, registerUndo);

            // SubTarget properties
            m_ActiveSubTarget.value.GetPropertiesGUI(ref context, onChange, registerUndo);

            // Custom Editor GUI
            // Requires FocusOutEvent
            m_CustomGUIField = new TextField("") { value = customEditorGUI };
            m_CustomGUIField.RegisterCallback<FocusOutEvent>(s =>
            {
                if (Equals(customEditorGUI, m_CustomGUIField.value))
                    return;

                registerUndo("Change Custom Editor GUI");
                customEditorGUI = m_CustomGUIField.value;
                onChange();
            });
            context.AddProperty("Custom Editor GUI", m_CustomGUIField, (evt) => { });
        }

        public void GetRenderStatePropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddProperty("Blend Mode", new EnumField(blendMode) { value = blendMode }, (evt) =>
            {
                if (Equals(blendMode, evt.newValue))
                    return;

                registerUndo("Change Blend Mode");
                blendMode = (FullscreenBlendMode)evt.newValue;
                onChange();
            });

            if (blendMode == FullscreenBlendMode.Custom)
            {
                context.globalIndentLevel++;
                context.AddLabel("Color Blend Mode", 0);

                context.AddProperty("Src Color", new EnumField(srcColorBlendMode) { value = srcColorBlendMode }, (evt) =>
                {
                    if (Equals(srcColorBlendMode, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    srcColorBlendMode = (BlendMode)evt.newValue;
                    onChange();
                });
                context.AddProperty("Dst Color", new EnumField(dstColorBlendMode) { value = dstColorBlendMode }, (evt) =>
                {
                    if (Equals(dstColorBlendMode, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    dstColorBlendMode = (BlendMode)evt.newValue;
                    onChange();
                });
                context.AddProperty("Color Operation", new EnumField(colorBlendOperation) { value = colorBlendOperation }, (evt) =>
                {
                    if (Equals(colorBlendOperation, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    colorBlendOperation = (BlendOp)evt.newValue;
                    onChange();
                });

                context.AddLabel("Alpha Blend Mode", 0);


                context.AddProperty("Src", new EnumField(srcAlphaBlendMode) { value = srcAlphaBlendMode }, (evt) =>
                {
                    if (Equals(srcAlphaBlendMode, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    srcAlphaBlendMode = (BlendMode)evt.newValue;
                    onChange();
                });
                context.AddProperty("Dst", new EnumField(dstAlphaBlendMode) { value = dstAlphaBlendMode }, (evt) =>
                {
                    if (Equals(dstAlphaBlendMode, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    dstAlphaBlendMode = (BlendMode)evt.newValue;
                    onChange();
                });
                context.AddProperty("Blend Operation Alpha", new EnumField(alphaBlendOperation) { value = alphaBlendOperation }, (evt) =>
                {
                    if (Equals(alphaBlendOperation, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    alphaBlendOperation = (BlendOp)evt.newValue;
                    onChange();
                });

                context.globalIndentLevel--;
            }

            context.AddProperty("Enable Stencil", new Toggle { value = enableStencil }, (evt) =>
             {
                 if (Equals(enableStencil, evt.newValue))
                     return;

                 registerUndo("Change Enable Stencil");
                 enableStencil = evt.newValue;
                 onChange();
             });

            if (enableStencil)
            {
                context.globalIndentLevel++;

                context.AddProperty("Reference", new IntegerField { value = stencilReference, isDelayed = true }, (evt) =>
                 {
                     if (Equals(stencilReference, evt.newValue))
                         return;

                     registerUndo("Change Stencil Reference");
                     stencilReference = evt.newValue;
                     onChange();
                 });

                context.AddProperty("Read Mask", new IntegerField { value = stencilReadMask, isDelayed = true }, (evt) =>
                 {
                     if (Equals(stencilReadMask, evt.newValue))
                         return;

                     registerUndo("Change Stencil Read Mask");
                     stencilReadMask = evt.newValue;
                     onChange();
                 });

                context.AddProperty("Write Mask", new IntegerField { value = stencilWriteMask, isDelayed = true }, (evt) =>
                 {
                     if (Equals(stencilWriteMask, evt.newValue))
                         return;

                     registerUndo("Change Stencil Write Mask");
                     stencilWriteMask = evt.newValue;
                     onChange();
                 });

                context.AddProperty("Comparison", new EnumField(stencilCompareFunction) { value = stencilCompareFunction }, (evt) =>
                {
                    if (Equals(stencilCompareFunction, evt.newValue))
                        return;

                    registerUndo("Change Stencil Comparison");
                    stencilCompareFunction = (CompareFunction)evt.newValue;
                    onChange();
                });

                context.AddProperty("Pass", new EnumField(stencilPassOperation) { value = stencilPassOperation }, (evt) =>
                {
                    if (Equals(stencilPassOperation, evt.newValue))
                        return;

                    registerUndo("Change Stencil Pass Operation");
                    stencilPassOperation = (StencilOp)evt.newValue;
                    onChange();
                });

                context.AddProperty("Fail", new EnumField(stencilFailOperation) { value = stencilFailOperation }, (evt) =>
                {
                    if (Equals(stencilFailOperation, evt.newValue))
                        return;

                    registerUndo("Change Stencil Fail Operation");
                    stencilFailOperation = (StencilOp)evt.newValue;
                    onChange();
                });

                context.AddProperty("Depth Fail", new EnumField(stencilDepthTestFailOperation) { value = stencilDepthTestFailOperation }, (evt) =>
                {
                    if (Equals(stencilDepthTestFailOperation, evt.newValue))
                        return;

                    registerUndo("Change Stencil Depth Fail Operation");
                    stencilDepthTestFailOperation = (StencilOp)evt.newValue;
                    onChange();
                });

                context.globalIndentLevel--;
            }

            context.AddProperty("Depth Test", new EnumField(ZTestMode.LEqual) { value = depthTestMode }, (evt) =>
            {
                if (Equals(depthTestMode, evt.newValue))
                    return;

                registerUndo("Change Depth Test");
                depthTestMode = (ZTestMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Write", new Toggle { value = depthWrite }, (evt) =>
             {
                 if (Equals(depthTestMode, evt.newValue))
                     return;

                 registerUndo("Change Depth Test");
                 depthWrite = evt.newValue;
                 onChange();
             });

        }

        public bool TrySetActiveSubTarget(Type subTargetType)
        {
            if (!subTargetType.IsSubclassOf(typeof(SubTarget)))
                return false;

            foreach (var subTarget in m_SubTargets)
            {
                if (subTarget.GetType().Equals(subTargetType))
                {
                    m_ActiveSubTarget = subTarget;
                    return true;
                }
            }

            return false;
        }

        // The fullscreen target is compatible with all pipeline (it doesn't rely on any RP rendering feature)
        public override bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline) => true;

        public override void OnAfterDeserialize(string json)
        {
            base.OnAfterDeserialize(json);

            if (this.sgVersion < latestVersion)
            {
                ChangeVersion(latestVersion);
            }
        }

        #region Metadata
        string IHasMetadata.identifier
        {
            get
            {
                // defer to subtarget
                if (m_ActiveSubTarget.value is IHasMetadata subTargetHasMetaData)
                    return subTargetHasMetaData.identifier;
                return null;
            }
        }

        ScriptableObject IHasMetadata.GetMetadataObject()
        {
            // defer to subtarget
            if (m_ActiveSubTarget.value is IHasMetadata subTargetHasMetaData)
                return subTargetHasMetaData.GetMetadataObject();
            return null;
        }

        #endregion
    }

    #region Passes
    static class CorePasses
    {
        internal static void AddSurfaceTypeControlToPass(ref PassDescriptor pass, FullscreenTarget target)
        {
            // if (target.allowMaterialOverride)
            // {
            //     pass.keywords.Add(CoreKeywordDescriptors.SurfaceTypeTransparent);
            // }
            // else if (target.surfaceType == SurfaceType.Transparent)
            // {
            //     pass.defines.Add(CoreKeywordDescriptors.SurfaceTypeTransparent, 1);
            // }
        }

        internal static void AddAlphaPremultiplyControlToPass(ref PassDescriptor pass, FullscreenTarget target)
        {
            // if (target.allowMaterialOverride)
            // {
            //     pass.keywords.Add(CoreKeywordDescriptors.AlphaPremultiplyOn);
            // }
            // else if (target.alphaMode == AlphaMode.Premultiply)
            // {
            //     pass.defines.Add(CoreKeywordDescriptors.AlphaPremultiplyOn, 1);
            // }
        }

        internal static void AddAlphaClipControlToPass(ref PassDescriptor pass, FullscreenTarget target)
        {
            // if (target.allowMaterialOverride)
            // {
            //     pass.keywords.Add(CoreKeywordDescriptors.AlphaClip);
            //     pass.keywords.Add(CoreKeywordDescriptors.AlphaTestOn);
            // }
            // else if (target.alphaClip)
            // {
            //     pass.defines.Add(CoreKeywordDescriptors.AlphaClip, 1);
            //     pass.defines.Add(CoreKeywordDescriptors.AlphaTestOn, 1);
            // }
        }

        // public static PassDescriptor DepthOnly(FullscreenTarget target)
        // {
        //     var result = new PassDescriptor()
        //     {
        //         // Definition
        //         displayName = "DepthOnly",
        //         referenceName = "SHADERPASS_DEPTHONLY",
        //         lightMode = "DepthOnly",
        //         useInPreview = true,

        //         // Template
        //         passTemplatePath = FullscreenTarget.kTemplatePath,
        //         sharedTemplateDirectories = FullscreenTarget.kSharedTemplateDirectories,

        //         // Port Mask
        //         validVertexBlocks = CoreBlockMasks.Vertex,
        //         validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

        //         // Fields
        //         structs = CoreStructCollections.Default,
        //         fieldDependencies = CoreFieldDependencies.Default,

        //         // Conditional State
        //         renderStates = CoreRenderStates.DepthOnly(target),
        //         pragmas = CorePragmas.Instanced,
        //         defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
        //         keywords = new KeywordCollection(),
        //         includes = CoreIncludes.DepthOnly,

        //         // Custom Interpolator Support
        //         customInterpolators = CoreCustomInterpDescriptors.Common
        //     };

        //     AddAlphaClipControlToPass(ref result, target);

        //     return result;
        // }

        // public static PassDescriptor ShadowCaster(FullscreenTarget target)
        // {
        //     var result = new PassDescriptor()
        //     {
        //         // Definition
        //         displayName = "ShadowCaster",
        //         referenceName = "SHADERPASS_SHADOWCASTER",
        //         lightMode = "ShadowCaster",

        //         // Template
        //         passTemplatePath = FullscreenTarget.kTemplatePath,
        //         sharedTemplateDirectories = FullscreenTarget.kSharedTemplateDirectories,

        //         // Port Mask
        //         validVertexBlocks = CoreBlockMasks.Vertex,
        //         validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

        //         // Fields
        //         structs = CoreStructCollections.Default,
        //         requiredFields = CoreRequiredFields.ShadowCaster,
        //         fieldDependencies = CoreFieldDependencies.Default,

        //         // Conditional State
        //         renderStates = CoreRenderStates.ShadowCaster(target),
        //         pragmas = CorePragmas.ShadowCaster,
        //         defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
        //         keywords = new KeywordCollection { CoreKeywords.ShadowCaster },
        //         includes = CoreIncludes.ShadowCaster,

        //         // Custom Interpolator Support
        //         customInterpolators = CoreCustomInterpDescriptors.Common
        //     };

        //     AddCommonPassSurfaceControlsToPass(ref result, target);

        //     return result;
        // }

        // public static PassDescriptor SceneSelection(FullscreenTarget target)
        // {
        //     var result = new PassDescriptor()
        //     {
        //         // Definition
        //         displayName = "SceneSelectionPass",
        //         referenceName = "SceneSelectionPass",
        //         lightMode = "SceneSelectionPass",
        //         useInPreview = true,

        //         // Template
        //         passTemplatePath = FullscreenTarget.kTemplatePath,
        //         sharedTemplateDirectories = FullscreenTarget.kSharedTemplateDirectories,

        //         // Port Mask
        //         validVertexBlocks = CoreBlockMasks.Vertex,
        //         validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

        //         // Fields
        //         structs = CoreStructCollections.Default,
        //         fieldDependencies = CoreFieldDependencies.Default,

        //         // Conditional State
        //         renderStates = CoreRenderStates.SceneSelection(target),
        //         pragmas = CorePragmas.Instanced,
        //         defines = new DefineCollection { CoreDefines.SceneSelection },
        //         keywords = new KeywordCollection(),
        //         includes = CoreIncludes.SceneSelection,

        //         // Custom Interpolator Support
        //         customInterpolators = CoreCustomInterpDescriptors.Common
        //     };

        //     AddCommonPassSurfaceControlsToPass(ref result, target);

        //     return result;
        // }

        // public static PassDescriptor ScenePicking(FullscreenTarget target)
        // {
        //     var result = new PassDescriptor()
        //     {
        //         // Definition
        //         displayName = "ScenePickingPass",
        //         referenceName = "ScenePickingPass",
        //         lightMode = "Picking",
        //         useInPreview = true,

        //         // Template
        //         passTemplatePath = FullscreenTarget.kTemplatePath,
        //         sharedTemplateDirectories = FullscreenTarget.kSharedTemplateDirectories,

        //         // Port Mask
        //         validVertexBlocks = CoreBlockMasks.Vertex,
        //         validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

        //         // Fields
        //         structs = CoreStructCollections.Default,
        //         fieldDependencies = CoreFieldDependencies.Default,

        //         // Conditional State
        //         renderStates = CoreRenderStates.ScenePicking(target),
        //         pragmas = CorePragmas.Instanced,
        //         defines = new DefineCollection { CoreDefines.ScenePicking },
        //         keywords = new KeywordCollection(),
        //         includes = CoreIncludes.ScenePicking,

        //         // Custom Interpolator Support
        //         customInterpolators = CoreCustomInterpDescriptors.Common
        //     };

        //     AddCommonPassSurfaceControlsToPass(ref result, target);

        //     return result;
        // }
    }
    #endregion

    #region FieldDependencies
    static class CoreFieldDependencies
    {
        public static readonly DependencyCollection Default = new DependencyCollection()
        {
            { FieldDependencies.Default },
            // TODO: VR support
            // new FieldDependency(BuiltInStructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,    StructFields.Attributes.instanceID),
            // new FieldDependency(BuiltInStructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,     StructFields.Attributes.instanceID),
        };
    }
    #endregion

    #region RenderStates
    static class CoreRenderStates
    {
        public static class Uniforms
        {
            public static readonly string srcBlend = "[" + Property.SG_SrcBlend + "]";
            public static readonly string dstBlend = "[" + Property.SG_DstBlend + "]";
            public static readonly string cullMode = "[" + Property.SG_Cull + "]";
            public static readonly string zWrite = "[" + Property.SG_ZWrite + "]";
            public static readonly string zTest = "[" + Property.SG_ZTest + "]";
        }

        public static Cull RenderFaceToCull(RenderFace renderFace)
        {
            switch (renderFace)
            {
                case RenderFace.Back:
                    return Cull.Front;
                case RenderFace.Front:
                    return Cull.Back;
                case RenderFace.Both:
                    return Cull.Off;
            }
            return Cull.Back;
        }

        public static void AddUberSwitchedZTest(FullscreenTarget target, RenderStateCollection renderStates)
        {
            if (target.allowMaterialOverride)
                renderStates.Add(RenderState.ZTest(Uniforms.zTest));
            else
                renderStates.Add(RenderState.ZTest(target.depthTestMode.ToString()));
        }

        public static void AddUberSwitchedZWrite(FullscreenTarget target, RenderStateCollection renderStates)
        {
            if (target.allowMaterialOverride)
                renderStates.Add(RenderState.ZWrite(Uniforms.zWrite));
            // TODO
            // else
            // renderStates.Add(RenderState.ZWrite(ZWriteControlToZWrite(target.zWriteControl, target.surfaceType)));
        }

        public static void AddUberSwitchedCull(FullscreenTarget target, RenderStateCollection renderStates)
        {
            // TODO
            // renderStates.Add(RenderState.Cull());
        }

        public static void AddUberSwitchedBlend(FullscreenTarget target, RenderStateCollection renderStates)
        {
            if (target.allowMaterialOverride)
            {
                renderStates.Add(RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend));
            }
            else
            {
                // TODO
                // if (target.alphaMode == AlphaMode.Alpha)
                //     renderStates.Add(RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                // else if (target.alphaMode == AlphaMode.Premultiply)
                //     renderStates.Add(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                // else if (target.alphaMode == AlphaMode.Additive)
                //     renderStates.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One, Blend.One, Blend.One));
                // else if (target.alphaMode == AlphaMode.Multiply)
                //     renderStates.Add(RenderState.Blend(Blend.DstColor, Blend.Zero));
            }
        }

        public static readonly RenderStateCollection MaterialControlledDefault = new RenderStateCollection
        {
            { RenderState.ZTest(Uniforms.zTest) },
            { RenderState.ZWrite(Uniforms.zWrite) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend) },
        };

        public static RenderStateCollection Default(FullscreenTarget target)
        {
            if (target.allowMaterialOverride)
                return MaterialControlledDefault;
            else
            {
                var result = new RenderStateCollection();
                AddUberSwitchedZTest(target, result);
                AddUberSwitchedZWrite(target, result);
                AddUberSwitchedCull(target, result);
                AddUberSwitchedBlend(target, result);
                // TODO: option?
                // result.Add(RenderState.ColorMask("ColorMask RGBA"));
                return result;
            }
        }

        // public static RenderStateCollection ForwardAdd(FullscreenTarget target)
        // {
        //     var result = new RenderStateCollection();

        //     result.Add(RenderState.ZWrite(ZWrite.Off));
        //     if (target.surfaceType != SurfaceType.Opaque)
        //     {
        //         result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One));
        //         result.Add(RenderState.ColorMask("ColorMask RGB"));
        //     }
        //     else
        //     {
        //         result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One, Blend.One, Blend.One));
        //     }
        //     return result;
        // }

        // public static readonly RenderStateCollection Meta = new RenderStateCollection
        // {
        //     { RenderState.Cull(Cull.Off) },
        // };

        // public static RenderStateCollection ShadowCaster(FullscreenTarget target)
        // {
        //     var result = new RenderStateCollection();
        //     result.Add(RenderState.ZTest(ZTest.LEqual));
        //     result.Add(RenderState.ZWrite(ZWrite.On));
        //     AddUberSwitchedCull(target, result);
        //     AddUberSwitchedBlend(target, result);
        //     result.Add(RenderState.ColorMask("ColorMask 0"));
        //     return result;
        // }

        // public static RenderStateCollection DepthOnly(FullscreenTarget target)
        // {
        //     var result = new RenderStateCollection();
        //     result.Add(RenderState.ZTest(ZTest.LEqual));
        //     result.Add(RenderState.ZWrite(ZWrite.On));
        //     AddUberSwitchedCull(target, result);
        //     AddUberSwitchedBlend(target, result);
        //     result.Add(RenderState.ColorMask("ColorMask 0"));
        //     return result;
        // }

        // public static RenderStateCollection SceneSelection(FullscreenTarget target)
        // {
        //     var result = new RenderStateCollection()
        //     {
        //         { RenderState.Cull(Cull.Off) }
        //     };
        //     return result;
        // }

        // public static RenderStateCollection ScenePicking(FullscreenTarget target)
        // {
        //     var result = new RenderStateCollection();
        //     AddUberSwitchedCull(target, result);
        //     return result;
        // }
    }
    #endregion

    #region Pragmas

    static class CorePragmas
    {
        public static readonly PragmaCollection Default = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target30) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        // public static readonly PragmaCollection Instanced = new PragmaCollection
        // {
        //     { Pragma.Target(ShaderModel.Target30) },
        //     { Pragma.MultiCompileInstancing },
        //     { Pragma.Vertex("vert") },
        //     { Pragma.Fragment("frag") },
        // };

        // public static readonly PragmaCollection Forward = new PragmaCollection
        // {
        //     { Pragma.Target(ShaderModel.Target30) },
        //     { Pragma.MultiCompileInstancing },
        //     { Pragma.MultiCompileFog },
        //     { Pragma.MultiCompileForwardBase },
        //     { Pragma.Vertex("vert") },
        //     { Pragma.Fragment("frag") },
        // };

        // public static readonly PragmaCollection ForwardAdd = new PragmaCollection
        // {
        //     { Pragma.Target(ShaderModel.Target30) },
        //     { Pragma.MultiCompileInstancing },
        //     { Pragma.MultiCompileFog },
        //     { Pragma.MultiCompileForwardAddFullShadowsBase },
        //     { Pragma.Vertex("vert") },
        //     { Pragma.Fragment("frag") },
        // };

        // public static readonly PragmaCollection Deferred = new PragmaCollection
        // {
        //     { Pragma.Target(ShaderModel.Target45) },
        //     { Pragma.MultiCompileInstancing },
        //     { new PragmaDescriptor { value = "exclude_renderers nomrt" } },
        //     { Pragma.MultiCompilePrePassFinal },
        //     { Pragma.SkipVariants(new[] {"FOG_LINEAR", "FOG_EXP", "FOG_EXP2" }) },
        //     { Pragma.Vertex("vert") },
        //     { Pragma.Fragment("frag") },
        // };

        // public static readonly PragmaCollection ShadowCaster = new PragmaCollection
        // {
        //     { Pragma.Target(ShaderModel.Target30) },
        //     { Pragma.MultiCompileShadowCaster },
        //     { Pragma.Vertex("vert") },
        //     { Pragma.Fragment("frag") },
        // };
    }
    #endregion

    #region Includes
    static class CoreIncludes
    {
        const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
        const string kTexture = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl";
        const string kCore = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl";
        const string kLighting = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl";
        const string kGraphFunctions = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl";
        const string kVaryings = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl";
        const string kShaderPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl";
        const string kDepthOnlyPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl";
        const string kShadowCasterPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl";

        const string kShims = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl";
        const string kLegacySurfaceVertex = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl";

        public static readonly IncludeCollection CorePregraph = new IncludeCollection
        {
            { kShims, IncludeLocation.Pregraph },
            { kColor, IncludeLocation.Pregraph },
            { kCore, IncludeLocation.Pregraph },
            { kTexture, IncludeLocation.Pregraph },
            { kLighting, IncludeLocation.Pregraph },
            { kLegacySurfaceVertex, IncludeLocation.Pregraph },
        };

        public static readonly IncludeCollection ShaderGraphPregraph = new IncludeCollection
        {
            { kGraphFunctions, IncludeLocation.Pregraph },
        };

        public static readonly IncludeCollection CorePostgraph = new IncludeCollection
        {
            { kShaderPass, IncludeLocation.Postgraph },
            { kVaryings, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection DepthOnly = new IncludeCollection
        {
            // Pre-graph
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kDepthOnlyPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection ShadowCaster = new IncludeCollection
        {
            // Pre-graph
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kShadowCasterPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection SceneSelection = new IncludeCollection
        {
            // Pre-graph
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kDepthOnlyPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection ScenePicking = new IncludeCollection
        {
            // Pre-graph
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kDepthOnlyPass, IncludeLocation.Postgraph },
        };
    }
    #endregion

    #region Defines
    // static class CoreDefines
    // {
    //     public static readonly DefineCollection UseLegacySpriteBlocks = new DefineCollection
    //     {
    //         { CoreKeywordDescriptors.UseLegacySpriteBlocks, 1, new FieldCondition(CoreFields.UseLegacySpriteBlocks, true) },
    //     };
    //     public static readonly DefineCollection BuiltInTargetAPI = new DefineCollection
    //     {
    //         { CoreKeywordDescriptors.BuiltInTargetAPI, 1 },
    //     };
    //     public static readonly DefineCollection SceneSelection = new DefineCollection
    //     {
    //         { CoreKeywordDescriptors.BuiltInTargetAPI, 1 },
    //         { CoreKeywordDescriptors.SceneSelectionPass, 1 },
    //     };
    //     public static readonly DefineCollection ScenePicking = new DefineCollection
    //     {
    //         { CoreKeywordDescriptors.BuiltInTargetAPI, 1 },
    //         { CoreKeywordDescriptors.ScenePickingPass, 1 },
    //     };
    // }
    #endregion

    #region KeywordDescriptors

    static class CoreKeywordDescriptors
    {
        // TODO: cleanup!
        // public static readonly KeywordDescriptor Lightmap = new KeywordDescriptor()
        // {
        //     displayName = "Lightmap",
        //     referenceName = "LIGHTMAP_ON",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor DirectionalLightmapCombined = new KeywordDescriptor()
        // {
        //     displayName = "Directional Lightmap Combined",
        //     referenceName = "DIRLIGHTMAP_COMBINED",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor SampleGI = new KeywordDescriptor()
        // {
        //     displayName = "Sample GI",
        //     referenceName = "_SAMPLE_GI",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.ShaderFeature,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor AlphaTestOn = new KeywordDescriptor()
        // {
        //     displayName = Keyword.SG_AlphaTestOn,
        //     referenceName = Keyword.SG_AlphaTestOn,
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.ShaderFeature,
        //     scope = KeywordScope.Local,
        //     stages = KeywordShaderStage.Fragment,
        // };

        // public static readonly KeywordDescriptor AlphaClip = new KeywordDescriptor()
        // {
        //     displayName = "Alpha Clipping",
        //     referenceName = Keyword.SG_AlphaClip,
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.ShaderFeature,
        //     scope = KeywordScope.Local,
        //     stages = KeywordShaderStage.Fragment,
        // };

        // public static readonly KeywordDescriptor SurfaceTypeTransparent = new KeywordDescriptor()
        // {
        //     displayName = Keyword.SG_SurfaceTypeTransparent,
        //     referenceName = Keyword.SG_SurfaceTypeTransparent,
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.ShaderFeature,
        //     scope = KeywordScope.Local,
        //     stages = KeywordShaderStage.Fragment,
        // };

        // public static readonly KeywordDescriptor AlphaPremultiplyOn = new KeywordDescriptor()
        // {
        //     displayName = Keyword.SG_AlphaPremultiplyOn,
        //     referenceName = Keyword.SG_AlphaPremultiplyOn,
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.ShaderFeature,
        //     scope = KeywordScope.Local,
        //     stages = KeywordShaderStage.Fragment,
        // };

        // public static readonly KeywordDescriptor MainLightShadows = new KeywordDescriptor()
        // {
        //     displayName = "Main Light Shadows",
        //     referenceName = "",
        //     type = KeywordType.Enum,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        //     entries = new KeywordEntry[]
        //     {
        //         new KeywordEntry() { displayName = "Off", referenceName = "" },
        //         new KeywordEntry() { displayName = "No Cascade", referenceName = "MAIN_LIGHT_SHADOWS" },
        //         new KeywordEntry() { displayName = "Cascade", referenceName = "MAIN_LIGHT_SHADOWS_CASCADE" },
        //         new KeywordEntry() { displayName = "Screen", referenceName = "MAIN_LIGHT_SHADOWS_SCREEN" },
        //     }
        // };

        // public static readonly KeywordDescriptor CastingPunctualLightShadow = new KeywordDescriptor()
        // {
        //     displayName = "Casting Punctual Light Shadow",
        //     referenceName = "_CASTING_PUNCTUAL_LIGHT_SHADOW",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor AdditionalLights = new KeywordDescriptor()
        // {
        //     displayName = "Additional Lights",
        //     referenceName = "_ADDITIONAL",
        //     type = KeywordType.Enum,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        //     entries = new KeywordEntry[]
        //     {
        //         new KeywordEntry() { displayName = "Vertex", referenceName = "LIGHTS_VERTEX" },
        //         new KeywordEntry() { displayName = "Fragment", referenceName = "LIGHTS" },
        //         new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
        //     }
        // };

        // public static readonly KeywordDescriptor AdditionalLightShadows = new KeywordDescriptor()
        // {
        //     displayName = "Additional Light Shadows",
        //     referenceName = "_ADDITIONAL_LIGHT_SHADOWS",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor ShadowsSoft = new KeywordDescriptor()
        // {
        //     displayName = "Shadows Soft",
        //     referenceName = "_SHADOWS_SOFT",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor MixedLightingSubtractive = new KeywordDescriptor()
        // {
        //     displayName = "Mixed Lighting Subtractive",
        //     referenceName = "_MIXED_LIGHTING_SUBTRACTIVE",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor LightmapShadowMixing = new KeywordDescriptor()
        // {
        //     displayName = "Lightmap Shadow Mixing",
        //     referenceName = "LIGHTMAP_SHADOW_MIXING",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor ShadowsShadowmask = new KeywordDescriptor()
        // {
        //     displayName = "Shadows Shadowmask",
        //     referenceName = "SHADOWS_SHADOWMASK",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor SmoothnessChannel = new KeywordDescriptor()
        // {
        //     displayName = "Smoothness Channel",
        //     referenceName = "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.ShaderFeature,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor ShapeLightType0 = new KeywordDescriptor()
        // {
        //     displayName = "Shape Light Type 0",
        //     referenceName = "USE_SHAPE_LIGHT_TYPE_0",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor ShapeLightType1 = new KeywordDescriptor()
        // {
        //     displayName = "Shape Light Type 1",
        //     referenceName = "USE_SHAPE_LIGHT_TYPE_1",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor ShapeLightType2 = new KeywordDescriptor()
        // {
        //     displayName = "Shape Light Type 2",
        //     referenceName = "USE_SHAPE_LIGHT_TYPE_2",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor ShapeLightType3 = new KeywordDescriptor()
        // {
        //     displayName = "Shape Light Type 3",
        //     referenceName = "USE_SHAPE_LIGHT_TYPE_3",
        //     type = KeywordType.Boolean,
        //     definition = KeywordDefinition.MultiCompile,
        //     scope = KeywordScope.Global,
        // };

        // public static readonly KeywordDescriptor UseLegacySpriteBlocks = new KeywordDescriptor()
        // {
        //     displayName = "UseLegacySpriteBlocks",
        //     referenceName = "USELEGACYSPRITEBLOCKS",
        //     type = KeywordType.Boolean,
        // };

        // public static readonly KeywordDescriptor BuiltInTargetAPI = new KeywordDescriptor()
        // {
        //     displayName = "BuiltInTargetAPI",
        //     referenceName = "BUILTIN_TARGET_API",
        //     type = KeywordType.Boolean,
        // };

        // public static readonly KeywordDescriptor SceneSelectionPass = new KeywordDescriptor()
        // {
        //     displayName = "Scene Selection Pass",
        //     referenceName = "SCENESELECTIONPASS",
        //     type = KeywordType.Boolean,
        // };

        // public static readonly KeywordDescriptor ScenePickingPass = new KeywordDescriptor()
        // {
        //     displayName = "Scene Picking Pass",
        //     referenceName = "SCENEPICKINGPASS",
        //     type = KeywordType.Boolean,
        // };
    }
    #endregion

    // #region Keywords
    // static class CoreKeywords
    // {
    //     public static readonly KeywordCollection ShadowCaster = new KeywordCollection
    //     {
    //         { CoreKeywordDescriptors.CastingPunctualLightShadow },
    //     };
    // }
    // #endregion

    #region FieldDescriptors
    static class CoreFields
    {
        public static readonly FieldDescriptor UseLegacySpriteBlocks = new FieldDescriptor("BuiltIn", "UseLegacySpriteBlocks", "BUILTIN_USELEGACYSPRITEBLOCKS");
    }
    #endregion

    #region CustomInterpolators
    static class CoreCustomInterpDescriptors
    {
        public static readonly CustomInterpSubGen.Collection Common = new CustomInterpSubGen.Collection
        {
            // Custom interpolators are not explicitly defined in the SurfaceDescriptionInputs template.
            // This entry point will let us generate a block of pass-through assignments for each field.
            CustomInterpSubGen.Descriptor.MakeBlock(CustomInterpSubGen.Splice.k_spliceCopyToSDI, "output", "input"),

            // sgci_PassThroughFunc is called from BuildVaryings in Varyings.hlsl to copy custom interpolators from vertex descriptions.
            // this entry point allows for the function to be defined before it is used.
            CustomInterpSubGen.Descriptor.MakeFunc(CustomInterpSubGen.Splice.k_splicePreSurface, "CustomInterpolatorPassThroughFunc", "Varyings", "VertexDescription", "CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC", "FEATURES_GRAPH_VERTEX")
        };
    }
    #endregion
}
