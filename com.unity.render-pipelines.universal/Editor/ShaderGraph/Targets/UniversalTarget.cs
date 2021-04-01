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

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    public enum MaterialType
    {
        Lit,
        Unlit,
        SpriteLit,
        SpriteUnlit,
    }

    public enum WorkflowMode
    {
        Specular,
        Metallic,
    }

    enum SurfaceType
    {
        Opaque,
        Transparent,
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

    sealed class UniversalTarget : Target, ILegacyTarget
    {
        public override int latestVersion => 1;

        // Constants
        static readonly GUID kSourceCodeGuid = new GUID("8c72f47fdde33b14a9340e325ce56f4d"); // UniversalTarget.cs
        public const string kPipelineTag = "UniversalPipeline";
        public const string kLitMaterialTypeTag = "\"UniversalMaterialType\" = \"Lit\"";
        public const string kUnlitMaterialTypeTag = "\"UniversalMaterialType\" = \"Unlit\"";
        public static readonly string[] kSharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories().Union(new string[] {"Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Templates" }).ToArray();
        public const string kUberTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Templates/ShaderPass.template";

        // SubTarget
        List<SubTarget> m_SubTargets;
        List<string> m_SubTargetNames;
        int activeSubTargetIndex => m_SubTargets.IndexOf(m_ActiveSubTarget);

        // View
        PopupField<string> m_SubTargetField;
        TextField m_CustomGUIField;

        [SerializeField]
        JsonData<SubTarget> m_ActiveSubTarget;

        [SerializeField]
        SurfaceType m_SurfaceType = SurfaceType.Opaque;

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
        string m_CustomEditorGUI;

        internal override bool ignoreCustomInterpolators => false;
        internal override int padCustomInterpolatorLimit => 4;

        public UniversalTarget()
        {
            displayName = "Universal";
            m_SubTargets = TargetUtils.GetSubTargets(this);
            m_SubTargetNames = m_SubTargets.Select(x => x.displayName).ToList();
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            // ProcessSubTargetDatas(m_ActiveSubTarget.value);
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

        // TODO what is this used for?  It's using a default value (alphaClip) that may not always apply...
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

        public SurfaceType surfaceType
        {
            get => m_SurfaceType;
            set => m_SurfaceType = value;
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

        public string customEditorGUI
        {
            get => m_CustomEditorGUI;
            set => m_CustomEditorGUI = value;
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
            return worksWithThisSrp && base.IsNodeAllowedByTarget(nodeType);
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
            m_ActiveSubTarget.value.Setup(ref context);
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
            context.AddField(Fields.GraphVertex, descs.Contains(BlockFields.VertexDescription.Position) ||
                descs.Contains(BlockFields.VertexDescription.Normal) ||
                descs.Contains(BlockFields.VertexDescription.Tangent));
            context.AddField(Fields.GraphPixel);
            //context.AddField(Fields.AlphaClip, alphaClip);
            //context.AddField(Fields.DoubleSided, twoSided);

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

        public override void ProcessPreviewMaterial(Material material)
        {
            m_ActiveSubTarget.value.ProcessPreviewMaterial(material);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            activeSubTarget.CollectShaderProperties(collector, generationMode);

            collector.AddShaderProperty(LightmappingShaderProperties.kLightmapsArray);
            collector.AddShaderProperty(LightmappingShaderProperties.kLightmapsIndirectionArray);
            collector.AddShaderProperty(LightmappingShaderProperties.kShadowMasksArray);
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
            context.AddProperty("Custom Editor GUI", m_CustomGUIField, (evt) => {});
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
                    // deserialize old value:   TODO: use legacy and remove two sided
                    var oldSettings = JsonUtility.FromJson<UniversalTargetLegacySerialization>(json);

                    this.m_RenderFace = oldSettings.m_TwoSided ? RenderFace.Both : RenderFace.Front;
                }
                ChangeVersion(latestVersion);
            }
        }
    }

    #region Passes
    static class CorePasses
    {
        // used by lit/unlit targets
        public static readonly PassDescriptor DepthOnly = new PassDescriptor()
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
            renderStates = CoreRenderStates.DepthOnly,
            pragmas = CorePragmas.Instanced,
            keywords = CoreKeywords.DepthOnly,
            includes = CoreIncludes.DepthOnly,

            // Custom Interpolator Support
            customInterpolators = CoreCustomInterpDescriptors.Common
        };

        // used by lit/unlit targets
        public static readonly PassDescriptor ShadowCaster = new PassDescriptor()
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
            renderStates = CoreRenderStates.ShadowCaster,
            pragmas = CorePragmas.Instanced,
            keywords = CoreKeywords.ShadowCaster,
            includes = CoreIncludes.ShadowCaster,

            // Custom Interpolator Support
            customInterpolators = CoreCustomInterpDescriptors.Common
        };
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
            // TODO: this is a pure copy of HDRP uniforms, renamed -- some might not be needed in URP
            public static readonly string srcBlend = "[" + Property.SrcBlendSG + "]";
            public static readonly string dstBlend = "[" + Property.DstBlendSG + "]";
            public static readonly string cullMode = "[" + Property.Cull + "]";
            public static readonly string zWrite = "[" + Property.ZWriteSG + "]";
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

        // used by lit/unlit targets
        public static readonly RenderStateCollection UberDefault = new RenderStateCollection
        {
            // { RenderState.ZTest(Uniforms.zTest) },       // not used currently
            { RenderState.ZWrite(Uniforms.zWrite) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend) }, //, Uniforms.alphaSrcBlend, Uniforms.alphaDstBlend) },
        };

        // used by lit target ONLY
        public static readonly RenderStateCollection Meta = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off) },
        };

        // used by lit/unlit targets
        public static readonly RenderStateCollection ShadowCaster = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Cull(Uniforms.cullMode) },  // TODO: does URP want to support separate cull setting for shadow casting?
            { RenderState.ColorMask("ColorMask 0") },
        };

        // used by lit/unlit targets
        public static readonly RenderStateCollection DepthOnly = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.ColorMask("ColorMask 0") },
        };

        // used by lit target ONLY
        public static readonly RenderStateCollection DepthNormalsOnly = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Cull(Uniforms.cullMode) },
        };
    }
    #endregion

    #region Pragmas
    // TODO: should these be renamed and moved to UniversalPragmas/UniversalPragmas.cs ?
    // TODO: these aren't "core" as HDRP doesn't use them
    // TODO: and the same for the rest "Core" things
    static class CorePragmas
    {
        public static readonly PragmaCollection Default = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.OnlyRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore, Platform.D3D11 }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Instanced = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.OnlyRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore, Platform.D3D11 }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Forward = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.OnlyRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore, Platform.D3D11 }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
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

        public static readonly PragmaCollection DOTSDefault = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.ExcludeRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection DOTSInstanced = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.ExcludeRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.DOTSInstancing },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection DOTSForward = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.ExcludeRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.DOTSInstancing },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection DOTSGBuffer = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.ExcludeRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.DOTSInstancing },
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
        const string kLighting = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl";
        const string kGraphFunctions = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl";
        const string kVaryings = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl";
        const string kShaderPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl";
        const string kDepthOnlyPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl";
        const string kDepthNormalsOnlyPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl";
        const string kShadowCasterPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl";
        const string kTextureStack = "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl";

        public static readonly IncludeCollection CorePregraph = new IncludeCollection
        {
            { kColor, IncludeLocation.Pregraph },
            { kTexture, IncludeLocation.Pregraph },
            { kCore, IncludeLocation.Pregraph },
            { kLighting, IncludeLocation.Pregraph },
            { kTextureStack, IncludeLocation.Pregraph },        // TODO: put this on a conditional
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
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kDepthOnlyPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection DepthNormalsOnly = new IncludeCollection
        {
            // Pre-graph
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kDepthNormalsOnlyPass, IncludeLocation.Postgraph },
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
    }
    #endregion

    #region KeywordDescriptors
    // TODO: should these be renamed and moved to UniversalKeywordDescriptors/UniversalKeywords.cs ?
    // TODO: these aren't "core" as they aren't used by HDRP
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
            displayName = "_ALPHATEST_ON",
            referenceName = "_ALPHATEST_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };


        public static readonly KeywordDescriptor SurfaceTypeTransparent = new KeywordDescriptor()
        {
            displayName = "_SURFACE_TYPE_TRANSPARENT",
            referenceName = "_SURFACE_TYPE_TRANSPARENT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor AlphaPremultiplyOn = new KeywordDescriptor()
        {
            displayName = "_ALPHAPREMULTIPLY_ON",
            referenceName = "_ALPHAPREMULTIPLY_ON",
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

        /*      // this is a per-shader keyword, shouldn't be declared globally...
        public static readonly KeywordDescriptor SmoothnessChannel = new KeywordDescriptor()
        {
            displayName = "Smoothness Channel",
            referenceName = "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };
        */

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
            referenceName = "_FOG_FRAGMENT 1",
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
            CoreKeywordDescriptors.AlphaTestOn
        };

        public static readonly KeywordCollection DepthOnly = new KeywordCollection
        {
            CoreKeywordDescriptors.AlphaTestOn
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
