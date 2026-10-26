using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine.UIElements;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Slots;

namespace UnityEditor.Rendering.UITK.ShaderGraph
{
    [GenerateBlocks("UI")]
    struct UITKBlocks
    {
        public static BlockFieldDescriptor coverage = new(BlockFields.SurfaceDescription.name, "Coverage", "Coverage",
            "SURFACEDESCRIPTION_COVERAGE", new FloatControl(1), ShaderStage.Fragment, true);
    }

    internal abstract class UISubTarget<T> : SubTarget<T>, IUISubTarget, INodeValidationExtension, IRequiresData<UIData> where T : Target
    {
        const string kAssetGuid = "a5150c3db0b6942f6a0b1f7a9ce97d5c"; // UISubTarget.cs

        #region Includes
        static readonly string[] kSharedTemplateDirectories = GetUITKTemplateDirectories();

        private static string[] GetUITKTemplateDirectories()
        {
            var shared = GenerationUtils.GetDefaultSharedTemplateDirectories();

            var uitkTemplateDirectories = new string[shared.Length + 1];
            uitkTemplateDirectories[shared.Length] = "Packages/com.unity.shadergraph/Editor/Generation/Targets/UITK/Templates";
            for(int i = 0; i < shared.Length; ++i)
            {
                uitkTemplateDirectories[i] = shared[i];
            }
            return uitkTemplateDirectories;
        }

        //HLSL Includes
        protected static readonly string kTemplatePath = "Packages/com.unity.shadergraph/Editor/Generation/Targets/UITK/Templates/PassUI.template";
        protected static readonly string kCommon  = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl";
        protected static readonly string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
        protected static readonly string kTexture = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl";
        protected static readonly string kInstancing = "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl";
        protected static readonly string kPacking = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl";
        protected static readonly string kSpaceTransforms = "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl";
        protected static readonly string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
        protected static readonly string kTextureStack = "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl";
        protected static readonly string kUIShim = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/UIShim.hlsl";
#endregion

        UIData m_UIData;

        UIData IRequiresData<UIData>.data
        {
            get => m_UIData;
            set => m_UIData = value;
        }

        public UIData uiData
        {
            get => m_UIData;
            set => m_UIData = value;
        }
        protected bool TargetsVFX() => false;
        protected virtual IncludeCollection pregraphIncludes => new IncludeCollection();
        protected virtual IncludeCollection postgraphIncludes => new IncludeCollection();
        protected abstract string pipelineTag { get; }

        public virtual string identifier => GetType().Name;

        public override bool IsActive() => true;
        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(new GUID(kAssetGuid), AssetCollection.Flags.SourceDependency);
            context.AddSubShader(GenerateDefaultSubshader());
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Position is always active so the graph vertex stage can read/write element-local
            // position. An untouched block defaults to the object-space position, so the pass output
            // is unchanged for graphs that don't drive Position.
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            CollectRenderStateShaderProperties(collector, generationMode);
        }

