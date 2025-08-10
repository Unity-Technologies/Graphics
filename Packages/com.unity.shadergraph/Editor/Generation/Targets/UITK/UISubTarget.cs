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

    internal abstract class UISubTarget<T> : SubTarget<T>, IUISubTarget, IRequiresData<UIData> where T : Target
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
                includes = AdditionalIncludesOnly(),

                //definitions
                defines  = GetPassDefines(),
                keywords = new KeywordCollection(),

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
