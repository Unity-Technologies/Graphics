using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // Fullscreen-quad filter pass over a previous render; sibling of UISubTarget/CanvasSubTarget (per-element mesh), URP-only.
    internal class UniversalFilterSubTarget : SubTarget<UniversalTarget>, IHasImportArtifact, IRequiresData<FilterData>
    {
        const string kAssetGuid = "e1ff808ef8874836a011b0c079786f26"; // UniversalFilterSubTarget.cs

        #region Includes
        static readonly string[] kSharedTemplateDirectories = GetFilterTemplateDirectories();

        private static string[] GetFilterTemplateDirectories()
        {
            var shared = GenerationUtils.GetDefaultSharedTemplateDirectories();

            var filterTemplateDirectories = new string[shared.Length + 1];
            filterTemplateDirectories[shared.Length] = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Filter/Templates";
            for (int i = 0; i < shared.Length; ++i)
            {
                filterTemplateDirectories[i] = shared[i];
            }
            return filterTemplateDirectories;
        }

        // HLSL Includes
        protected static readonly string kTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Filter/Templates/PassFilter.template";
        protected static readonly string kCommon = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl";
        protected static readonly string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
        protected static readonly string kTexture = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl";
        protected static readonly string kInstancing = "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl";
        protected static readonly string kSpaceTransforms = "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl";
        protected static readonly string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
        protected static readonly string kTextureStack = "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl";
        #endregion

        FilterData m_FilterData;

        FilterData IRequiresData<FilterData>.data
        {
            get => m_FilterData;
            set => m_FilterData = value;
        }

        public FilterData filterData
        {
            get => m_FilterData;
            set => m_FilterData = value;
        }

        ScriptableObject IHasImportArtifact.GetImportArtifact(GraphData graph, Material material, string assetName)
            => FilterFunctionDefinitionGenerator.Generate(graph, material, assetName);

        static readonly string kFilterPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/FilterPass.hlsl";
        static readonly string kFilterUVRect = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/FilterUVRect.hlsl";

        IncludeCollection pregraphIncludes => new IncludeCollection
        {
            { CoreIncludes.CorePregraph },
            { kInstancing, IncludeLocation.Pregraph },
            { CoreIncludes.ShaderGraphPregraph },
            { kFilterUVRect, IncludeLocation.Pregraph },
        };

        IncludeCollection postgraphIncludes => new IncludeCollection
        {
            { kFilterPass, IncludeLocation.Postgraph },
        };

        string pipelineTag => UniversalTarget.kPipelineTag;

        public virtual string identifier => GetType().Name;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(new GUID(kAssetGuid), AssetCollection.Flags.SourceDependency);
            context.AddSubShader(GenerateDefaultSubshader());
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // No vertex block: a filter pass is a fixed fullscreen quad, there's no meaningful
            // per-vertex authoring surface the way there is for an element mesh.
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
            collector.AddShaderProperty(FilterProperties.MainTex);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            context.AddField(UnityEditor.ShaderGraph.Fields.GraphPixel);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
        {
            const string readMarginTooltip = "Extra pixels beyond the element's bounds this filter needs to read from the source texture. Must be non-negative.";
            var readMarginField = new FloatField() { value = filterData.readMargin };
            context.AddProperty("Read Margin", readMarginTooltip, 0, readMarginField, (evt) =>
            {
                var newValue = Mathf.Max(0f, evt.newValue);
                if (newValue != evt.newValue)
                    readMarginField.SetValueWithoutNotify(newValue);

                if (Equals(filterData.readMargin, newValue))
                    return;

                registerUndo("Read Margin");
                filterData.readMargin = newValue;
                onChange();
            });

            const string writeMarginTooltip = "Extra pixels beyond the element's bounds this filter needs to write to the destination texture. Must be non-negative.";
            var writeMarginField = new FloatField() { value = filterData.writeMargin };
            context.AddProperty("Write Margin", writeMarginTooltip, 0, writeMarginField, (evt) =>
            {
                var newValue = Mathf.Max(0f, evt.newValue);
                if (newValue != evt.newValue)
                    writeMarginField.SetValueWithoutNotify(newValue);

                if (Equals(filterData.writeMargin, newValue))
                    return;

                registerUndo("Write Margin");
                filterData.writeMargin = newValue;
                onChange();
            });
        }

        public virtual SubShaderDescriptor GenerateDefaultSubshader(bool isSRP = true)
        {
            var result = new SubShaderDescriptor()
            {
                pipelineTag = pipelineTag,
                renderQueue = "Transparent",
                renderType = "Transparent",
                // Without this, the master preview falls back to ShaderGraph's default mesh (a sphere)
                // instead of a fullscreen quad. CanvasSubTarget and UISubTarget set this for the same reason.
                PreviewType = "Plane",
                generatesPreview = true,
                passes = new PassCollection(),
            };
            result.passes.Add(GenerateFilterPassDescriptor(isSRP));
            return result;
        }

        public IncludeCollection AdditionalIncludesOnly()
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

        // Same gamma-correction multi_compile as the built-in filter shaders.
        static readonly KeywordDescriptor s_OutputLinear = new KeywordDescriptor()
        {
            displayName = "UIE Output Linear",
            referenceName = "_UIE_OUTPUT_LINEAR",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Local,
        };

        protected virtual KeywordCollection GetPassKeywords()
            => new KeywordCollection { s_OutputLinear };

        public virtual PassDescriptor GenerateFilterPassDescriptor(bool isSRP)
        {
            var defaultFilterPass = new PassDescriptor()
            {
                // Definition
                displayName = "Default",
                referenceName = "SHADERPASS_CUSTOM_UI",

                useInPreview = true,

                // Templates
                passTemplatePath = kTemplatePath,
                sharedTemplateDirectories = kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = null,
                validPixelBlocks = FilterBlockMasks.Fragment,

                // Fields
                structs = FilterStructCollections.Default,
                requiredFields = FilterRequiredFields.Default,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = FilterRenderStates.GenerateRenderStateDeclaration(),
                pragmas = FilterPragmas.Default,
                includes = isSRP ? SRPCoreIncludes() : AdditionalIncludesOnly(),

                // Definitions
                defines = GetPassDefines(),
                keywords = GetPassKeywords(),
            };
            return defaultFilterPass;
        }

        // We don't need the save context / update materials for now
        public override object saveContext => null;

        public UniversalFilterSubTarget()
        {
            displayName = "Filter";
            // Never hidden: isHidden also gates reimport matching; the feature flag only gates Assets/Create.
        }

        // A fixed fullscreen quad has no mesh or scene-buffer data sources for these.
        static readonly HashSet<Type> k_UnsupportedNodes = new HashSet<Type>
        {
            typeof(BakedGINode),
            typeof(ParallaxMappingNode),
            typeof(ParallaxOcclusionMappingNode),
            typeof(TriplanarNode),
            typeof(IsFrontFaceNode),
            typeof(ViewDirectionNode),
            typeof(SceneDepthNode),
            typeof(SceneColorNode),
            typeof(NormalVectorNode),
            typeof(TangentVectorNode),
            typeof(BitangentVectorNode),
            typeof(PositionNode),
            typeof(VertexIDNode),
            typeof(ComputeDeformNode),
            typeof(LinearBlendSkinningNode),
            // FilterStructs.Varyings declares no color/world-position varying -- no configuration of
            // these nodes could work, same as PositionNode etc. above.
            typeof(VertexColorNode),
            typeof(ScreenPositionNode),
        };

        public override bool IsNodeAllowedBySubTarget(Type nodeType)
        {
            // Subgraph nodes inherit all interfaces including vertex ones.
            if (nodeType == typeof(SubGraphNode))
                return true;

            if (k_UnsupportedNodes.Contains(nodeType))
                return false;

            var interfaces = nodeType.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i] == typeof(IMayRequireVertexSkinning))
                    return false;
            }

            return true;
        }

        // Instance-level check: only UV0 is declared, and codegen would not catch a UV1-3 request until shader compilation.
        static readonly List<UVMaterialSlot> s_UVSlotsScratch = new();

        public override bool ValidateNodeCompatibility(AbstractMaterialNode node, out string errorMessage, out ShaderCompilerMessageSeverity severity)
        {
            severity = ShaderCompilerMessageSeverity.Error;

            if (node is UVNode uvNode && uvNode.uvChannel != UVChannel.UV0)
            {
                errorMessage = $"{node.name} must use UV0 in Filter graphs -- only Attributes.uv0/Varyings.texCoord0 is declared.";
                return true;
            }

            s_UVSlotsScratch.Clear();
            node.GetInputSlots(s_UVSlotsScratch);
            foreach (var slot in s_UVSlotsScratch)
            {
                if (slot.channel != UVChannel.UV0)
                {
                    errorMessage = $"{node.name}'s UV input must use UV0 in Filter graphs -- only Attributes.uv0/Varyings.texCoord0 is declared.";
                    return true;
                }
            }

            errorMessage = null;
            return false;
        }
    }

    #region PortMasks
    class FilterBlockMasks
    {
        // Port Mask
        public static BlockFieldDescriptor[] Fragment = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.BaseColor,
            BlockFields.SurfaceDescription.Alpha,
        };
    }
    #endregion

    #region StructCollections
    static class FilterStructCollections
    {
        public static StructCollection Default = new StructCollection()
        {
            FilterStructs.Attributes,
            Structs.SurfaceDescriptionInputs,
            FilterStructs.Varyings,
            Structs.VertexDescriptionInputs,
        };
    }
    #endregion

    #region RequiredFields
    static class FilterRequiredFields
    {
        public static FieldCollection Default = new FieldCollection()
        {
            StructFields.Varyings.positionCS,
            StructFields.Varyings.texCoord0,
            StructFields.Attributes.positionOS,
            StructFields.Attributes.uv0, // the filter's own MainTex UV
            StructFields.Attributes.instanceID,
            StructFields.Attributes.vertexID,
        };
    }
    #endregion

    #region RenderStates
    static class FilterRenderStates
    {
        public static RenderStateCollection GenerateRenderStateDeclaration()
        {
            // Matches the render-state contract of the built-in filter shaders.
            return new RenderStateCollection
            {
                {RenderState.Cull(Cull.Off)},
                {RenderState.ZWrite(ZWrite.Off)},
                {RenderState.ZTest(ZTest.Always)},
                {RenderState.Blend(Blend.One, Blend.Zero)},
            };
        }
    }
    #endregion

    #region Pragmas
    static class FilterPragmas
    {
        public static PragmaCollection Default = new PragmaCollection
        {
            // FilterStructs' instanceID/vertexID/stereo fields need GPU instancing and single-pass
            // stereo XR, both of which require 3.5+ -- matches UISubTarget, which carries the same fields.
            {Pragma.Target(ShaderModel.Target35)},
            {Pragma.Vertex("vert")},
            {Pragma.Fragment("frag")},
        };
    }
    #endregion
}