        public void CollectRenderStateShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            context.AddField(UnityEditor.ShaderGraph.Fields.GraphPixel);
        }

        public virtual SubShaderDescriptor GenerateDefaultSubshader(bool isSRP = true )
        {
            var result = new SubShaderDescriptor()
                {
                    pipelineTag =  pipelineTag,
                    renderQueue = "Transparent",
                    IgnoreProjector = "True",
                    renderType = "Transparent",
                    PreviewType = "Plane",
                    shaderFallback = "",
                    CanUseSpriteAtlas = "True",
                    generatesPreview = true,
                    customTags = "\"isCustomUITKShader\"=\"true\"",
                    passes = new PassCollection(),

                };
            result.passes.Add(GenerateUIPassDescriptor(isSRP));
            result.passes.Add(GenerateSceneSelectionPassDescriptor(isSRP));
            result.passes.Add(GenerateScenePickingPassDescriptor(isSRP));
            return result;
        }

        public IncludeCollection  AdditionalIncludesOnly()
        {
            return new IncludeCollection
            {
                { pregraphIncludes },
                { postgraphIncludes },
            };
        }

        public IncludeCollection SRPCoreIncludes()
        {
            return new IncludeCollection
            {
                // Pre-graph
                SRPPreGraphIncludes(),
                // Post-graph
                SRPPostGraphIncludes(),
            };
        }
        public virtual IncludeCollection SRPPreGraphIncludes()
        {
            return new IncludeCollection
            {
                {kCommon, IncludeLocation.Pregraph},
                {kColor, IncludeLocation.Pregraph},
                {kTexture, IncludeLocation.Pregraph},
                {kTextureStack, IncludeLocation.Pregraph},
                { pregraphIncludes },
                {kSpaceTransforms, IncludeLocation.Pregraph},
                {kFunctions, IncludeLocation.Pregraph},
            };
        }

        public virtual IncludeCollection SRPPostGraphIncludes()
        {
            return new IncludeCollection
            {
                { postgraphIncludes },
            };
        }

        static readonly KeywordDescriptor s_AutomaticOpacity = new KeywordDescriptor()
        {
            displayName = "Automatic Opacity",
            referenceName = "UITK_AUTOMATIC_OPACITY",
            type = KeywordType.Boolean,
        };

        protected virtual DefineCollection GetPassDefines()
        {
            var defines = new DefineCollection();
            if (uiData == null || uiData.automaticOpacity)
                defines.Add(s_AutomaticOpacity, 1);
            return defines;
        }

        protected virtual KeywordCollection GetPassKeywords()
            => new KeywordCollection();

        public virtual PassDescriptor GenerateUIPassDescriptor(bool isSRP)
        {

            var DefaultUITKPass = new PassDescriptor()
            {
                // Definition
                displayName = "Default",
                referenceName = "SHADERPASS_CUSTOM_UI",

                useInPreview = true,

                // Templates
                passTemplatePath =  kTemplatePath,
                sharedTemplateDirectories = kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = UITKBlockMasks.Vertex,
                validPixelBlocks = UITKBlockMasks.Fragment,

                // Collections

                // Fields
                structs = UITKStructCollections.Default,
                requiredFields = UITKRequiredFields.Default,
                fieldDependencies = FieldDependencies.Default,

                //Conditional State
                renderStates = UITKRenderStates.GenerateRenderStateDeclaration(),
                pragmas  = UITKPragmas.Default,
                keywords = UITKKeywords.Default,
                includes = AdditionalIncludesOnly(),

                //definitions
                defines  = GetPassDefines(),

                customInterpolators = UITKCustomInterpolators.Common,
            };
            return DefaultUITKPass;
        }

        // The Scene view selection outline and picking only position UITK's element-local vertices
        // correctly when the material provides its own SceneSelectionPass/Picking pass; otherwise the
        // editor falls back to a replacement material that skips the element transform and draws the
        // geometry mispositioned (UUM-146911). These passes reuse the Default pass vertex path
        // (uie_custom_vert, including the graph vertex stage). The selection pass mirrors
        // Internal-UIRDefault.shader's SceneSelectionPass; that shader has no picking pass, so the
        // picking pass follows URP's CorePasses.ScenePicking instead.
        protected virtual PassDescriptor GenerateSceneSelectionPassDescriptor(bool isSRP)
        {
            var pass = GenerateUIPassDescriptor(isSRP);
            pass.displayName = "SceneSelectionPass";
            pass.lightMode = "SceneSelectionPass";
            pass.useInPreview = false;
            // Fixed states: URP overrides the Default pass blend per alphaMode, but the selection
            // output (_ObjectId, _PassValue, 1, 1) must not depend on it. Alpha is 1, so the
            // standard SrcAlpha OneMinusSrcAlpha blend overwrites the destination.
            pass.renderStates = UITKRenderStates.GenerateRenderStateDeclaration();
            pass.keywords = UITKKeywords.SelectionAndPicking;
            pass.defines.Add(UITKKeywords.SceneSelectionPass, 1);
            return pass;
        }

        protected virtual PassDescriptor GenerateScenePickingPassDescriptor(bool isSRP)
        {
            var pass = GenerateUIPassDescriptor(isSRP);
            pass.displayName = "ScenePickingPass";
            pass.lightMode = "Picking";
            pass.useInPreview = false;
            pass.renderStates = UITKRenderStates.GenerateScenePickingRenderStateDeclaration();
            pass.keywords = UITKKeywords.SelectionAndPicking;
            pass.defines.Add(UITKKeywords.ScenePickingPass, 1);
            return pass;
        }

        // We don't need the save context / update materials for nows
        public override object saveContext => null;

        public UISubTarget()
        {
            displayName = "UI";
        }

        System.Collections.Generic.HashSet<Type> m_UnsupportedNodes;

        public string GetValidatorKey()
        {
            return "UISubTarget";
        }

        const string kUVErrorMessageNode = "UI Material only supports UV0-UV3. To use UV1-3, enable the matching channel under PanelSettings.extraVertexChannels.";
        const string kUVErrorMessageSubGraph = "UI Material only supports UV0-UV3 in the subgraph. To use UV1-3, enable the matching channel under PanelSettings.extraVertexChannels.";
        const string kGeometryFragmentStageError = "{0} cannot be used in the fragment stage on UI Toolkit shaders. UI Toolkit does not provide a per-element transform that would make the value meaningful in the fragment shader. Route the value through a Custom Interpolator (vertex stage to fragment stage) to read the raw vertex data in the fragment shader.";
        const string kGeometryNonObjectSpaceError = "{0}'s Space must be set to Object on UI Toolkit shaders. UI Toolkit does not provide a meaningful per-element world transform; other spaces would apply the batched-group transform and produce incorrect data. The Object space option returns the value as it was packed into the vertex stream.";
        const string kPositionSpaceError = "{0}'s Space must be set to Object or World on UI Toolkit shaders. Object returns the element-local position; World applies the element transform (translation, bone and group). View, Tangent and Absolute World are not supported.";
        const string kPositionObjectFragmentError = "{0} in Object space is the element-local position and is only available in the vertex stage on UI Toolkit shaders. Use World space to read the position in the fragment stage, or route the Object-space value through a Custom Interpolator (vertex stage to fragment stage).";
        const string kSampleElementTextureVertexError = "{0} in Standard mip sampling mode samples with implicit derivatives, which the vertex stage does not provide. Set its Mip Sampling Mode to LOD to sample the element texture in the vertex stage.";

        static bool IsAllowedUVChannel(UnityEditor.ShaderGraph.Internal.UVChannel channel)
        {
            return channel == UnityEditor.ShaderGraph.Internal.UVChannel.UV0
                || channel == UnityEditor.ShaderGraph.Internal.UVChannel.UV1
                || channel == UnityEditor.ShaderGraph.Internal.UVChannel.UV2
                || channel == UnityEditor.ShaderGraph.Internal.UVChannel.UV3;
        }

        public INodeValidationExtension.Status GetValidationStatus(AbstractMaterialNode node, out string msg)
        {
            if (node.owner == null)
            {
                msg = null;
                return INodeValidationExtension.Status.None;
            }

            if (!IsIUISubTarget(node))
            {
                msg = null;
                return INodeValidationExtension.Status.None;
            }

            if (!HasUVMaterialSlotOrIsUVNode(node))
            {
                msg = null;
                return INodeValidationExtension.Status.None;
            }

            node.owner.messageManager.ClearNodesFromProvider(node.owner, new[] { node });
            node.owner.messageManager.ClearNodesFromProvider(this, new[] { node });

            foreach (var item in node.owner.activeTargets)
            {
                if (item.prefersUITKPreview)
                {
                    if (ValidateUV(node, out msg))
                    {
                        return INodeValidationExtension.Status.Warning;
                    }
                }
            }

            msg = null;
            return INodeValidationExtension.Status.None;
        }

        static bool IsIUISubTarget(AbstractMaterialNode node)
        {
            bool isIUISubTarget = false;
            foreach (var target in node.owner.activeTargets)
            {
                var subTarget = target.activeSubTarget;
                if (subTarget is IUISubTarget)
                {
                    isIUISubTarget = true;
                    break;
                }
            }
            return isIUISubTarget;
        }

        static bool HasUVMaterialSlotOrIsUVNode(AbstractMaterialNode node)
        {
            List<UVMaterialSlot> uvSlots = new();
            node.GetInputSlots<UVMaterialSlot>(uvSlots);

            if (uvSlots.Count > 0)
            {
                return true;
            }

            UVNode uvNode = node as UVNode;
            if (uvNode != null)
            {
                return true;
            }
            return false;
        }

        static bool ValidateUV(AbstractMaterialNode node, out string warningMessage)
        {
            if (!IsIUISubTarget(node))
            {
                warningMessage = null;
                return false;
            }

            if (!HasUVMaterialSlotOrIsUVNode(node))
            {
                warningMessage = null;
                return false;
            }

            List<UVMaterialSlot> uvSlots = new();
            node.GetInputSlots<UVMaterialSlot>(uvSlots);

            foreach (var uvSlot in uvSlots)
            {
                if (!IsAllowedUVChannel(uvSlot.channel))
                {
                    warningMessage = kUVErrorMessageNode;
                    return true;
                }
            }

            UVNode uvNode = node as UVNode;
            if (uvNode != null)
            {
                if (!IsAllowedUVChannel(uvNode.uvChannel))
                {
                    warningMessage = kUVErrorMessageNode;
                    return true;
                }
            }

            warningMessage = null;
            return false;
        }

        static bool ValidateGeometryNode(AbstractMaterialNode node, out string errorMessage)
        {
            errorMessage = null;

            if (node is not GeometryNode geometryNode)
                return false;

            // Object (element-local) and World (group * bone * (local + translation)) are the only
            // meaningful UITK position spaces; View/Tangent/AbsoluteWorld would apply an unrelated
            // transform.
            if (geometryNode is PositionNode)
            {
                if (geometryNode.space != CoordinateSpace.Object && geometryNode.space != CoordinateSpace.World)
                {
                    errorMessage = string.Format(kPositionSpaceError, node.name);
                    return true;
                }

                // Object is element-local and only available in the vertex stage: the fragment can't
                // recover it from the panel-root world varying (inverting UNITY_MATRIX_M only reaches
                // group space). World rides the positionWS varying, so it works in both stages.
                if (geometryNode.space == CoordinateSpace.Object)
                {
                    var positionOutput = node.FindOutputSlot<MaterialSlot>(0);
                    if (positionOutput != null)
                    {
                        var positionCapability = NodeUtils.GetEffectiveShaderStageCapability(positionOutput, goingBackwards: false);
                        if (positionCapability != ShaderStageCapability.All && (positionCapability & ShaderStageCapability.Fragment) != 0)
                        {
                            errorMessage = string.Format(kPositionObjectFragmentError, node.name);
                            return true;
                        }
                    }
                }
                return false;
            }

            // ViewDirectionNode is blocked outright via m_UnsupportedNodes; only the three vector
            // nodes reach this point on UITK.
            if (geometryNode is not (NormalVectorNode or TangentVectorNode or BitangentVectorNode))
                return false;

            var outputSlot = node.FindOutputSlot<MaterialSlot>(0);
            if (outputSlot == null)
                return false;

            var capability = NodeUtils.GetEffectiveShaderStageCapability(outputSlot, goingBackwards: false);
            // capability == All means disconnected (no edges traversed); only flag actual fragment-stage paths.
            if (capability != ShaderStageCapability.All && (capability & ShaderStageCapability.Fragment) != 0)
            {
                errorMessage = string.Format(kGeometryFragmentStageError, node.name);
                return true;
            }

            if (geometryNode.space != CoordinateSpace.Object)
            {
                errorMessage = string.Format(kGeometryNonObjectSpaceError, node.name);
                return true;
            }

            return false;
        }

        static bool ValidateSampleElementTextureNode(AbstractMaterialNode node, out string errorMessage)
        {
            errorMessage = null;

            if (node is not SampleElementTextureNode sampleNode)
                return false;

            // LOD sampling takes an explicit mip and is valid in both stages; only Standard sampling
            // (implicit derivatives) is fragment-only, so it's the only mode that needs flagging.
            if (sampleNode.mipSamplingMode != ElementTextureMipSamplingMode.Standard)
                return false;

            var outputs = new List<MaterialSlot>();
            node.GetOutputSlots(outputs);
            foreach (var slot in outputs)
            {
                var capability = NodeUtils.GetEffectiveShaderStageCapability(slot, goingBackwards: false);
                // capability == All means disconnected (no edges traversed); only flag actual vertex paths.
                if (capability != ShaderStageCapability.All && (capability & ShaderStageCapability.Vertex) != 0)
                {
                    errorMessage = string.Format(kSampleElementTextureVertexError, node.name);
                    return true;
                }
            }

            return false;
        }

        static bool ValidateSubGraph(AbstractMaterialNode node, out string warningMessage)
        {
            warningMessage = null;

            if (node is not SubGraphNode subGraphNode)
                return false;

            SubGraphAsset subGraphAsset = subGraphNode.asset;
            if (subGraphAsset == null)
                return false;

            foreach (var item in subGraphAsset.requirements.requiresMeshUVs)
            {
                if (!IsAllowedUVChannel(item))
                {
                    warningMessage = kUVErrorMessageSubGraph;
                    return true;
                }
            }

            return false;
        }

        public override bool ValidateNodeCompatibility(AbstractMaterialNode node, out string warningMessage, out Rendering.ShaderCompilerMessageSeverity severity)
        {
            severity = ShaderCompilerMessageSeverity.Warning;

            if (ValidateGeometryNode(node, out warningMessage))
            {
                severity = ShaderCompilerMessageSeverity.Error;
                return true;
            }

            if (ValidateSampleElementTextureNode(node, out warningMessage))
            {
                severity = ShaderCompilerMessageSeverity.Error;
                return true;
            }

            if (ValidateUV(node, out warningMessage))
                return true;

            if (ValidateSubGraph(node, out warningMessage))
                return true;

            warningMessage = null;
            return false;
        }

        public override bool IsNodeAllowedBySubTarget(Type nodeType)
        {
            if (m_UnsupportedNodes == null)
            {
                m_UnsupportedNodes = new HashSet<Type>();
                m_UnsupportedNodes.Add(typeof(BakedGINode));
                m_UnsupportedNodes.Add(typeof(ParallaxMappingNode));
                m_UnsupportedNodes.Add(typeof(ParallaxOcclusionMappingNode));
                m_UnsupportedNodes.Add(typeof(TriplanarNode));
                m_UnsupportedNodes.Add(typeof(IsFrontFaceNode));

                // Nodes deriving from GeometryNode.
                // Normal / Tangent / Bitangent are now allowed: the corresponding streams are
                // opt-in via PanelSettings.extraVertexChannels (Normal, Tangent). When the
                // matching channel isn't enabled the stream is zero, so the user is responsible
                // for opting in to get meaningful values.
                // Position is allowed too (Object = element-local, World = element transform); its
                // spaces are validated in ValidateGeometryNode. ViewDirection stays blocked (no view).
                m_UnsupportedNodes.Add(typeof(ViewDirectionNode));

                // Vertex attribute related node which cannot be correctly handled in UITK.
                m_UnsupportedNodes.Add(typeof(VertexIDNode));
                m_UnsupportedNodes.Add(typeof(ComputeDeformNode));
                m_UnsupportedNodes.Add(typeof(LinearBlendSkinningNode));
            }

            var interfaces = nodeType.GetInterfaces();
            int numInterfaces = interfaces.Length;

            // Subgraph nodes inherits all the interfaces including vertex ones.
            if (m_UnsupportedNodes.Contains(nodeType))
                return false;

            if (nodeType == typeof(SubGraphNode))
                return true;

            for (int i = 0; i < numInterfaces; i++)
            {
                if (interfaces[i] == typeof(IMayRequireVertexSkinning)) return false;
            }

            return true;
        }
    }

