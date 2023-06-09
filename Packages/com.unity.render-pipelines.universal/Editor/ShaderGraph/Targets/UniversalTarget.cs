using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.UIElements;
using UnityEditor.ShaderGraph.Serialization;
using UnityEditor.ShaderGraph.Legacy;
#if HAS_VFX_GRAPH
using UnityEditor.VFX;
#endif

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    /// <summary>
    /// Options for the material type.
    /// </summary>
    public enum MaterialType
    {
        /// <summary>
        /// Use this for URP lit.
        /// </summary>
        Lit,

        /// <summary>
        /// Use this for URP unlit.
        /// </summary>
        Unlit,

        /// <summary>
        /// Use this for sprite lit.
        /// </summary>
        SpriteLit,

        /// <summary>
        /// Use this for Sprite unlit.
        /// </summary>
        SpriteUnlit,
    }

    /// <summary>
    /// Workflow modes for the shader.
    /// </summary>
    public enum WorkflowMode
    {
        /// <summary>
        /// Use this for specular workflow.
        /// </summary>
        Specular,

        /// <summary>
        /// Use this for metallic workflow.
        /// </summary>
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
        Disabled = 0,
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

    internal enum RenderFace
    {
        Front = 2,      // = CullMode.Back -- render front face only
        Back = 1,       // = CullMode.Front -- render back face only
        Both = 0        // = CullMode.Off -- render both faces
    }

    sealed class UniversalTarget : Target, IHasMetadata, ILegacyTarget
#if HAS_VFX_GRAPH
        , IMaySupportVFX, IRequireVFXContext
#endif
    {
        public override int latestVersion => 1;

        // Constants
        static readonly GUID kSourceCodeGuid = new GUID("8c72f47fdde33b14a9340e325ce56f4d"); // UniversalTarget.cs
        public const string kPipelineTag = "UniversalPipeline";
        public const string kLitMaterialTypeTag = "\"UniversalMaterialType\" = \"Lit\"";
        public const string kUnlitMaterialTypeTag = "\"UniversalMaterialType\" = \"Unlit\"";
        public static readonly string[] kSharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories().Union(new string[]
        {
            "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Templates"
#if HAS_VFX_GRAPH
            , "Packages/com.unity.visualeffectgraph/Editor/ShaderGraph/Templates"
#endif
        }).ToArray();
        public const string kUberTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Templates/ShaderPass.template";

        // SubTarget
        List<SubTarget> m_SubTargets;
        List<string> m_SubTargetNames;
        int activeSubTargetIndex => m_SubTargets.IndexOf(m_ActiveSubTarget);

        // Subtarget Data
        [SerializeField]
        List<JsonData<JsonObject>> m_Datas = new List<JsonData<JsonObject>>();

        // View
        PopupField<string> m_SubTargetField;
        TextField m_CustomGUIField;
#if HAS_VFX_GRAPH
        Toggle m_SupportVFXToggle;
#endif

        [SerializeField]
        JsonData<SubTarget> m_ActiveSubTarget;

        // when checked, allows the material to control ALL surface settings (uber shader style)
        [SerializeField]
        bool m_AllowMaterialOverride = false;

        [SerializeField]
        SurfaceType m_SurfaceType = SurfaceType.Opaque;

        [SerializeField]
        ZTestMode m_ZTestMode = ZTestMode.LEqual;

        [SerializeField]
        ZWriteControl m_ZWriteControl = ZWriteControl.Auto;

        [SerializeField]
        AlphaMode m_AlphaMode = AlphaMode.Alpha;

        [SerializeField]
        RenderFace m_RenderFace = RenderFace.Front;

        [SerializeField]
        bool m_AlphaClip = false;

        [SerializeField]
        bool m_CastShadows = true;

        [SerializeField]
        bool m_ReceiveShadows = true;

        [SerializeField]
        bool m_SupportsLODCrossFade = false;

        [SerializeField]
        string m_CustomEditorGUI;

        [SerializeField]
        bool m_SupportVFX;

        internal override bool ignoreCustomInterpolators => false;
        internal override int padCustomInterpolatorLimit => 4;
        internal override bool prefersSpritePreview =>
            activeSubTarget is UniversalSpriteUnlitSubTarget or UniversalSpriteLitSubTarget or
                               UniversalSpriteCustomLitSubTarget;

        public UniversalTarget()
        {
            displayName = "Universal";
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

        // this sets up the default renderQueue -- but it can be overridden by ResetMaterialKeywords()
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

        public string disableBatching
        {
            get
            {
                if (supportsLodCrossFade)
                    return $"{UnityEditor.ShaderGraph.DisableBatching.LODFading}";
                else
                    return $"{UnityEditor.ShaderGraph.DisableBatching.False}";
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

        public bool castShadows
        {
            get => m_CastShadows;
            set => m_CastShadows = value;
        }

        public bool receiveShadows
        {
            get => m_ReceiveShadows;
            set => m_ReceiveShadows = value;
        }

        public bool supportsLodCrossFade
        {
            get => m_SupportsLODCrossFade;
            set => m_SupportsLODCrossFade = value;
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
            bool isUniversalRenderPipeline = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;
            return isUniversalRenderPipeline && activeSubTarget.IsActive();
        }

        public override bool IsNodeAllowedByTarget(Type nodeType)
        {
            SRPFilterAttribute srpFilter = NodeClassCache.GetAttributeOnNodeType<SRPFilterAttribute>(nodeType);
            bool worksWithThisSrp = srpFilter == null || srpFilter.srpTypes.Contains(typeof(UniversalRenderPipeline));

            SubTargetFilterAttribute subTargetFilter = NodeClassCache.GetAttributeOnNodeType<SubTargetFilterAttribute>(nodeType);
            bool worksWithThisSubTarget = subTargetFilter == null || subTargetFilter.subTargetTypes.Contains(activeSubTarget.GetType());

            if (activeSubTarget.IsActive())
                worksWithThisSubTarget &= activeSubTarget.IsNodeAllowedBySubTarget(nodeType);

            return worksWithThisSrp && worksWithThisSubTarget && base.IsNodeAllowedByTarget(nodeType);
        }

        public override void Setup(ref TargetSetupContext context)
        {
            // Setup the Target
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);

            // Override EditorGUI (replaces the URP material editor by a custom one)
            if (!string.IsNullOrEmpty(m_CustomEditorGUI))
                context.AddCustomEditorForRenderPipeline(m_CustomEditorGUI, typeof(UniversalRenderPipelineAsset));

            // Setup the active SubTarget
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            m_ActiveSubTarget.value.target = this;
            ProcessSubTargetDatas(m_ActiveSubTarget.value);
            m_ActiveSubTarget.value.Setup(ref context);
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            m_ActiveSubTarget.value.target = this;

            // OnAfterMultiDeserialize order is not guaranteed to be hierarchical (target->subtarget).
            // Update active subTarget (only, since the target is shared and non-active subTargets could override active settings)
            // after Target has been deserialized and target <-> subtarget references are intact.
            m_ActiveSubTarget.value.OnAfterParentTargetDeserialized();
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);
            // Core fields
            context.AddField(Fields.GraphVertex, descs.Contains(BlockFields.VertexDescription.Position) ||
                descs.Contains(BlockFields.VertexDescription.Normal) ||
                descs.Contains(BlockFields.VertexDescription.Tangent));
            context.AddField(Fields.GraphPixel);

            // SubTarget fields
            m_ActiveSubTarget.value.GetFields(ref context);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Core blocks
            if (!(m_ActiveSubTarget.value is UnityEditor.Rendering.Fullscreen.ShaderGraph.FullscreenSubTarget<UniversalTarget>))
            {
                context.AddBlock(BlockFields.VertexDescription.Position);
                context.AddBlock(BlockFields.VertexDescription.Normal);
                context.AddBlock(BlockFields.VertexDescription.Tangent);
                context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            }

            // SubTarget blocks
            m_ActiveSubTarget.value.GetActiveBlocks(ref context);
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            m_ActiveSubTarget.value.ProcessPreviewMaterial(material);
        }

        public override object saveContext => m_ActiveSubTarget.value?.saveContext;

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            activeSubTarget.CollectShaderProperties(collector, generationMode);

            collector.AddShaderProperty(LightmappingShaderProperties.kLightmapsArray);
            collector.AddShaderProperty(LightmappingShaderProperties.kLightmapsIndirectionArray);
            collector.AddShaderProperty(LightmappingShaderProperties.kShadowMasksArray);


            // SubTarget blocks
            m_ActiveSubTarget.value.CollectShaderProperties(collector, generationMode);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
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

#if HAS_VFX_GRAPH
            // VFX Support
            if (!(m_ActiveSubTarget.value is UniversalSubTarget))
                context.AddHelpBox(MessageType.Info, $"The {m_ActiveSubTarget.value.displayName} target does not support VFX Graph.");
            else
            {
                m_SupportVFXToggle = new Toggle("") { value = m_SupportVFX };
                context.AddProperty("Support VFX Graph", m_SupportVFXToggle, (evt) =>
                {
                    m_SupportVFX = m_SupportVFXToggle.value;
                });
            }
#endif
        }

        // this is a copy of ZTestMode, but hides the "Disabled" option, which is invalid
        enum ZTestModeForUI
        {
            Never = 1,
            Less = 2,
            Equal = 3,
            LEqual = 4,     // default for most rendering
            Greater = 5,
            NotEqual = 6,
            GEqual = 7,
            Always = 8,
        };

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

        public void AddDefaultSurfacePropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo, bool showReceiveShadows)
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

            context.AddProperty("Depth Test", new EnumField(ZTestModeForUI.LEqual) { value = (ZTestModeForUI)zTestMode }, (evt) =>
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

            context.AddProperty("Cast Shadows", new Toggle() { value = castShadows }, (evt) =>
            {
                if (Equals(castShadows, evt.newValue))
                    return;

                registerUndo("Change Cast Shadows");
                castShadows = evt.newValue;
                onChange();
            });

            if (showReceiveShadows)
                context.AddProperty("Receive Shadows", new Toggle() { value = receiveShadows }, (evt) =>
                {
                    if (Equals(receiveShadows, evt.newValue))
                        return;

                    registerUndo("Change Receive Shadows");
                    receiveShadows = evt.newValue;
                    onChange();
                });

            context.AddProperty("Supports LOD Cross Fade", new Toggle() { value = supportsLodCrossFade }, (evt) =>
            {
                if (Equals(supportsLodCrossFade, evt.newValue))
                    return;

                registerUndo("Change Supports LOD Cross Fade");
                supportsLodCrossFade = evt.newValue;
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

        void ProcessSubTargetDatas(SubTarget subTarget)
        {
            var typeCollection = TypeCache.GetTypesDerivedFrom<JsonObject>();
            foreach (var type in typeCollection)
            {
                if (type.IsGenericType)
                    continue;

                // Data requirement interfaces need generic type arguments
                // Therefore we need to use reflections to call the method
                var methodInfo = typeof(UniversalTarget).GetMethod(nameof(SetDataOnSubTarget));
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
                var methodInfo = typeof(UniversalTarget).GetMethod(nameof(ValidateDataForSubTarget));
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

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            void UpgradeAlphaClip()
            {
                var clipThresholdId = 8;
                var node = masterNode as AbstractMaterialNode;
                var clipThresholdSlot = node.FindSlot<Vector1MaterialSlot>(clipThresholdId);
                if (clipThresholdSlot == null)
                    return;

                clipThresholdSlot.owner = node;
                if (clipThresholdSlot.isConnected || clipThresholdSlot.value > 0.0f)
                {
                    m_AlphaClip = true;
                }
            }

            // Upgrade Target
            allowMaterialOverride = false;
            switch (masterNode)
            {
                case PBRMasterNode1 pbrMasterNode:
                    m_SurfaceType = (SurfaceType)pbrMasterNode.m_SurfaceType;
                    m_AlphaMode = (AlphaMode)pbrMasterNode.m_AlphaMode;
                    m_RenderFace = pbrMasterNode.m_TwoSided ? RenderFace.Both : RenderFace.Front;
                    UpgradeAlphaClip();
                    m_CustomEditorGUI = pbrMasterNode.m_OverrideEnabled ? pbrMasterNode.m_ShaderGUIOverride : "";
                    break;
                case UnlitMasterNode1 unlitMasterNode:
                    m_SurfaceType = (SurfaceType)unlitMasterNode.m_SurfaceType;
                    m_AlphaMode = (AlphaMode)unlitMasterNode.m_AlphaMode;
                    m_RenderFace = unlitMasterNode.m_TwoSided ? RenderFace.Both : RenderFace.Front;
                    UpgradeAlphaClip();
                    m_CustomEditorGUI = unlitMasterNode.m_OverrideEnabled ? unlitMasterNode.m_ShaderGUIOverride : "";
                    break;
                case SpriteLitMasterNode1 spriteLitMasterNode:
                    m_CustomEditorGUI = spriteLitMasterNode.m_OverrideEnabled ? spriteLitMasterNode.m_ShaderGUIOverride : "";
                    break;
                case SpriteUnlitMasterNode1 spriteUnlitMasterNode:
                    m_CustomEditorGUI = spriteUnlitMasterNode.m_OverrideEnabled ? spriteUnlitMasterNode.m_ShaderGUIOverride : "";
                    break;
            }

            // Upgrade SubTarget
            foreach (var subTarget in m_SubTargets)
            {
                if (!(subTarget is ILegacyTarget legacySubTarget))
                    continue;

                if (legacySubTarget.TryUpgradeFromMasterNode(masterNode, out blockMap))
                {
                    m_ActiveSubTarget = subTarget;
                    return true;
                }
            }

            blockMap = null;
            return false;
        }

        public override bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline)
        {
            return scriptableRenderPipeline?.GetType() == typeof(UniversalRenderPipelineAsset);
        }

#if HAS_VFX_GRAPH
        public void ConfigureContextData(VFXContext context, VFXTaskCompiledData data)
        {
            if (!(m_ActiveSubTarget.value is IRequireVFXContext vfxSubtarget))
                return;

            vfxSubtarget.ConfigureContextData(context, data);
        }

#endif

        public bool CanSupportVFX()
        {
            if (m_ActiveSubTarget.value == null)
                return false;

            if (m_ActiveSubTarget.value is UniversalUnlitSubTarget)
                return true;

            if (m_ActiveSubTarget.value is UniversalLitSubTarget)
                return true;

            if (m_ActiveSubTarget.value is UniversalSpriteLitSubTarget)
                return true;

            if (m_ActiveSubTarget.value is UniversalSpriteUnlitSubTarget)
                return true;

            if (m_ActiveSubTarget.value is UniversalSpriteCustomLitSubTarget)
                return true;

            //It excludes:
            // - UniversalDecalSubTarget
            return false;
        }

        public bool SupportsVFX()
        {
#if HAS_VFX_GRAPH
            if (!CanSupportVFX())
                return false;

            return m_SupportVFX;
#else
            return false;
#endif
        }

        [Serializable]
        class UniversalTargetLegacySerialization
        {
            [SerializeField]
            public bool m_TwoSided = false;
        }

        public override void OnAfterDeserialize(string json)
        {
            base.OnAfterDeserialize(json);

            if (this.sgVersion < latestVersion)
            {
                if (this.sgVersion == 0)
                {
                    // deserialize the old settings to upgrade
                    var oldSettings = JsonUtility.FromJson<UniversalTargetLegacySerialization>(json);
                    this.m_RenderFace = oldSettings.m_TwoSided ? RenderFace.Both : RenderFace.Front;
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
        /// <summary>
        ///  Automatically enables Alpha-To-Coverage in the provided opaque pass targets using alpha clipping
        /// </summary>
        /// <param name="pass">The pass to modify</param>
        /// <param name="target">The target to query</param>
        internal static void AddAlphaToMaskControlToPass(ref PassDescriptor pass, UniversalTarget target)
        {
            if (target.allowMaterialOverride)
            {
                // When material overrides are allowed, we have to rely on the _AlphaToMask material property since we can't be
                // sure of the surface type and alpha clip state based on the target alone.
                pass.renderStates.Add(RenderState.AlphaToMask("[_AlphaToMask]"));
            }
            else if (target.alphaClip && (target.surfaceType == SurfaceType.Opaque))
            {
                pass.renderStates.Add(RenderState.AlphaToMask("On"));
            }
        }

        internal static void AddAlphaClipControlToPass(ref PassDescriptor pass, UniversalTarget target)
        {
            if (target.allowMaterialOverride)
                pass.keywords.Add(CoreKeywordDescriptors.AlphaTestOn);
            else if (target.alphaClip)
                pass.defines.Add(CoreKeywordDescriptors.AlphaTestOn, 1);
        }

        internal static void AddLODCrossFadeControlToPass(ref PassDescriptor pass, UniversalTarget target)
        {
            if (target.supportsLodCrossFade)
            {
                pass.includes.Add(CoreIncludes.LODCrossFade);
                pass.keywords.Add(CoreKeywordDescriptors.LODFadeCrossFade);
                pass.defines.Add(CoreKeywordDescriptors.UseUnityCrossFade, 1);
            }
        }

        internal static void AddTargetSurfaceControlsToPass(ref PassDescriptor pass, UniversalTarget target, bool blendModePreserveSpecular = false)
        {
            // the surface settings can either be material controlled or target controlled
            if (target.allowMaterialOverride)
            {
                // setup material control of via keyword
                pass.keywords.Add(CoreKeywordDescriptors.SurfaceTypeTransparent);
                pass.keywords.Add(CoreKeywordDescriptors.AlphaPremultiplyOn);
                pass.keywords.Add(CoreKeywordDescriptors.AlphaModulateOn);
            }
            else
            {
                // setup target control via define
                if (target.surfaceType == SurfaceType.Transparent)
                {
                    pass.defines.Add(CoreKeywordDescriptors.SurfaceTypeTransparent, 1);

                    // alpha premultiply in shader only needed when alpha is different for diffuse & specular
                    if ((target.alphaMode == AlphaMode.Alpha || target.alphaMode == AlphaMode.Additive) && blendModePreserveSpecular)
                        pass.defines.Add(CoreKeywordDescriptors.AlphaPremultiplyOn, 1);
                    else if (target.alphaMode == AlphaMode.Multiply)
                        pass.defines.Add(CoreKeywordDescriptors.AlphaModulateOn, 1);
                }
            }

            AddAlphaClipControlToPass(ref pass, target);
        }

        // used by lit/unlit subtargets
        public static PassDescriptor DepthOnly(UniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "DepthOnly",
                useInPreview = true,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.DepthOnly(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection(),
                keywords = new KeywordCollection(),
                includes = new IncludeCollection { CoreIncludes.DepthOnly },

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);
            AddLODCrossFadeControlToPass(ref result, target);

            return result;
        }

        // used by lit/unlit subtargets
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

            AddAlphaClipControlToPass(ref result, target);
            AddLODCrossFadeControlToPass(ref result, target);

            return result;
        }

        // used by lit/unlit subtargets
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
                keywords = new KeywordCollection { CoreKeywordDescriptors.GBufferNormalsOct },
                includes = new IncludeCollection { CoreIncludes.DepthNormalsOnly },

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);
            AddLODCrossFadeControlToPass(ref result, target);

            return result;
        }

        // used by lit/unlit targets
        public static PassDescriptor ShadowCaster(UniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWCASTER",
                lightMode = "ShadowCaster",

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.ShadowCaster,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.ShadowCaster(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection(),
                keywords = new KeywordCollection { CoreKeywords.ShadowCaster },
                includes = new IncludeCollection { CoreIncludes.ShadowCaster },

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);
            AddLODCrossFadeControlToPass(ref result, target);

            return result;
        }

        public static PassDescriptor SceneSelection(UniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.SceneSelection(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection { CoreDefines.SceneSelection, { CoreKeywordDescriptors.AlphaClipThreshold, 1 } },
                keywords = new KeywordCollection(),
                includes = CoreIncludes.SceneSelection,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);

            return result;
        }

        public static PassDescriptor ScenePicking(UniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "ScenePickingPass",
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "Picking",
                useInPreview = false,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.ScenePicking(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection { CoreDefines.ScenePicking, { CoreKeywordDescriptors.AlphaClipThreshold, 1 } },
                keywords = new KeywordCollection(),
                includes = CoreIncludes.ScenePicking,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);

            return result;
        }

        public static PassDescriptor _2DSceneSelection(UniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.SceneSelection(target),
                pragmas = CorePragmas._2DDefault,
                defines = new DefineCollection { CoreDefines.SceneSelection, { CoreKeywordDescriptors.AlphaClipThreshold, 0 } },
                keywords = new KeywordCollection(),
                includes = CoreIncludes.ScenePicking,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);

            return result;
        }

        public static PassDescriptor _2DScenePicking(UniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "ScenePickingPass",
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "Picking",
                useInPreview = false,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.ScenePicking(target),
                pragmas = CorePragmas._2DDefault,
                defines = new DefineCollection { CoreDefines.ScenePicking, { CoreKeywordDescriptors.AlphaClipThreshold, 0 } },
                keywords = new KeywordCollection(),
                includes = CoreIncludes.SceneSelection,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);

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

    #region StructCollections
    static class CoreStructCollections
    {
        public static readonly StructCollection Default = new StructCollection
        {
            { Structs.Attributes },
            { UniversalStructs.Varyings },
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
            StructFields.Varyings.normalWS,
        };

        public static readonly FieldCollection DepthNormals = new FieldCollection()
        {
            StructFields.Attributes.uv1,                            // needed for meta vertex position
            StructFields.Varyings.normalWS,
            StructFields.Varyings.tangentWS,                        // needed for vertex lighting
        };
    }
    #endregion

    #region FieldDependencies
    static class CoreFieldDependencies
    {
        public static readonly DependencyCollection Default = new DependencyCollection()
        {
            { FieldDependencies.Default },
            new FieldDependency(UniversalStructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,    StructFields.Attributes.instanceID),
            new FieldDependency(UniversalStructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,     StructFields.Attributes.instanceID),
        };
    }
    #endregion

    #region RenderStates
    static class CoreRenderStates
    {
        public static class Uniforms
        {
            public static readonly string srcBlend = "[" + Property.SrcBlend + "]";
            public static readonly string dstBlend = "[" + Property.DstBlend + "]";
            public static readonly string cullMode = "[" + Property.CullMode + "]";
            public static readonly string zWrite = "[" + Property.ZWrite + "]";
            public static readonly string zTest = "[" + Property.ZTest + "]";
        }

        // used by sprite targets, NOT used by lit/unlit anymore
        public static readonly RenderStateCollection Default = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On), new FieldCondition(UniversalFields.SurfaceOpaque, true) },
            { RenderState.ZWrite(ZWrite.Off), new FieldCondition(UniversalFields.SurfaceTransparent, true) },
            { RenderState.Cull(Cull.Back), new FieldCondition(Fields.DoubleSided, false) },
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(UniversalFields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendAlpha, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(UniversalFields.BlendPremultiply, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.One, Blend.One, Blend.One), new FieldCondition(UniversalFields.BlendAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(UniversalFields.BlendMultiply, true) },
        };

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

        // used by lit/unlit subtargets
        public static RenderStateCollection UberSwitchedRenderState(UniversalTarget target, bool blendModePreserveSpecular = false)
        {
            if (target.allowMaterialOverride)
            {
                return new RenderStateCollection
                {
                    RenderState.ZTest(Uniforms.zTest),
                    RenderState.ZWrite(Uniforms.zWrite),
                    RenderState.Cull(Uniforms.cullMode),
                    RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend),
                };
            }
            else
            {
                var result = new RenderStateCollection();

                result.Add(RenderState.ZTest(target.zTestMode.ToString()));

                if (target.zWriteControl == ZWriteControl.Auto)
                {
                    if (target.surfaceType == SurfaceType.Opaque)
                        result.Add(RenderState.ZWrite(ZWrite.On));
                    else
                        result.Add(RenderState.ZWrite(ZWrite.Off));
                }
                else if (target.zWriteControl == ZWriteControl.ForceEnabled)
                    result.Add(RenderState.ZWrite(ZWrite.On));
                else
                    result.Add(RenderState.ZWrite(ZWrite.Off));

                result.Add(RenderState.Cull(RenderFaceToCull(target.renderFace)));

                if (target.surfaceType == SurfaceType.Opaque)
                {
                    result.Add(RenderState.Blend(Blend.One, Blend.Zero));
                }
                else
                {
                    // Lift alpha multiply from ROP to shader in preserve spec for different diffuse and specular blends.
                    Blend blendSrcRGB = blendModePreserveSpecular ? Blend.One : Blend.SrcAlpha;

                    switch (target.alphaMode)
                    {
                        case AlphaMode.Alpha:
                            result.Add(RenderState.Blend(blendSrcRGB, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                            break;
                        case AlphaMode.Premultiply:
                            result.Add(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                            break;
                        case AlphaMode.Additive:
                            result.Add(RenderState.Blend(blendSrcRGB, Blend.One, Blend.One, Blend.One));
                            break;
                        case AlphaMode.Multiply:
                            result.Add(RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.Zero, Blend.One)); // Multiply RGB only, keep A
                            break;
                    }
                }


                return result;
            }
        }

        // used by lit target ONLY
        public static readonly RenderStateCollection Meta = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off) },
        };

        public static RenderStateDescriptor UberSwitchedCullRenderState(UniversalTarget target)
        {
            if (target.allowMaterialOverride)
                return RenderState.Cull(Uniforms.cullMode);
            else
                return RenderState.Cull(RenderFaceToCull(target.renderFace));
        }

        // used by lit/unlit targets
        public static RenderStateCollection ShadowCaster(UniversalTarget target)
        {
            var result = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.On) },
                { UberSwitchedCullRenderState(target) },
                { RenderState.ColorMask("ColorMask 0") },
            };
            return result;
        }

        // used by lit/unlit targets
        public static RenderStateCollection DepthOnly(UniversalTarget target)
        {
            var result = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.On) },
                { UberSwitchedCullRenderState(target) },
                { RenderState.ColorMask("ColorMask R") },
            };

            return result;
        }

        // used by lit target ONLY
        public static RenderStateCollection DepthNormalsOnly(UniversalTarget target)
        {
            var result = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.On) },
                { UberSwitchedCullRenderState(target) }
            };

            return result;
        }

        // Used by all targets
        public static RenderStateCollection SceneSelection(UniversalTarget target)
        {
            var result = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off) },
            };

            return result;
        }

        public static RenderStateCollection ScenePicking(UniversalTarget target)
        {
            var result = new RenderStateCollection
            {
                { UberSwitchedCullRenderState(target) }
            };

            return result;
        }
    }
    #endregion

    #region Pragmas
    static class CorePragmas
    {
        public static readonly PragmaCollection Default = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Instanced = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.MultiCompileInstancing },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Forward = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection _2DDefault = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.ExcludeRenderers(new[] { Platform.D3D9 }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection GBuffer = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.ExcludeRenderers(new[] { Platform.GLES3, Platform.GLCore }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
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
        const string kCore = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl";
        const string kInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl";
        const string kLighting = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl";
        const string kGraphFunctions = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl";
        const string kVaryings = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl";
        const string kShaderPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl";
        const string kDepthOnlyPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl";
        const string kDepthNormalsOnlyPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl";
        const string kShadowCasterPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl";
        const string kTextureStack = "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl";
        const string kDBuffer = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl";
        const string kSelectionPickingPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl";
        const string kLODCrossFade = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl";

        // Files that are included with #include_with_pragmas
        const string kDOTS = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl";
        const string kRenderingLayers = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl";
        const string kProbeVolumes = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl";

        public static readonly IncludeCollection CorePregraph = new IncludeCollection
        {
            { kColor, IncludeLocation.Pregraph },
            { kTexture, IncludeLocation.Pregraph },
            { kCore, IncludeLocation.Pregraph },
            { kLighting, IncludeLocation.Pregraph },
            { kInput, IncludeLocation.Pregraph },
            { kTextureStack, IncludeLocation.Pregraph },        // TODO: put this on a conditional
        };

        public static readonly IncludeCollection DOTSPregraph = new IncludeCollection
        {
            { kDOTS, IncludeLocation.Pregraph, true },
        };

        public static readonly IncludeCollection WriteRenderLayersPregraph = new IncludeCollection
        {
            { kRenderingLayers, IncludeLocation.Pregraph, true },
        };

        public static readonly IncludeCollection ProbeVolumePregraph = new IncludeCollection
        {
            { kProbeVolumes, IncludeLocation.Pregraph, true },
        };

        public static readonly IncludeCollection ShaderGraphPregraph = new IncludeCollection
        {
            { kGraphFunctions, IncludeLocation.Pregraph },
        };

        public static readonly IncludeCollection CorePostgraph = new IncludeCollection
        {
            { kShaderPass, IncludeLocation.Pregraph },
            { kVaryings, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection DepthOnly = new IncludeCollection
        {
            // Pre-graph
            { DOTSPregraph },
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kDepthOnlyPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection DepthNormalsOnly = new IncludeCollection
        {
            // Pre-graph
            { DOTSPregraph },
            { WriteRenderLayersPregraph },
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kDepthNormalsOnlyPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection ShadowCaster = new IncludeCollection
        {
            // Pre-graph
            { DOTSPregraph },
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kShadowCasterPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection DBufferPregraph = new IncludeCollection
        {
            { kDBuffer, IncludeLocation.Pregraph },
        };

        public static readonly IncludeCollection SceneSelection = new IncludeCollection
        {
            // Pre-graph
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kSelectionPickingPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection ScenePicking = new IncludeCollection
        {
            // Pre-graph
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kSelectionPickingPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection LODCrossFade = new IncludeCollection
        {
            { kLODCrossFade, IncludeLocation.Pregraph }
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

        public static readonly DefineCollection UseFragmentFog = new DefineCollection()
        {
            {CoreKeywordDescriptors.UseFragmentFog, 1},
        };

        public static readonly DefineCollection SceneSelection = new DefineCollection
        {
            { CoreKeywordDescriptors.SceneSelectionPass, 1 },
        };

        public static readonly DefineCollection ScenePicking = new DefineCollection
        {
            { CoreKeywordDescriptors.ScenePickingPass, 1 },
        };
    }
    #endregion

    #region KeywordDescriptors
    static class CoreKeywordDescriptors
    {
        public static readonly KeywordDescriptor StaticLightmap = new KeywordDescriptor()
        {
            displayName = "Static Lightmap",
            referenceName = "LIGHTMAP_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor DynamicLightmap = new KeywordDescriptor()
        {
            displayName = "Dynamic Lightmap",
            referenceName = "DYNAMICLIGHTMAP_ON",
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
            displayName = ShaderKeywordStrings._ALPHATEST_ON,
            referenceName = ShaderKeywordStrings._ALPHATEST_ON,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor SurfaceTypeTransparent = new KeywordDescriptor()
        {
            displayName = ShaderKeywordStrings._SURFACE_TYPE_TRANSPARENT,
            referenceName = ShaderKeywordStrings._SURFACE_TYPE_TRANSPARENT,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global, // needs to match HDRP
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor AlphaPremultiplyOn = new KeywordDescriptor()
        {
            displayName = ShaderKeywordStrings._ALPHAPREMULTIPLY_ON,
            referenceName = ShaderKeywordStrings._ALPHAPREMULTIPLY_ON,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor AlphaModulateOn = new KeywordDescriptor()
        {
            displayName = ShaderKeywordStrings._ALPHAMODULATE_ON,
            referenceName = ShaderKeywordStrings._ALPHAMODULATE_ON,
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
            stages = KeywordShaderStage.Vertex,
        };

        public static readonly KeywordDescriptor AdditionalLights = new KeywordDescriptor()
        {
            displayName = "Additional Lights",
            referenceName = "",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "" },
                new KeywordEntry() { displayName = "Vertex", referenceName = "ADDITIONAL_LIGHTS_VERTEX" },
                new KeywordEntry() { displayName = "Fragment", referenceName = "ADDITIONAL_LIGHTS" },
            }
        };

        public static readonly KeywordDescriptor AdditionalLightShadows = new KeywordDescriptor()
        {
            displayName = "Additional Light Shadows",
            referenceName = "_ADDITIONAL_LIGHT_SHADOWS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor ReflectionProbeBlending = new KeywordDescriptor()
        {
            displayName = "Reflection Probe Blending",
            referenceName = "_REFLECTION_PROBE_BLENDING",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor ReflectionProbeBoxProjection = new KeywordDescriptor()
        {
            displayName = "Reflection Probe Box Projection",
            referenceName = "_REFLECTION_PROBE_BOX_PROJECTION",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor ShadowsSoft = new KeywordDescriptor()
        {
            displayName = "Shadows Soft",
            referenceName = "_SHADOWS_SOFT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
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

        public static readonly KeywordDescriptor LightLayers = new KeywordDescriptor()
        {
            displayName = "Light Layers",
            referenceName = "_LIGHT_LAYERS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor RenderPassEnabled = new KeywordDescriptor()
        {
            displayName = "Render Pass Enabled",
            referenceName = "_RENDER_PASS_ENABLED",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
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

        public static readonly KeywordDescriptor UseFragmentFog = new KeywordDescriptor()
        {
            displayName = "UseFragmentFog",
            referenceName = "_FOG_FRAGMENT",
            type = KeywordType.Boolean,
        };

        public static readonly KeywordDescriptor GBufferNormalsOct = new KeywordDescriptor()
        {
            displayName = "GBuffer normal octahedron encoding",
            referenceName = "_GBUFFER_NORMALS_OCT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor DBuffer = new KeywordDescriptor()
        {
            displayName = "Decals",
            referenceName = "",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "" },
                new KeywordEntry() { displayName = "DBuffer Mrt1", referenceName = "DBUFFER_MRT1" },
                new KeywordEntry() { displayName = "DBuffer Mrt2", referenceName = "DBUFFER_MRT2" },
                new KeywordEntry() { displayName = "DBuffer Mrt3", referenceName = "DBUFFER_MRT3" },
            },
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor DebugDisplay = new KeywordDescriptor()
        {
            displayName = "Debug Display",
            referenceName = "DEBUG_DISPLAY",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor FoveatedRendering = new KeywordDescriptor()
        {
            displayName = "Foveated Rendering Non Uniform Raster",
            referenceName = "_FOVEATED_RENDERING_NON_UNIFORM_RASTER",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
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

        public static readonly KeywordDescriptor AlphaClipThreshold = new KeywordDescriptor()
        {
            displayName = "AlphaClipThreshold",
            referenceName = "ALPHA_CLIP_THRESHOLD",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
        };

        public static readonly KeywordDescriptor LightCookies = new KeywordDescriptor()
        {
            displayName = "Light Cookies",
            referenceName = "_LIGHT_COOKIES",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor ForwardPlus = new KeywordDescriptor()
        {
            displayName = "Forward+",
            referenceName = "_FORWARD_PLUS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor EditorVisualization = new KeywordDescriptor()
        {
            displayName = "Editor Visualization",
            referenceName = "EDITOR_VISUALIZATION",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor LODFadeCrossFade = new KeywordDescriptor()
        {
            displayName = ShaderKeywordStrings.LOD_FADE_CROSSFADE,
            referenceName = ShaderKeywordStrings.LOD_FADE_CROSSFADE,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor UseUnityCrossFade = new KeywordDescriptor()
        {
            displayName = ShaderKeywordStrings.USE_UNITY_CROSSFADE,
            referenceName = ShaderKeywordStrings.USE_UNITY_CROSSFADE,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor ScreenSpaceAmbientOcclusion = new KeywordDescriptor()
        {
            displayName = "Screen Space Ambient Occlusion",
            referenceName = "_SCREEN_SPACE_OCCLUSION",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
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
        public static readonly FieldDescriptor UseLegacySpriteBlocks = new FieldDescriptor("Universal", "UseLegacySpriteBlocks", "UNIVERSAL_USELEGACYSPRITEBLOCKS");
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
