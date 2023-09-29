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


namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    enum MaterialType
    {
        Lit,
        UnLit
    }

    enum WorkflowMode
    {
        Specular,
        Metallic,
    }

    enum SurfaceType
    {
        Opaque,
        Transparent,
    }

    enum ZWriteControl
    {
        Auto = 0,
        ForceEnabled = 1,
        ForceDisabled = 2
    }

    enum ZTestMode  // the values here match UnityEngine.Rendering.CompareFunction
    {
        // Disabled = 0, "Disabled" option is invalid for actual use in shaders
        Never = 1,
        Less = 2,
        Equal = 3,
        LEqual = 4,     // default for most rendering
        Greater = 5,
        NotEqual = 6,
        GEqual = 7,
        Always = 8,
    }

    enum AlphaMode
    {
        Alpha,
        Premultiply,
        Additive,
        Multiply,
    }
    public enum RenderFace
    {
        Front = 2,      // = CullMode.Back -- render front face only
        Back = 1,       // = CullMode.Front -- render back face only
        Both = 0        // = CullMode.Off -- render both faces
    }

    sealed class BuiltInTarget : Target, IHasMetadata
    {
        public override int latestVersion => 2;

        // Constants
        static readonly GUID kSourceCodeGuid = new GUID("d0f59811de3924b6ab62802eb365ef6b"); // BuiltInTarget.cs
        public const string kPipelineTag = "BuiltInPipeline";
        public const string kLitMaterialTypeTag = "\"BuiltInMaterialType\" = \"Lit\"";
        public const string kUnlitMaterialTypeTag = "\"BuiltInMaterialType\" = \"Unlit\"";
        public static readonly string[] kSharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories().Union(new string[] { "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Templates" }).ToArray();
        public const string kTemplatePath = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Templates/ShaderPass.template";

        // SubTarget
        List<SubTarget> m_SubTargets;
        List<string> m_SubTargetNames;

        int activeSubTargetIndex => m_SubTargets.IndexOf(m_ActiveSubTarget);

        [SerializeField]
        List<JsonData<JsonObject>> m_Datas = new List<JsonData<JsonObject>>();

        // View
        PopupField<string> m_SubTargetField;
        TextField m_CustomGUIField;

        [SerializeField]
        JsonData<SubTarget> m_ActiveSubTarget;

        // When checked, allows the material to control ALL surface settings (uber shader style)
        [SerializeField]
        bool m_AllowMaterialOverride = false;

        [SerializeField]
        SurfaceType m_SurfaceType = SurfaceType.Opaque;

        [SerializeField]
        ZWriteControl m_ZWriteControl = ZWriteControl.Auto;

        [SerializeField]
        ZTestMode m_ZTestMode = ZTestMode.LEqual;

        [SerializeField]
        AlphaMode m_AlphaMode = AlphaMode.Alpha;

        [SerializeField]
        RenderFace m_RenderFace = RenderFace.Front;

        [SerializeField]
        bool m_AlphaClip = false;

        [SerializeField]
        string m_CustomEditorGUI;

        internal override bool ignoreCustomInterpolators => m_ActiveSubTarget.value is BuiltInCanvasSubTarget;
        internal override int padCustomInterpolatorLimit => 4;

        public BuiltInTarget()
        {
            displayName = "Built-In";
            m_SubTargets = TargetUtils.GetSubTargets(this);
            m_SubTargetNames = m_SubTargets.Select(x => x.displayName).ToList();
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            ProcessSubTargetDatas(m_ActiveSubTarget.value);
        }

        public string renderType
        {
            get
            {
                if (surfaceType == SurfaceType.Transparent)
                    return $"{RenderType.Transparent}";
                else
                    return $"{RenderType.Opaque}";
            }
        }

        public string renderQueue
        {
            get
            {
                if (surfaceType == SurfaceType.Transparent)
                    return $"{UnityEditor.ShaderGraph.RenderQueue.Transparent}";
                else if (alphaClip)
                    return $"{UnityEditor.ShaderGraph.RenderQueue.AlphaTest}";
                else
                    return $"{UnityEditor.ShaderGraph.RenderQueue.Geometry}";
            }
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

        public SurfaceType surfaceType
        {
            get => m_SurfaceType;
            set => m_SurfaceType = value;
        }

        public ZWriteControl zWriteControl
        {
            get => m_ZWriteControl;
            set => m_ZWriteControl = value;
        }

        public ZTestMode zTestMode
        {
            get => m_ZTestMode;
            set => m_ZTestMode = value;
        }

        public AlphaMode alphaMode
        {
            get => m_AlphaMode;
            set => m_AlphaMode = value;
        }

        public RenderFace renderFace
        {
            get => m_RenderFace;
            set => m_RenderFace = value;
        }

        public bool alphaClip
        {
            get => m_AlphaClip;
            set => m_AlphaClip = value;
        }

        public string customEditorGUI
        {
            get => m_CustomEditorGUI;
            set => m_CustomEditorGUI = value;
        }

        // generally used to know if we need to build a depth pass
        public bool mayWriteDepth
        {
            get
            {
                if (allowMaterialOverride)
                {
                    // material may or may not choose to write depth... we should create the depth pass
                    return true;
                }
                else
                {
                    switch (zWriteControl)
                    {
                        case ZWriteControl.Auto:
                            return (surfaceType == SurfaceType.Opaque);
                        case ZWriteControl.ForceDisabled:
                            return false;
                        default:
                            return true;
                    }
                }
            }
        }

        public override bool IsActive()
        {
            bool isBuiltInRenderPipeline = GraphicsSettings.currentRenderPipeline == null;
            return isBuiltInRenderPipeline && activeSubTarget.IsActive();
        }

        public override bool IsNodeAllowedByTarget(Type nodeType)
        {
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
            ProcessSubTargetDatas(m_ActiveSubTarget.value);
            m_ActiveSubTarget.value.Setup(ref context);

            // Override EditorGUI
            if (!string.IsNullOrEmpty(m_CustomEditorGUI))
                context.AddCustomEditorForRenderPipeline(m_CustomEditorGUI, "");
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
            bool useCoreBlocks = !(m_ActiveSubTarget.value is UnityEditor.Rendering.Canvas.ShaderGraph.CanvasSubTarget<BuiltInTarget>);

            // Core blocks
            if (useCoreBlocks)
            {
                // Core blocks
                context.AddBlock(BlockFields.VertexDescription.Position);
                context.AddBlock(BlockFields.VertexDescription.Normal);
                context.AddBlock(BlockFields.VertexDescription.Tangent);
                context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            }

            // SubTarget blocks
            m_ActiveSubTarget.value.GetActiveBlocks(ref context);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            activeSubTarget.CollectShaderProperties(collector, generationMode);
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
            m_SubTargetField = new PopupField<string>(m_SubTargetNames, activeSubTargetIndex);
            context.AddProperty("Material", m_SubTargetField, (evt) =>
            {
                if (Equals(activeSubTargetIndex, m_SubTargetField.index))
                    return;

                registerUndo("Change Material");
                m_ActiveSubTarget = m_SubTargets[m_SubTargetField.index];
                ProcessSubTargetDatas(m_ActiveSubTarget.value);
                onChange();
            });

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

        public void AddDefaultMaterialOverrideGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            // At some point we may want to convert this to be a per-property control
            // or Unify the UX with the upcoming "lock" feature of the Material Variant properties
            context.AddProperty("Allow Material Override", new Toggle() { value = allowMaterialOverride }, (evt) =>
            {
                if (Equals(allowMaterialOverride, evt.newValue))
                    return;

                registerUndo("Change Allow Material Override");
                allowMaterialOverride = evt.newValue;
                onChange();
            });
        }

        public void GetDefaultSurfacePropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddProperty("Surface Type", new EnumField(SurfaceType.Opaque) { value = surfaceType }, (evt) =>
            {
                if (Equals(surfaceType, evt.newValue))
                    return;

                registerUndo("Change Surface");
                surfaceType = (SurfaceType)evt.newValue;
                onChange();
            });

            context.AddProperty("Blending Mode", new EnumField(AlphaMode.Alpha) { value = alphaMode }, surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(alphaMode, evt.newValue))
                    return;

                registerUndo("Change Blend");
                alphaMode = (AlphaMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Render Face", new EnumField(RenderFace.Front) { value = renderFace }, (evt) =>
            {
                if (Equals(renderFace, evt.newValue))
                    return;

                registerUndo("Change Render Face");
                renderFace = (RenderFace)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Write", new EnumField(ZWriteControl.Auto) { value = zWriteControl }, (evt) =>
            {
                if (Equals(zWriteControl, evt.newValue))
                    return;

                registerUndo("Change Depth Write Control");
                zWriteControl = (ZWriteControl)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Test", new EnumField(ZTestMode.LEqual) { value = zTestMode }, (evt) =>
            {
                if (Equals(zTestMode, evt.newValue))
                    return;

                registerUndo("Change Depth Test");
                zTestMode = (ZTestMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Alpha Clipping", new Toggle() { value = alphaClip }, (evt) =>
            {
                if (Equals(alphaClip, evt.newValue))
                    return;

                registerUndo("Change Alpha Clip");
                alphaClip = evt.newValue;
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
                    ProcessSubTargetDatas(m_ActiveSubTarget);
                    return true;
                }
            }

            return false;
        }

        public override bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline)
        {
            return scriptableRenderPipeline == null;
        }


        #region IRequiresDataSupport
        void ProcessSubTargetDatas(SubTarget subTarget)
        {
            var typeCollection = TypeCache.GetTypesDerivedFrom<JsonObject>();
            foreach (var type in typeCollection)
            {
                if (type.IsGenericType)
                    continue;

                // Data requirement interfaces need generic type arguments
                // Therefore we need to use reflections to call the method
                var methodInfo = typeof(BuiltInTarget).GetMethod(nameof(SetDataOnSubTarget));
                var genericMethodInfo = methodInfo.MakeGenericMethod(type);
                genericMethodInfo.Invoke(this, new object[] { subTarget });
            }
        }
        void ClearUnusedData()
        {
            for (int i = 0; i < m_Datas.Count; i++)
            {
                var data = m_Datas[i];
                var type = data.value.GetType();

                // Data requirement interfaces need generic type arguments
                // Therefore we need to use reflections to call the method
                var methodInfo = typeof(BuiltInTarget).GetMethod(nameof(ValidateDataForSubTarget));
                var genericMethodInfo = methodInfo.MakeGenericMethod(type);
                genericMethodInfo.Invoke(this, new object[] { m_ActiveSubTarget.value, data.value });
            }
        }

        public void SetDataOnSubTarget<T>(SubTarget subTarget) where T : JsonObject
        {
            if (!(subTarget is IRequiresData<T> requiresData))
                return;

            // Ensure data object exists in list
            var data = m_Datas.SelectValue().FirstOrDefault(x => x.GetType().Equals(typeof(T))) as T;
            if (data == null)
            {
                data = Activator.CreateInstance(typeof(T)) as T;
                m_Datas.Add(data);
            }

            // Apply data object to SubTarget
            requiresData.data = data;
        }

        public void ValidateDataForSubTarget<T>(SubTarget subTarget, T data) where T : JsonObject
        {
            if (!(subTarget is IRequiresData<T> requiresData))
            {
                m_Datas.Remove(data);
            }
        }

        public override void OnBeforeSerialize()
        {
            ClearUnusedData();
        }
        #endregion


        public override void OnAfterDeserialize(string json)
        {
            base.OnAfterDeserialize(json);

            if (this.sgVersion < latestVersion)
            {
                // Version 0 didn't have m_AllowMaterialOverride but acted as if it was true
                if (this.sgVersion <= 1)
                {
                    this.m_AllowMaterialOverride = true;
                }
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

        ScriptableObject IHasMetadata.GetMetadataObject(GraphDataReadOnly graph)
        {
            // defer to subtarget
            if (m_ActiveSubTarget.value is IHasMetadata subTargetHasMetaData)
                return subTargetHasMetaData.GetMetadataObject(graph);
            return null;
        }

        #endregion
    }

    #region Passes
    static class CorePasses
    {
        internal static void AddSurfaceTypeControlToPass(ref PassDescriptor pass, BuiltInTarget target)
        {
            if (target.allowMaterialOverride)
            {
                pass.keywords.Add(CoreKeywordDescriptors.SurfaceTypeTransparent);
            }
            else if (target.surfaceType == SurfaceType.Transparent)
            {
                pass.defines.Add(CoreKeywordDescriptors.SurfaceTypeTransparent, 1);
            }
        }

        internal static void AddAlphaPremultiplyControlToPass(ref PassDescriptor pass, BuiltInTarget target)
        {
            if (target.allowMaterialOverride)
            {
                pass.keywords.Add(CoreKeywordDescriptors.AlphaPremultiplyOn);
            }
            else if (target.alphaMode == AlphaMode.Premultiply)
            {
                pass.defines.Add(CoreKeywordDescriptors.AlphaPremultiplyOn, 1);
            }
        }

        internal static void AddAlphaClipControlToPass(ref PassDescriptor pass, BuiltInTarget target)
        {
            if (target.allowMaterialOverride)
            {
                pass.keywords.Add(CoreKeywordDescriptors.AlphaClip);
                pass.keywords.Add(CoreKeywordDescriptors.AlphaTestOn);
            }
            else if (target.alphaClip)
            {
                pass.defines.Add(CoreKeywordDescriptors.AlphaClip, 1);
                pass.defines.Add(CoreKeywordDescriptors.AlphaTestOn, 1);
            }
        }

        internal static void AddTargetSurfaceControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
        {
            AddSurfaceTypeControlToPass(ref pass, target);
            AddAlphaPremultiplyControlToPass(ref pass, target);
            AddAlphaClipControlToPass(ref pass, target);
        }

        internal static void AddCommonPassSurfaceControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
        {
            AddSurfaceTypeControlToPass(ref pass, target);
            AddAlphaClipControlToPass(ref pass, target);
        }

        public static PassDescriptor DepthOnly(BuiltInTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "DepthOnly",
                useInPreview = true,

                // Template
                passTemplatePath = BuiltInTarget.kTemplatePath,
                sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.DepthOnly(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
                keywords = new KeywordCollection(),
                includes = CoreIncludes.DepthOnly,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);

            return result;
        }

        public static PassDescriptor ShadowCaster(BuiltInTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWCASTER",
                lightMode = "ShadowCaster",

                // Template
                passTemplatePath = BuiltInTarget.kTemplatePath,
                sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.ShadowCaster,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.ShadowCaster(target),
                pragmas = CorePragmas.ShadowCaster,
                defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
                keywords = new KeywordCollection { CoreKeywords.ShadowCaster },
                includes = CoreIncludes.ShadowCaster,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddCommonPassSurfaceControlsToPass(ref result, target);

            return result;
        }

        public static PassDescriptor SceneSelection(BuiltInTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SceneSelectionPass",
                lightMode = "SceneSelectionPass",
                useInPreview = true,

                // Template
                passTemplatePath = BuiltInTarget.kTemplatePath,
                sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.SceneSelection(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection { CoreDefines.SceneSelection },
                keywords = new KeywordCollection(),
                includes = CoreIncludes.SceneSelection,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddCommonPassSurfaceControlsToPass(ref result, target);

            return result;
        }

        public static PassDescriptor ScenePicking(BuiltInTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "ScenePickingPass",
                referenceName = "ScenePickingPass",
                lightMode = "Picking",
                useInPreview = true,

                // Template
                passTemplatePath = BuiltInTarget.kTemplatePath,
                sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.ScenePicking(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection { CoreDefines.ScenePicking },
                keywords = new KeywordCollection(),
                includes = CoreIncludes.ScenePicking,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddCommonPassSurfaceControlsToPass(ref result, target);

            return result;
        }
    }
    #endregion

    #region PortMasks
    class CoreBlockMasks
    {
        public static readonly BlockFieldDescriptor[] Vertex = new BlockFieldDescriptor[]
        {
            BlockFields.VertexDescription.Position,
            BlockFields.VertexDescription.Normal,
            BlockFields.VertexDescription.Tangent,
        };

        public static readonly BlockFieldDescriptor[] FragmentAlphaOnly = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
        };

        public static readonly BlockFieldDescriptor[] FragmentColorAlpha = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.BaseColor,
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
        };
    }
    #endregion

    #region StructCollections
    static class CoreStructCollections
    {
        public static readonly StructCollection Default = new StructCollection
        {
            { Structs.Attributes },
            { BuiltInStructs.Varyings },
            { Structs.SurfaceDescriptionInputs },
            { Structs.VertexDescriptionInputs },
        };
    }
    #endregion

    #region RequiredFields
    static class CoreRequiredFields
    {
        public static readonly FieldCollection ShadowCaster = new FieldCollection()
        {
            StructFields.Attributes.normalOS,
        };
    }
    #endregion

    #region FieldDependencies
    static class CoreFieldDependencies
    {
        public static readonly DependencyCollection Default = new DependencyCollection()
        {
            { FieldDependencies.Default },
            new FieldDependency(BuiltInStructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,    StructFields.Attributes.instanceID),
            new FieldDependency(BuiltInStructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,     StructFields.Attributes.instanceID),
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

        public static ZWrite ZWriteControlToZWrite(ZWriteControl zWriteControl, SurfaceType surfaceType)
        {
            if (zWriteControl == ZWriteControl.ForceEnabled)
                return ZWrite.On;
            else if (zWriteControl == ZWriteControl.ForceDisabled)
                return ZWrite.Off;
            else // ZWriteControl.Auto
            {
                if (surfaceType == SurfaceType.Opaque)
                    return ZWrite.On;
                else
                    return ZWrite.Off;
            }
        }

        public static void AddUberSwitchedZTest(BuiltInTarget target, RenderStateCollection renderStates)
        {
            if (target.allowMaterialOverride)
                renderStates.Add(RenderState.ZTest(Uniforms.zTest));
            else
                renderStates.Add(RenderState.ZTest(target.zTestMode.ToString()));
        }

        public static void AddUberSwitchedZWrite(BuiltInTarget target, RenderStateCollection renderStates)
        {
            if (target.allowMaterialOverride)
                renderStates.Add(RenderState.ZWrite(Uniforms.zWrite));
            else
                renderStates.Add(RenderState.ZWrite(ZWriteControlToZWrite(target.zWriteControl, target.surfaceType)));
        }

        public static void AddUberSwitchedCull(BuiltInTarget target, RenderStateCollection renderStates)
        {
            if (target.allowMaterialOverride)
                renderStates.Add(RenderState.Cull(Uniforms.cullMode));
            else
                renderStates.Add(RenderState.Cull(RenderFaceToCull(target.renderFace)));
        }

        public static void AddUberSwitchedBlend(BuiltInTarget target, RenderStateCollection renderStates)
        {
            if (target.allowMaterialOverride)
            {
                renderStates.Add(RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend));
            }
            else
            {
                if (target.surfaceType == SurfaceType.Opaque)
                {
                    renderStates.Add(RenderState.Blend(Blend.One, Blend.Zero));
                }
                else
                {
                    if (target.alphaMode == AlphaMode.Alpha)
                        renderStates.Add(RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                    else if (target.alphaMode == AlphaMode.Premultiply)
                        renderStates.Add(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                    else if (target.alphaMode == AlphaMode.Additive)
                        renderStates.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One, Blend.One, Blend.One));
                    else if (target.alphaMode == AlphaMode.Multiply)
                        renderStates.Add(RenderState.Blend(Blend.DstColor, Blend.Zero));
                }
            }
        }

        public static readonly RenderStateCollection MaterialControlledDefault = new RenderStateCollection
        {
            { RenderState.ZTest(Uniforms.zTest) },
            { RenderState.ZWrite(Uniforms.zWrite) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend) },
        };

        public static RenderStateCollection Default(BuiltInTarget target)
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
                if (target.surfaceType != SurfaceType.Opaque)
                    result.Add(RenderState.ColorMask("ColorMask RGB"));
                return result;
            }
        }

        public static RenderStateCollection ForwardAdd(BuiltInTarget target)
        {
            var result = new RenderStateCollection();

            result.Add(RenderState.ZWrite(ZWrite.Off));
            if (target.surfaceType != SurfaceType.Opaque)
            {
                result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One));
                result.Add(RenderState.ColorMask("ColorMask RGB"));
            }
            else
            {
                result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One, Blend.One, Blend.One));
            }
            return result;
        }

        public static readonly RenderStateCollection Meta = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off) },
        };

        public static RenderStateCollection ShadowCaster(BuiltInTarget target)
        {
            var result = new RenderStateCollection();
            result.Add(RenderState.ZTest(ZTest.LEqual));
            result.Add(RenderState.ZWrite(ZWrite.On));
            AddUberSwitchedCull(target, result);
            AddUberSwitchedBlend(target, result);
            result.Add(RenderState.ColorMask("ColorMask 0"));
            return result;
        }

        public static RenderStateCollection DepthOnly(BuiltInTarget target)
        {
            var result = new RenderStateCollection();
            result.Add(RenderState.ZTest(ZTest.LEqual));
            result.Add(RenderState.ZWrite(ZWrite.On));
            AddUberSwitchedCull(target, result);
            AddUberSwitchedBlend(target, result);
            result.Add(RenderState.ColorMask("ColorMask 0"));
            return result;
        }

        public static RenderStateCollection SceneSelection(BuiltInTarget target)
        {
            var result = new RenderStateCollection()
            {
                { RenderState.Cull(Cull.Off) }
            };
            return result;
        }

        public static RenderStateCollection ScenePicking(BuiltInTarget target)
        {
            var result = new RenderStateCollection();
            AddUberSwitchedCull(target, result);
            return result;
        }
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

        public static readonly PragmaCollection Instanced = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target30) },
            { Pragma.MultiCompileInstancing },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Forward = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target30) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.MultiCompileForwardBase },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection ForwardAdd = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target30) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.MultiCompileForwardAddFullShadowsBase },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Deferred = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.MultiCompileInstancing },
            { new PragmaDescriptor { value = "exclude_renderers nomrt" } },
            { Pragma.MultiCompilePrePassFinal },
            { Pragma.SkipVariants(new[] {"FOG_LINEAR", "FOG_EXP", "FOG_EXP2" }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection ShadowCaster = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target30) },
            { Pragma.MultiCompileShadowCaster },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };
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
    static class CoreDefines
    {
        public static readonly DefineCollection UseLegacySpriteBlocks = new DefineCollection
        {
            { CoreKeywordDescriptors.UseLegacySpriteBlocks, 1, new FieldCondition(CoreFields.UseLegacySpriteBlocks, true) },
        };
        public static readonly DefineCollection BuiltInTargetAPI = new DefineCollection
        {
            { CoreKeywordDescriptors.BuiltInTargetAPI, 1 },
        };
        public static readonly DefineCollection SceneSelection = new DefineCollection
        {
            { CoreKeywordDescriptors.BuiltInTargetAPI, 1 },
            { CoreKeywordDescriptors.SceneSelectionPass, 1 },
        };
        public static readonly DefineCollection ScenePicking = new DefineCollection
        {
            { CoreKeywordDescriptors.BuiltInTargetAPI, 1 },
            { CoreKeywordDescriptors.ScenePickingPass, 1 },
        };
    }
    #endregion

    #region KeywordDescriptors

    static class CoreKeywordDescriptors
    {
        public static readonly KeywordDescriptor Lightmap = new KeywordDescriptor()
        {
            displayName = "Lightmap",
            referenceName = "LIGHTMAP_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor DirectionalLightmapCombined = new KeywordDescriptor()
        {
            displayName = "Directional Lightmap Combined",
            referenceName = "DIRLIGHTMAP_COMBINED",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor SampleGI = new KeywordDescriptor()
        {
            displayName = "Sample GI",
            referenceName = "_SAMPLE_GI",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor AlphaTestOn = new KeywordDescriptor()
        {
            displayName = Keyword.SG_AlphaTestOn,
            referenceName = Keyword.SG_AlphaTestOn,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor AlphaClip = new KeywordDescriptor()
        {
            displayName = "Alpha Clipping",
            referenceName = Keyword.SG_AlphaClip,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor SurfaceTypeTransparent = new KeywordDescriptor()
        {
            displayName = Keyword.SG_SurfaceTypeTransparent,
            referenceName = Keyword.SG_SurfaceTypeTransparent,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor AlphaPremultiplyOn = new KeywordDescriptor()
        {
            displayName = Keyword.SG_AlphaPremultiplyOn,
            referenceName = Keyword.SG_AlphaPremultiplyOn,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor MainLightShadows = new KeywordDescriptor()
        {
            displayName = "Main Light Shadows",
            referenceName = "",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "" },
                new KeywordEntry() { displayName = "No Cascade", referenceName = "MAIN_LIGHT_SHADOWS" },
                new KeywordEntry() { displayName = "Cascade", referenceName = "MAIN_LIGHT_SHADOWS_CASCADE" },
                new KeywordEntry() { displayName = "Screen", referenceName = "MAIN_LIGHT_SHADOWS_SCREEN" },
            }
        };

        public static readonly KeywordDescriptor CastingPunctualLightShadow = new KeywordDescriptor()
        {
            displayName = "Casting Punctual Light Shadow",
            referenceName = "_CASTING_PUNCTUAL_LIGHT_SHADOW",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor AdditionalLights = new KeywordDescriptor()
        {
            displayName = "Additional Lights",
            referenceName = "_ADDITIONAL",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Vertex", referenceName = "LIGHTS_VERTEX" },
                new KeywordEntry() { displayName = "Fragment", referenceName = "LIGHTS" },
                new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
            }
        };

        public static readonly KeywordDescriptor AdditionalLightShadows = new KeywordDescriptor()
        {
            displayName = "Additional Light Shadows",
            referenceName = "_ADDITIONAL_LIGHT_SHADOWS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor ShadowsSoft = new KeywordDescriptor()
        {
            displayName = "Shadows Soft",
            referenceName = "_SHADOWS_SOFT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor MixedLightingSubtractive = new KeywordDescriptor()
        {
            displayName = "Mixed Lighting Subtractive",
            referenceName = "_MIXED_LIGHTING_SUBTRACTIVE",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor LightmapShadowMixing = new KeywordDescriptor()
        {
            displayName = "Lightmap Shadow Mixing",
            referenceName = "LIGHTMAP_SHADOW_MIXING",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor ShadowsShadowmask = new KeywordDescriptor()
        {
            displayName = "Shadows Shadowmask",
            referenceName = "SHADOWS_SHADOWMASK",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor SmoothnessChannel = new KeywordDescriptor()
        {
            displayName = "Smoothness Channel",
            referenceName = "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor ShapeLightType0 = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 0",
            referenceName = "USE_SHAPE_LIGHT_TYPE_0",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor ShapeLightType1 = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 1",
            referenceName = "USE_SHAPE_LIGHT_TYPE_1",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor ShapeLightType2 = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 2",
            referenceName = "USE_SHAPE_LIGHT_TYPE_2",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor ShapeLightType3 = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 3",
            referenceName = "USE_SHAPE_LIGHT_TYPE_3",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor UseLegacySpriteBlocks = new KeywordDescriptor()
        {
            displayName = "UseLegacySpriteBlocks",
            referenceName = "USELEGACYSPRITEBLOCKS",
            type = KeywordType.Boolean,
        };

        public static readonly KeywordDescriptor BuiltInTargetAPI = new KeywordDescriptor()
        {
            displayName = "BuiltInTargetAPI",
            referenceName = "BUILTIN_TARGET_API",
            type = KeywordType.Boolean,
        };

        public static readonly KeywordDescriptor SceneSelectionPass = new KeywordDescriptor()
        {
            displayName = "Scene Selection Pass",
            referenceName = "SCENESELECTIONPASS",
            type = KeywordType.Boolean,
        };

        public static readonly KeywordDescriptor ScenePickingPass = new KeywordDescriptor()
        {
            displayName = "Scene Picking Pass",
            referenceName = "SCENEPICKINGPASS",
            type = KeywordType.Boolean,
        };
    }
    #endregion

    #region Keywords
    static class CoreKeywords
    {
        public static readonly KeywordCollection ShadowCaster = new KeywordCollection
        {
            { CoreKeywordDescriptors.CastingPunctualLightShadow },
        };
    }
    #endregion

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