#region PortMasks
    class UITKBlockMasks
    {
        // Port Mask
        public static BlockFieldDescriptor[] Vertex =
        {
            BlockFields.VertexDescription.Position,
        };

        public static  BlockFieldDescriptor[] Fragment =
        {
            BlockFields.SurfaceDescription.BaseColor,
            BlockFields.SurfaceDescription.Alpha,
        };
    }
#endregion

#region StructCollections
    static class UITKStructCollections
    {
        public static StructCollection Default = new StructCollection()
        {
            UIStructs.Attributes,
            UIStructs.UITKSurfaceDescriptionInputs,
            UIStructs.Varyings,
            UIStructs.UITKVertexDescriptionInputs,
        };
    }

#endregion

#region RequiredFields
    static class UITKRequiredFields
    {
        public static  FieldCollection Default = new FieldCollection()
        {
            StructFields.Attributes.positionOS,
            StructFields.Attributes.color,
            StructFields.Attributes.uv0,
            UIStructs.PackedIdsAttribute, // Uint4 instead of Float4
            StructFields.Attributes.uv5,

            StructFields.Varyings.positionCS,
            StructFields.Varyings.color,
            // UITK internals: texCoord4 = uvClip, texCoord5 = typeTexSettings, texCoord6 = textCoreLoc + layoutUV, texCoord7 = circle.
            // texCoord0-3 are reserved for user UV0-UV3 (opt-in via PanelSettings.extraVertexChannels) and only pulled in on demand.
            StructFields.Varyings.texCoord4,
            StructFields.Varyings.texCoord5,
            StructFields.Varyings.texCoord6,
            StructFields.Varyings.texCoord7,
            UIStructs.OpacityVarying, // per-element opacity, applied at the end of the fragment

            // uie_custom_frag always computes AA coverage and rect clipping from these, so they must be
            // copied into SurfaceDescriptionInputs even when no graph node reads them. Otherwise they stay
            // zero (a constant-color graph loses arc carving and edge AA) — the requiresUITK ConditionalFields
            // in GenerationUtils only populate them when an IMayRequireUITK node happens to be present.
            StructFields.SurfaceDescriptionInputs.typeTexSettings,
            StructFields.SurfaceDescriptionInputs.circle,
            StructFields.SurfaceDescriptionInputs.uvClip,
        };
    }
