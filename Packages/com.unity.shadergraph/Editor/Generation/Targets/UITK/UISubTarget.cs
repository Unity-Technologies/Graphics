using UnityEditor.ShaderGraph;
using UnityEngine;
using System;
using System.Collections.Generic;
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
                    passes = new PassCollection(),

                };
            result.passes.Add(GenerateUIPassDescriptor(isSRP));
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

        protected virtual DefineCollection GetPassDefines()
            => new DefineCollection();

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
            };
            return DefaultUITKPass;
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

        public INodeValidationExtension.Status GetValidationStatus(AbstractMaterialNode node, out string msg)
        {
            // Make sure node is in our graph first
            if (node.owner == null)
            {
                msg = null;
                return INodeValidationExtension.Status.None;
            }

            foreach (var item in node.owner.activeTargets)
            {
                if (item.prefersUITKPreview)
                {
                    if (ValidateUV(node, out msg))
                    {
                        return INodeValidationExtension.Status.Warning;
                    }

                    UVNode uvNode = node as UVNode;
                    if (uvNode != null)
                    {
                        if (uvNode.uvChannel != UnityEditor.ShaderGraph.Internal.UVChannel.UV0)
                        {
                            msg = "UI Material does not support UV1-7. Consider using 'UV0'.";
                            return INodeValidationExtension.Status.Warning;
                        }
                    }
                }
            }

            msg = null;
            return INodeValidationExtension.Status.None;
        }

        private bool ValidateUV(AbstractMaterialNode node, out string warningMessage)
        {
            List<UVMaterialSlot> uvSlots = new();
            node.GetInputSlots<UVMaterialSlot>(uvSlots);

            foreach (var uvSlot in uvSlots)
            {
                if (uvSlot.channel != UnityEditor.ShaderGraph.Internal.UVChannel.UV0)
                {
                    warningMessage = "UI Material does not support UV1-7. Consider using 'UV0'.";
                    return true;
                }
            }

            warningMessage = null;
            return false;
        }

        public override bool ValidateNodeCompatibility(AbstractMaterialNode node, out string warningMessage, out Rendering.ShaderCompilerMessageSeverity severity)
        {
            List<UVMaterialSlot> uvSlots = new();
            node.GetInputSlots<UVMaterialSlot>(uvSlots);
            severity = ShaderCompilerMessageSeverity.Warning;

            if (ValidateUV(node, out warningMessage))
                return true;

            foreach (var uvSlot in uvSlots)
            {
                if (uvSlot.channel != UnityEditor.ShaderGraph.Internal.UVChannel.UV0)
                {
                    warningMessage = "UI Material does not support UV1-7. Consider using 'UV0'.";
                    return true;
                }
            }

            SubGraphNode subGraphNode = node as SubGraphNode;
            if (subGraphNode != null)
            {
                SubGraphAsset subGraphAsset = subGraphNode.asset;
                if (subGraphAsset == null)
                {
                    warningMessage = null;
                    return false;
                }
                else
                {
                    foreach (var item in subGraphAsset.requirements.requiresMeshUVs)
                    {
                        if (item != UnityEditor.ShaderGraph.Internal.UVChannel.UV0)
                        {
                            warningMessage = "UI Material does not support UV1-7. Consider using 'UV0' in the subgraph.";
                            return true;
                        }
                    }
                }
            }

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

                // Node deviring from GeometryNode
                m_UnsupportedNodes.Add(typeof(BitangentVectorNode));
                m_UnsupportedNodes.Add(typeof(NormalVectorNode));
                m_UnsupportedNodes.Add(typeof(PositionNode));
                m_UnsupportedNodes.Add(typeof(TangentVectorNode));
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
        public static BlockFieldDescriptor[] Vertex = null;

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
            StructFields.Attributes.uv1,
            StructFields.Attributes.uv2,
            StructFields.Attributes.uv3,
            StructFields.Attributes.uv4,
            StructFields.Attributes.uv5,
            StructFields.Attributes.uv6,
            StructFields.Attributes.uv7,

            StructFields.Varyings.positionCS,
            StructFields.Varyings.color,
            StructFields.Varyings.texCoord0,
            StructFields.Varyings.texCoord1,
            StructFields.Varyings.texCoord3,
            StructFields.Varyings.texCoord4,
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
}
