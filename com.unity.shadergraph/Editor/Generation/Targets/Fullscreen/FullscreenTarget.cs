using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using UnityEditor.Rendering.BuiltIn;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using BlendMode = UnityEngine.Rendering.BlendMode;
using BlendOp = UnityEditor.ShaderGraph.BlendOp;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Fullscreen.ShaderGraph
{
    sealed class FullscreenTarget : Target, IHasMetadata, IMaySupportVFX
    {
        [GenerateBlocks]
        public struct Blocks
        {
            // TODO: add optional depth write block
            public static BlockFieldDescriptor Color = new BlockFieldDescriptor(BlockFields.SurfaceDescription.name, "Color", "Color",
                "SURFACEDESCRIPTION_COLOR", new ColorRGBAControl(UnityEngine.Color.grey), ShaderStage.Fragment);
            public static BlockFieldDescriptor Depth = new BlockFieldDescriptor(BlockFields.SurfaceDescription.name, "Depth", "Depth",
                "SURFACEDESCRIPTION_DEPTH", new FloatControl(0), ShaderStage.Fragment);

        }

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

        public static class Uniforms
        {
            public static readonly string srcColorBlendProperty = "_Fullscreen_SrcColorBlend";
            public static readonly string dstColorBlendProperty = "_Fullscreen_DstColorBlend";
            public static readonly string srcAlphaBlendProperty = "_Fullscreen_SrcAlphaBlend";
            public static readonly string dstAlphaBlendProperty = "_Fullscreen_DstAlphaBlend";
            public static readonly string colorBlendOperationProperty = "_Fullscreen_ColorBlendOperation";
            public static readonly string alphaBlendOperationProperty = "_Fullscreen_AlphaBlendOperation";
            public static readonly string depthWriteProperty = "_Fullscreen_DepthWrite";
            public static readonly string depthTestProperty = "_Fullscreen_DepthTest";
            public static readonly string stencilReferenceProperty = "_Fullscreen_StencilReference";
            public static readonly string stencilReadMaskProperty = "_Fullscreen_StencilReadMask";
            public static readonly string stencilWriteMaskProperty = "_Fullscreen_StencilWriteMask";
            public static readonly string stencilComparisonProperty = "_Fullscreen_StencilComparison";
            public static readonly string stencilPassProperty = "_Fullscreen_StencilPass";
            public static readonly string stencilFailProperty = "_Fullscreen_StencilFail";
            public static readonly string stencilDepthFailProperty = "_Fullscreen_StencilDepthFail";

            public static readonly string srcColorBlend = "[" + srcColorBlendProperty + "]";
            public static readonly string dstColorBlend = "[" + dstColorBlendProperty + "]";
            public static readonly string srcAlphaBlend = "[" + srcAlphaBlendProperty + "]";
            public static readonly string dstAlphaBlend = "[" + dstAlphaBlendProperty + "]";
            public static readonly string colorBlendOperation = "[" + colorBlendOperationProperty + "]";
            public static readonly string alphaBlendOperation = "[" + alphaBlendOperationProperty + "]";
            public static readonly string depthWrite = "[" + depthWriteProperty + "]";
            public static readonly string depthTest = "[" + depthTestProperty + "]";
            public static readonly string stencilReference = "[" + stencilReferenceProperty + "]";
            public static readonly string stencilReadMask = "[" + stencilReadMaskProperty + "]";
            public static readonly string stencilWriteMask = "[" + stencilWriteMaskProperty + "]";
            public static readonly string stencilComparison = "[" + stencilComparisonProperty + "]";
            public static readonly string stencilPass = "[" + stencilPassProperty + "]";
            public static readonly string stencilFail = "[" + stencilFailProperty + "]";
            public static readonly string stencilDepthFail = "[" + stencilDepthFailProperty + "]";
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
        Blend m_SrcColorBlendMode = Blend.Zero;
        [SerializeField]
        Blend m_DstColorBlendMode = Blend.One;
        [SerializeField]
        BlendOp m_ColorBlendOperation = BlendOp.Add;

        [SerializeField]
        Blend m_SrcAlphaBlendMode = Blend.Zero;
        [SerializeField]
        Blend m_DstAlphaBlendMode = Blend.One;
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
            displayName = "Fullscreen";
            m_SubTargets = TargetUtils.GetSubTargets(this);
            m_SubTargetNames = m_SubTargets.Select(x => x.displayName).ToList();
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
        }

        public FullscreenBlendMode blendMode
        {
            get => m_BlendMode;
            set => m_BlendMode = value;
        }

        public Blend srcColorBlendMode
        {
            get => m_SrcColorBlendMode;
            set => m_SrcColorBlendMode = value;
        }

        public Blend dstColorBlendMode
        {
            get => m_DstColorBlendMode;
            set => m_DstColorBlendMode = value;
        }

        public BlendOp colorBlendOperation
        {
            get => m_ColorBlendOperation;
            set => m_ColorBlendOperation = value;
        }

        public Blend srcAlphaBlendMode
        {
            get => m_SrcAlphaBlendMode;
            set => m_SrcAlphaBlendMode = value;
        }

        public Blend dstAlphaBlendMode
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
            // context.AddField(Fields.GraphVertex); // We don't support custom vertex functions for now
            context.AddField(Fields.GraphPixel);

            // SubTarget fields
            m_ActiveSubTarget.value.GetFields(ref context);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Core blocks
            context.AddBlock(Blocks.Color);
            context.AddBlock(Blocks.Depth, depthWrite);

            // SubTarget blocks
            m_ActiveSubTarget.value.GetActiveBlocks(ref context);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            activeSubTarget.CollectShaderProperties(collector, generationMode);
        }

        public void CollectRenderStateShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {

            if (generationMode == GenerationMode.Preview || allowMaterialOverride)
            {
                collector.AddEnumProperty(Uniforms.srcColorBlendProperty, srcColorBlendMode);
                collector.AddEnumProperty(Uniforms.dstColorBlendProperty, dstColorBlendMode);
                collector.AddEnumProperty(Uniforms.srcAlphaBlendProperty, srcAlphaBlendMode);
                collector.AddEnumProperty(Uniforms.dstAlphaBlendProperty, dstAlphaBlendMode);
                collector.AddEnumProperty(Uniforms.colorBlendOperationProperty, colorBlendOperation);
                collector.AddEnumProperty(Uniforms.alphaBlendOperationProperty, alphaBlendOperation);
                collector.AddFloatProperty(Uniforms.depthWriteProperty, depthWrite ? 1 : 0);
                collector.AddFloatProperty(Uniforms.depthTestProperty, (float)depthTestMode);
                collector.AddIntProperty(Uniforms.stencilReferenceProperty, stencilReference);
                collector.AddIntProperty(Uniforms.stencilReadMaskProperty, stencilReadMask);
                collector.AddIntProperty(Uniforms.stencilWriteMaskProperty, stencilWriteMask);
                collector.AddEnumProperty(Uniforms.stencilComparisonProperty, stencilCompareFunction);
                collector.AddEnumProperty(Uniforms.stencilPassProperty, stencilPassOperation);
                collector.AddEnumProperty(Uniforms.stencilFailProperty, stencilFailOperation);
                collector.AddEnumProperty(Uniforms.stencilDepthFailProperty, stencilDepthTestFailOperation);
            }
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
                    srcColorBlendMode = (Blend)evt.newValue;
                    onChange();
                });
                context.AddProperty("Dst Color", new EnumField(dstColorBlendMode) { value = dstColorBlendMode }, (evt) =>
                {
                    if (Equals(dstColorBlendMode, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    dstColorBlendMode = (Blend)evt.newValue;
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
                    srcAlphaBlendMode = (Blend)evt.newValue;
                    onChange();
                });
                context.AddProperty("Dst", new EnumField(dstAlphaBlendMode) { value = dstAlphaBlendMode }, (evt) =>
                {
                    if (Equals(dstAlphaBlendMode, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    dstAlphaBlendMode = (Blend)evt.newValue;
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

        public bool SupportsVFX() => false;
        public bool CanSupportVFX() => false;

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

        public static StructDescriptor Varyings = new StructDescriptor()
        {
            name = "Varyings",
            packFields = true,
            populateWithCustomInterpolators = false,
            fields = new FieldDescriptor[]
            {
                StructFields.Varyings.positionCS,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.instanceID,
                BuiltInStructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,
                BuiltInStructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,
            }
        };

        public static readonly RenderStateCollection MaterialControlledDefault = new RenderStateCollection
        {
            { RenderState.ZTest(Uniforms.depthTest) },
            { RenderState.ZWrite(Uniforms.depthWrite) },
            { RenderState.Blend(Uniforms.srcColorBlend, Uniforms.dstColorBlend, Uniforms.srcAlphaBlend, Uniforms.dstAlphaBlend) },
            { RenderState.BlendOp(Uniforms.colorBlendOperation, Uniforms.alphaBlendOperation) },
            // TODO: Add stencil read mask!
            { RenderState.Stencil(new StencilDescriptor{ Ref = Uniforms.stencilReference, WriteMask = Uniforms.stencilWriteMask, Comp = Uniforms.stencilComparison, ZFail = Uniforms.stencilDepthFail, Fail = Uniforms.stencilFail, Pass = Uniforms.stencilPass}) }
        };

        public RenderStateCollection GetRenderState()
        {
            if (allowMaterialOverride)
                return MaterialControlledDefault;
            else
            {
                var result = new RenderStateCollection();
                result.Add(RenderState.ZTest(depthTestMode.ToString()));
                result.Add(RenderState.ZWrite(depthWrite.ToString()));

                // Blend mode
                if (blendMode == FullscreenBlendMode.Alpha)
                    result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                else if (blendMode == FullscreenBlendMode.Premultiply)
                    result.Add(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                else if (blendMode == FullscreenBlendMode.Additive)
                    result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One, Blend.One, Blend.One));
                else if (blendMode == FullscreenBlendMode.Multiply)
                    result.Add(RenderState.Blend(Blend.DstColor, Blend.Zero));
                else
                {
                    result.Add(RenderState.Blend(srcColorBlendMode, dstColorBlendMode, srcAlphaBlendMode, dstAlphaBlendMode));
                    result.Add(RenderState.BlendOp(colorBlendOperation, alphaBlendOperation));
                }

                result.Add(RenderState.Stencil(new StencilDescriptor
                {
                    Ref = stencilReference.ToString(),
                    WriteMask = stencilWriteMask.ToString(),
                    Comp = stencilCompareFunction.ToString(),
                    ZFail = stencilDepthTestFailOperation.ToString(),
                    Fail = stencilFailOperation.ToString(),
                    Pass = stencilPassOperation.ToString(),
                }));
                return result;
            }
        }
    }

    #region Includes
    static class CoreIncludes
    {
        const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
        const string kTexture = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl";
        // TODO: Add shadergraph functions support (replace functions.hlsl by SGFunctions)
        const string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
        const string kCommon = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl";
        const string kInstancing = "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl";
        // const string kCore = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl";
        // const string kLighting = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl";
        // const string kGraphFunctions = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl";
        // const string kVaryings = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl";
        // const string kShaderPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl";
        // const string kDepthOnlyPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl";
        // const string kShadowCasterPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl";

        // TODO: support SH
        // const string kShims = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl";

        public static readonly IncludeCollection CorePregraph = new IncludeCollection
        {
            // { kShims, IncludeLocation.Pregraph },
            { kColor, IncludeLocation.Pregraph },
            // { kCore, IncludeLocation.Pregraph },
            { kTexture, IncludeLocation.Pregraph },
            { kFunctions, IncludeLocation.Pregraph },
            { kCommon, IncludeLocation.Pregraph },
            { kInstancing, IncludeLocation.Pregraph }, // For VR
        };

        public static readonly IncludeCollection ShaderGraphPregraph = new IncludeCollection
        {
            // { kGraphFunctions, IncludeLocation.Pregraph },
        };

        // public static readonly IncludeCollection CorePostgraph = new IncludeCollection
        // {
        //     { kVaryings, IncludeLocation.Postgraph },
        // };
    }
    #endregion

    internal static class FullscreenPropertyCollectorExtension
    {
        public static void AddEnumProperty<T>(this PropertyCollector collector, string prop, T value, HLSLDeclaration hlslDeclaration = HLSLDeclaration.DoNotDeclare) where T : Enum
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Enum,
                enumType = EnumType.Enum,
                cSharpEnumType = typeof(T),
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = hlslDeclaration,
                value = Convert.ToInt32(value),
                overrideReferenceName = prop,
            });
        }

        public static void AddIntProperty(this PropertyCollector collector, string prop, int value, HLSLDeclaration hlslDeclaration = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Integer,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = hlslDeclaration,
                value = value,
                overrideReferenceName = prop,
            });
        }
    }

}