#endregion

#region Keywords

    static class UITKKeywords
    {
        public static KeywordDescriptor ForceGamma = new()
        {
            displayName = "Force Gamma",
            referenceName = "",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Local,
            entries = new KeywordEntry[]
            {
                new(){ displayName = "Disabled", referenceName = "" },
                new(){ displayName = "Enabled", referenceName = "UIE_FORCE_GAMMA" },
            }
        };

        public static KeywordDescriptor ForceTextureSlotCount = new()
        {
            displayName = "Force Texture Slot Count",
            referenceName = "",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Local,
            entries = new KeywordEntry[]
            {
                new(){ displayName = "8 Dynamic Texture Slots", referenceName = "" },
                new(){ displayName = "4 Dynamic Texture Slots", referenceName = "UIE_TEXTURE_SLOT_COUNT_4" },
                new(){ displayName = "2 Dynamic Texture Slots", referenceName = "UIE_TEXTURE_SLOT_COUNT_2" },
                new(){ displayName = "No Dynamic Texture Slot", referenceName = "UIE_TEXTURE_SLOT_COUNT_1" },
            }
        };

        public static KeywordDescriptor ForceRenderType = new()
        {
            displayName = "Force Render Type",
            referenceName = "",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Local,
            entries = new KeywordEntry[]
            {
                new(){ displayName = "Any Render Type", referenceName = "" },
                new(){ displayName = "Force Solid", referenceName = "UIE_RENDER_TYPE_SOLID" },
                new(){ displayName = "Force Texture", referenceName = "UIE_RENDER_TYPE_TEXTURE" },
                new(){ displayName = "Force Text", referenceName = "UIE_RENDER_TYPE_TEXT" },
                new(){ displayName = "Force Gradient", referenceName = "UIE_RENDER_TYPE_GRADIENT" },
            }
        };

        public static KeywordCollection Default = new()
        {
            ForceGamma,
            ForceTextureSlotCount,
            ForceRenderType,
        };

        public static KeywordDescriptor SceneSelectionPass = new()
        {
            displayName = "Scene Selection Pass",
            referenceName = "SCENESELECTIONPASS",
            type = KeywordType.Boolean,
        };

        public static KeywordDescriptor ScenePickingPass = new()
        {
            displayName = "Scene Picking Pass",
            referenceName = "SCENEPICKINGPASS",
            type = KeywordType.Boolean,
        };

        // ForceGamma is omitted: these passes only run for world-space panels, which never render
        // with forced gamma (UIRRenderTreeManager only enables it when !drawInCameras). The keyword
        // is therefore off in the Default pass too, so both passes evaluate the graph identically.
        public static KeywordCollection SelectionAndPicking = new()
        {
            ForceTextureSlotCount,
            ForceRenderType,
        };
    }

#endregion

#region RenderStates
    static class UITKRenderStates
    {
        public static RenderStateCollection GenerateRenderStateDeclaration()
        {
            return  new RenderStateCollection
            {
                {RenderState.Cull(Cull.Off)},
                {RenderState.ZWrite(ZWrite.Off)},
                {RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha)},
            };
        }

        public static RenderStateCollection GenerateScenePickingRenderStateDeclaration()
        {
            return new RenderStateCollection
            {
                {RenderState.Cull(Cull.Off)},
                // Picking reads back exact ids: ZWrite On resolves overlaps by depth, and
                // Blend One Zero prevents blending from corrupting the id (_SelectionID.a is
                // not 1). Mirrors URP's CorePasses.ScenePicking and the built-in particle
                // shaders' ScenePickingPass (Internal-UIRDefault.shader has no picking pass).
                {RenderState.ZWrite(ZWrite.On)},
                {RenderState.Blend(Blend.One, Blend.Zero)},
            };
        }
    }
#endregion

#region Pragmas
    static class UITKPragmas
    {
        public static  PragmaCollection Default = new PragmaCollection
        {
            {Pragma.Target(ShaderModel.Target35)},
            {Pragma.Vertex("uie_custom_vert")},
            {Pragma.Fragment("uie_custom_frag")},
        };
    }
#endregion

#region CustomInterpolators
    static class UITKCustomInterpolators
    {
        // CopyToSDI: pass-through assignments that copy each custom interpolator from Varyings
        // to SurfaceDescriptionInputs inside BuildSurfaceDescriptionInputs.
        // PreSurface: defines CustomInterpolatorPassThroughFunc(Varyings, VertexDescription),
        // which uie_custom_vert calls to copy custom interpolators from the vertex graph
        // output into the Varyings before packing.
        public static readonly CustomInterpSubGen.Collection Common = new CustomInterpSubGen.Collection
        {
            CustomInterpSubGen.Descriptor.MakeBlock(CustomInterpSubGen.Splice.k_spliceCopyToSDI, "output", "input"),
            CustomInterpSubGen.Descriptor.MakeFunc(CustomInterpSubGen.Splice.k_splicePreSurface, "CustomInterpolatorPassThroughFunc", "Varyings", "VertexDescription", "CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC", "FEATURES_GRAPH_VERTEX"),
        };
    }
#endregion
}
