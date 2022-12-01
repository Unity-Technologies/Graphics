using UnityEditor.ShaderGraph;
using UnityEngine;
using System;
using UnityEditor.ShaderGraph.Internal;
using System.Linq;
using BlendMode = UnityEngine.Rendering.BlendMode;
using BlendOp = UnityEditor.ShaderGraph.BlendOp;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Fullscreen.ShaderGraph
{
    [GenerateBlocks("Fullscreen")]
    internal struct FullscreenBlocks
    {
        public static BlockFieldDescriptor color = new BlockFieldDescriptor(BlockFields.SurfaceDescription.name, "FullscreenColor", "Color",
            "SURFACEDESCRIPTION_COLOR", new ColorControl(UnityEngine.Color.grey, true), ShaderStage.Fragment);
        public static BlockFieldDescriptor eyeDepth = new BlockFieldDescriptor(BlockFields.SurfaceDescription.name, "FullscreenEyeDepth", "Eye Depth",
            "SURFACEDESCRIPTION_EYE_DEPTH", new FloatControl(0), ShaderStage.Fragment);
        public static BlockFieldDescriptor linear01Depth = new BlockFieldDescriptor(BlockFields.SurfaceDescription.name, "FullscreenLinear01Depth", "Linear01 Depth",
            "SURFACEDESCRIPTION_LINEAR01_DEPTH", new FloatControl(0), ShaderStage.Fragment);
        public static BlockFieldDescriptor rawDepth = new BlockFieldDescriptor(BlockFields.SurfaceDescription.name, "FullscreenRawDepth", "Raw Depth",
            "SURFACEDESCRIPTION_RAW_DEPTH", new FloatControl(0), ShaderStage.Fragment);
    }

    [GenerationAPI]
    internal struct FullscreenFields
    {
        public static FieldDescriptor depth = new FieldDescriptor("OUTPUT", "depth", "OUTPUT_DEPTH");
    }

    internal enum FullscreenMode
    {
        FullScreen,
        CustomRenderTexture,
    }

    internal enum FullscreenCompatibility
    {
        Blit,
        DrawProcedural,
    }

    internal enum FullscreenBlendMode
    {
        Disabled,
        Alpha,
        Premultiply,
        Additive,
        Multiply,
        Custom,
    }

    internal enum FullscreenDepthWriteMode
    {
        LinearEye,
        Linear01,
        Raw,
    }

    internal static class FullscreenUniforms
    {
        public static readonly string blendModeProperty = "_Fullscreen_BlendMode";
        public static readonly string srcColorBlendProperty = "_Fullscreen_SrcColorBlend";
        public static readonly string dstColorBlendProperty = "_Fullscreen_DstColorBlend";
        public static readonly string srcAlphaBlendProperty = "_Fullscreen_SrcAlphaBlend";
        public static readonly string dstAlphaBlendProperty = "_Fullscreen_DstAlphaBlend";
        public static readonly string colorBlendOperationProperty = "_Fullscreen_ColorBlendOperation";
        public static readonly string alphaBlendOperationProperty = "_Fullscreen_AlphaBlendOperation";
        public static readonly string depthWriteProperty = "_Fullscreen_DepthWrite";
        public static readonly string depthTestProperty = "_Fullscreen_DepthTest";
        public static readonly string stencilEnableProperty = "_Fullscreen_Stencil";
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

    internal abstract class FullscreenSubTarget<T> : SubTarget<T>, IRequiresData<FullscreenData>, IHasMetadata where T : Target
    {
        static readonly GUID kSourceCodeGuid = new GUID("1cfc804c75474e144be5d4158b9522ed");  // FullscreenSubTarget.cs // TODO
        static readonly string[] kSharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories().Union(new string[] { "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Templates" }).ToArray();

        // HLSL includes
        protected static readonly string kFullscreenCommon = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenCommon.hlsl";
        protected static readonly string kTemplatePath = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Templates/ShaderPass.template";
        protected static readonly string kCommon = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl";
        protected static readonly string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
        protected static readonly string kTexture = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl";
        protected static readonly string kInstancing = "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl";
        protected static readonly string kFullscreenShaderPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenShaderPass.cs.hlsl";
        protected static readonly string kSpaceTransforms = "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl";
        protected static readonly string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
        protected static readonly string kTextureStack = "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl";
        protected virtual string fullscreenDrawProceduralInclude => "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenDrawProcedural.hlsl";
        protected virtual string fullscreenBlitInclude => "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenBlit.hlsl";

        FullscreenData m_FullscreenData;

        FullscreenData IRequiresData<FullscreenData>.data
        {
            get => m_FullscreenData;
            set => m_FullscreenData = value;
        }

        public FullscreenData fullscreenData
        {
            get => m_FullscreenData;
            set => m_FullscreenData = value;
        }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.SetDefaultShaderGUI(GetDefaultShaderGUI().FullName);
            context.AddSubShader(GenerateSubShader());
        }

        protected virtual IncludeCollection pregraphIncludes => new IncludeCollection();
        protected abstract string pipelineTag { get; }

        protected virtual Type GetDefaultShaderGUI() => typeof(FullscreenShaderGUI);

        public virtual string identifier => GetType().Name;
        public virtual ScriptableObject GetMetadataObject(GraphDataReadOnly graph)
        {
            var bultInMetadata = ScriptableObject.CreateInstance<FullscreenMetaData>();
            bultInMetadata.fullscreenMode = fullscreenData.fullscreenMode;
            return bultInMetadata;
        }

        public RenderStateCollection GetRenderState()
        {
            var result = new RenderStateCollection();

            if (fullscreenData.allowMaterialOverride)
            {
                if (fullscreenData.depthTestMode != CompareFunction.Disabled)
                    result.Add(RenderState.ZTest(FullscreenUniforms.depthTest));
                else
                    result.Add(RenderState.ZTest("Off"));
                result.Add(RenderState.ZWrite(FullscreenUniforms.depthWrite));
                if (fullscreenData.blendMode != FullscreenBlendMode.Disabled)
                {
                    result.Add(RenderState.Blend(FullscreenUniforms.srcColorBlend, FullscreenUniforms.dstColorBlend, FullscreenUniforms.srcAlphaBlend, FullscreenUniforms.dstAlphaBlend));
                    result.Add(RenderState.BlendOp(FullscreenUniforms.colorBlendOperation, FullscreenUniforms.alphaBlendOperation));
                }
                else
                {
                    result.Add(RenderState.Blend("Blend Off"));
                }

                if (fullscreenData.enableStencil)
                {
                    result.Add(RenderState.Stencil(new StencilDescriptor { Ref = FullscreenUniforms.stencilReference, ReadMask = FullscreenUniforms.stencilReadMask, WriteMask = FullscreenUniforms.stencilWriteMask, Comp = FullscreenUniforms.stencilComparison, ZFail = FullscreenUniforms.stencilDepthFail, Fail = FullscreenUniforms.stencilFail, Pass = FullscreenUniforms.stencilPass }));
                }
            }
            else
            {
                if (fullscreenData.depthTestMode == CompareFunction.Disabled)
                    result.Add(RenderState.ZTest("Off"));
                else
                    result.Add(RenderState.ZTest(CompareFunctionToZTest(fullscreenData.depthTestMode).ToString()));
                result.Add(RenderState.ZWrite(fullscreenData.depthWrite ? ZWrite.On.ToString() : ZWrite.Off.ToString()));

                // Blend mode
                if (fullscreenData.blendMode == FullscreenBlendMode.Alpha)
                    result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                else if (fullscreenData.blendMode == FullscreenBlendMode.Premultiply)
                    result.Add(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                else if (fullscreenData.blendMode == FullscreenBlendMode.Additive)
                    result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One, Blend.One, Blend.One));
                else if (fullscreenData.blendMode == FullscreenBlendMode.Multiply)
                    result.Add(RenderState.Blend(Blend.DstColor, Blend.Zero));
                else if (fullscreenData.blendMode == FullscreenBlendMode.Disabled)
                    result.Add(RenderState.Blend("Blend Off"));
                else
                {
                    result.Add(RenderState.Blend(BlendModeToBlend(fullscreenData.srcColorBlendMode), BlendModeToBlend(fullscreenData.dstColorBlendMode), BlendModeToBlend(fullscreenData.srcAlphaBlendMode), BlendModeToBlend(fullscreenData.dstAlphaBlendMode)));
                    result.Add(RenderState.BlendOp(fullscreenData.colorBlendOperation, fullscreenData.alphaBlendOperation));
                }

                if (fullscreenData.enableStencil)
                {
                    result.Add(RenderState.Stencil(new StencilDescriptor
                    {
                        Ref = fullscreenData.stencilReference.ToString(),
                        ReadMask = fullscreenData.stencilReadMask.ToString(),
                        WriteMask = fullscreenData.stencilWriteMask.ToString(),
                        Comp = CompareFunctionToStencilString(fullscreenData.stencilCompareFunction),
                        ZFail = StencilOpToStencilString(fullscreenData.stencilDepthTestFailOperation),
                        Fail = StencilOpToStencilString(fullscreenData.stencilFailOperation),
                        Pass = StencilOpToStencilString(fullscreenData.stencilPassOperation),
                    }));
                }
            }

            result.Add(RenderState.Cull(UnityEditor.ShaderGraph.Cull.Off));

            return result;
        }

        public static Blend BlendModeToBlend(BlendMode mode)
        {
            switch (mode)
            {
                case BlendMode.Zero: return Blend.Zero;
                case BlendMode.One: return Blend.One;
                case BlendMode.DstColor: return Blend.DstColor;
                case BlendMode.SrcColor: return Blend.SrcColor;
                case BlendMode.OneMinusDstColor: return Blend.OneMinusDstColor;
                case BlendMode.SrcAlpha: return Blend.SrcAlpha;
                case BlendMode.OneMinusSrcColor: return Blend.OneMinusSrcColor;
                case BlendMode.DstAlpha: return Blend.DstAlpha;
                case BlendMode.OneMinusDstAlpha: return Blend.OneMinusDstAlpha;
                case BlendMode.SrcAlphaSaturate: return Blend.SrcAlpha;
                case BlendMode.OneMinusSrcAlpha: return Blend.OneMinusSrcAlpha;
                default: return Blend.Zero;
            }
            ;
        }

        public static ZTest CompareFunctionToZTest(CompareFunction mode)
        {
            switch (mode)
            {
                case CompareFunction.Equal: return ZTest.Equal;
                case CompareFunction.NotEqual: return ZTest.NotEqual;
                case CompareFunction.Greater: return ZTest.Greater;
                case CompareFunction.Less: return ZTest.Less;
                case CompareFunction.GreaterEqual: return ZTest.GEqual;
                case CompareFunction.LessEqual: return ZTest.LEqual;
                case CompareFunction.Always: return ZTest.Always;
                case CompareFunction.Disabled: return ZTest.Always;
                default: return ZTest.Always;
            }
            ;
        }

        public static string CompareFunctionToStencilString(CompareFunction compare)
        {
            switch (compare)
            {
                case CompareFunction.Never: return "Never";
                case CompareFunction.Equal: return "Equal";
                case CompareFunction.NotEqual: return "NotEqual";
                case CompareFunction.Greater: return "Greater";
                case CompareFunction.Less: return "Less";
                case CompareFunction.GreaterEqual: return "GEqual";
                case CompareFunction.LessEqual: return "LEqual";
                case CompareFunction.Always: return "Always";
                default: return "Always";
            }
            ;
        }

        public static string StencilOpToStencilString(StencilOp op)
        {
            switch (op)
            {
                case StencilOp.Keep: return "Keep";
                case StencilOp.Zero: return "Zero";
                case StencilOp.Replace: return "Replace";
                case StencilOp.IncrementSaturate: return "IncrSat";
                case StencilOp.DecrementSaturate: return "DecrSat";
                case StencilOp.Invert: return "Invert";
                case StencilOp.IncrementWrap: return "IncrWrap";
                case StencilOp.DecrementWrap: return "DecrWrap";
                default: return "Keep";
            }
            ;
        }

        public virtual SubShaderDescriptor GenerateSubShader()
        {
            var result = new SubShaderDescriptor()
            {
                generatesPreview = true,
                passes = new PassCollection(),
                pipelineTag = pipelineTag,
            };

            result.passes.Add(GenerateFullscreenPass(FullscreenCompatibility.DrawProcedural));
            result.passes.Add(GenerateFullscreenPass(FullscreenCompatibility.Blit));

            return result;
        }

        public virtual IncludeCollection GetPreGraphIncludes()
        {
            return new IncludeCollection
            {
                { kCommon, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kTexture, IncludeLocation.Pregraph },
                { kTextureStack, IncludeLocation.Pregraph },
                { kFullscreenShaderPass, IncludeLocation.Pregraph }, // For VR
                { pregraphIncludes },
                { kSpaceTransforms, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
            };
        }

        public virtual IncludeCollection GetPostGraphIncludes()
        {
            return new IncludeCollection { { kFullscreenCommon, IncludeLocation.Postgraph } };
        }

        static readonly KeywordDescriptor depthWriteKeyword = new KeywordDescriptor
        {
            displayName = "Depth Write",
            referenceName = "DEPTH_WRITE",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            stages = KeywordShaderStage.Fragment,
        };

        static readonly KeywordDescriptor depthWriteModeKeyword = new KeywordDescriptor
        {
            displayName = "Depth Write Mode",
            referenceName = "DEPTH_WRITE_MODE",
            type = KeywordType.Enum,
            definition = KeywordDefinition.Predefined,
            entries = new KeywordEntry[]
            {
                new KeywordEntry("Eye Depth", "EYE"),
                new KeywordEntry("Eye Linear 01", "LINEAR01"),
                new KeywordEntry("Eye Raw", "RAW"),
            },
            stages = KeywordShaderStage.Fragment,
        };

        public static StructDescriptor Varyings = new StructDescriptor()
        {
            name = "Varyings",
            packFields = true,
            populateWithCustomInterpolators = false,
            fields = new FieldDescriptor[]
            {
                StructFields.Varyings.positionCS,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.texCoord1,
                StructFields.Varyings.instanceID,
                StructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,
                StructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,
            }
        };

        protected virtual DefineCollection GetPassDefines(FullscreenCompatibility compatibility)
            => new DefineCollection();

        protected virtual KeywordCollection GetPassKeywords(FullscreenCompatibility compatibility)
            => new KeywordCollection();

        static StructDescriptor GetFullscreenAttributes(FullscreenCompatibility compatibility)
        {
            var desc = new StructDescriptor()
            {
                name = "Attributes",
                packFields = false,
            };

            if (compatibility == FullscreenCompatibility.Blit)
            {
                desc.fields = new FieldDescriptor[]
                {
                    StructFields.Attributes.instanceID,
                    StructFields.Attributes.vertexID,
                    StructFields.Attributes.positionOS,
                };
            }
            else
            {
                desc.fields = new FieldDescriptor[]
                {
                    StructFields.Attributes.instanceID,
                    StructFields.Attributes.vertexID,
                };
            }

            return desc;
        }

        public virtual PassDescriptor GenerateFullscreenPass(FullscreenCompatibility compatibility)
        {
            var fullscreenPass = new PassDescriptor
            {
                // Definition
                displayName = compatibility.ToString(),
                referenceName = "SHADERPASS_" + compatibility.ToString().ToUpper(),
                useInPreview = true,

                // Template
                passTemplatePath = kTemplatePath,
                sharedTemplateDirectories = kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = null,
                validPixelBlocks = new BlockFieldDescriptor[]
                {
                    BlockFields.SurfaceDescription.BaseColor,
                    BlockFields.SurfaceDescription.Alpha,
                    FullscreenBlocks.eyeDepth,
                    FullscreenBlocks.linear01Depth,
                    FullscreenBlocks.rawDepth,
                },

                // Fields
                structs = new StructCollection
                {
                    { GetFullscreenAttributes(compatibility) },
                    { Structs.SurfaceDescriptionInputs },
                    { Varyings },
                    { Structs.VertexDescriptionInputs },
                },
                fieldDependencies = FieldDependencies.Default,
                requiredFields = new FieldCollection
                {
                    StructFields.Varyings.texCoord0, // Always need texCoord0 to calculate the other properties in fullscreen node code
                    StructFields.Varyings.texCoord1, // We store the view direction computed in the vertex in the texCoord1
                    StructFields.Attributes.vertexID, // Need the vertex Id for the DrawProcedural case
                },

                // Conditional State
                renderStates = GetRenderState(),
                pragmas = new PragmaCollection
                {
                    { Pragma.Target(ShaderModel.Target30) },
                    { Pragma.Vertex("vert") },
                    { Pragma.Fragment("frag") },
                },
                defines = new DefineCollection
                {
                    {depthWriteKeyword, 1, new FieldCondition(FullscreenFields.depth, true)},
                    {depthWriteModeKeyword, (int)fullscreenData.depthWriteMode, new FieldCondition(FullscreenFields.depth, true)},
                    GetPassDefines(compatibility),
                },
                keywords = GetPassKeywords(compatibility),
                includes = new IncludeCollection
                {
                    // Pre-graph
                    GetPreGraphIncludes(),

                    // Post-graph
                    GetPostGraphIncludes(),
                },
            };

            switch (compatibility)
            {
                default:
                case FullscreenCompatibility.Blit:
                    fullscreenPass.includes.Add(fullscreenBlitInclude, IncludeLocation.Postgraph);
                    break;
                case FullscreenCompatibility.DrawProcedural:
                    fullscreenPass.includes.Add(fullscreenDrawProceduralInclude, IncludeLocation.Postgraph);
                    break;
            }

            return fullscreenPass;
        }

        // We don't need the save context / update materials for now
        public override object saveContext => null;

        public FullscreenSubTarget()
        {
            displayName = "Fullscreen";
        }

        public override bool IsNodeAllowedBySubTarget(Type nodeType)
        {
            var interfaces = nodeType.GetInterfaces();
            bool allowed = true;

            // Subgraph nodes inherits all the interfaces including vertex ones.
            if (nodeType == typeof(SubGraphNode))
                return true;

            // There is no input in the vertex block for now
            if (interfaces.Contains(typeof(IMayRequireVertexID)))
                allowed = false;
            if (interfaces.Contains(typeof(IMayRequireVertexSkinning)))
                allowed = false;

            return allowed;
        }

        public override bool IsActive() => true;

        public override void GetFields(ref TargetFieldContext context)
        {
            context.AddField(UnityEditor.ShaderGraph.Fields.GraphPixel);
            context.AddField(FullscreenFields.depth, fullscreenData.depthWrite || fullscreenData.depthTestMode != CompareFunction.Disabled);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);

            var depthBlock = FullscreenBlocks.eyeDepth;

            if (fullscreenData.depthWriteMode == FullscreenDepthWriteMode.Linear01)
                depthBlock = FullscreenBlocks.linear01Depth;
            if (fullscreenData.depthWriteMode == FullscreenDepthWriteMode.Raw)
                depthBlock = FullscreenBlocks.rawDepth;

            context.AddBlock(depthBlock, fullscreenData.depthWrite || fullscreenData.depthTestMode != CompareFunction.Disabled);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (fullscreenData.allowMaterialOverride)
            {
                base.CollectShaderProperties(collector, generationMode);

                CollectRenderStateShaderProperties(collector, generationMode);
            }

            collector.AddFloatProperty("_FlipY", 0, HLSLDeclaration.Global, false);
        }

        public void CollectRenderStateShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (generationMode != GenerationMode.Preview && fullscreenData.allowMaterialOverride)
            {
                // When blend mode is disabled, we can't override
                if (fullscreenData.blendMode != FullscreenBlendMode.Disabled)
                {
                    BlendMode srcColorBlend = fullscreenData.srcColorBlendMode;
                    BlendMode srcAlphaBlend = fullscreenData.srcAlphaBlendMode;
                    BlendMode dstColorBlend = fullscreenData.dstColorBlendMode;
                    BlendMode dstAlphaBlend = fullscreenData.dstAlphaBlendMode;
                    BlendOp colorBlendOp = fullscreenData.colorBlendOperation;
                    BlendOp alphaBlendOp = fullscreenData.alphaBlendOperation;

                    // Patch the default blend values depending on the Blend Mode:
                    if (fullscreenData.blendMode != FullscreenBlendMode.Custom)
                    {
                        colorBlendOp = BlendOp.Add;
                        alphaBlendOp = BlendOp.Add;
                    }

                    if (fullscreenData.blendMode == FullscreenBlendMode.Alpha)
                    {
                        srcColorBlend = BlendMode.SrcAlpha;
                        dstColorBlend = BlendMode.OneMinusSrcAlpha;
                        srcAlphaBlend = BlendMode.One;
                        dstAlphaBlend = BlendMode.OneMinusSrcAlpha;
                    }
                    else if (fullscreenData.blendMode == FullscreenBlendMode.Premultiply)
                    {
                        srcColorBlend = BlendMode.One;
                        dstColorBlend = BlendMode.OneMinusSrcAlpha;
                        srcAlphaBlend = BlendMode.One;
                        dstAlphaBlend = BlendMode.OneMinusSrcAlpha;
                    }
                    else if (fullscreenData.blendMode == FullscreenBlendMode.Additive)
                    {
                        srcColorBlend = BlendMode.SrcAlpha;
                        dstColorBlend = BlendMode.One;
                        srcAlphaBlend = BlendMode.One;
                        dstAlphaBlend = BlendMode.One;
                    }
                    else if (fullscreenData.blendMode == FullscreenBlendMode.Multiply)
                    {
                        srcColorBlend = BlendMode.DstColor;
                        dstColorBlend = BlendMode.Zero;
                        srcAlphaBlend = BlendMode.One;
                        dstAlphaBlend = BlendMode.OneMinusSrcAlpha;
                    }

                    collector.AddEnumProperty(FullscreenUniforms.blendModeProperty, fullscreenData.blendMode);
                    collector.AddEnumProperty(FullscreenUniforms.srcColorBlendProperty, srcColorBlend);
                    collector.AddEnumProperty(FullscreenUniforms.dstColorBlendProperty, dstColorBlend);
                    collector.AddEnumProperty(FullscreenUniforms.srcAlphaBlendProperty, srcAlphaBlend);
                    collector.AddEnumProperty(FullscreenUniforms.dstAlphaBlendProperty, dstAlphaBlend);
                    collector.AddEnumProperty(FullscreenUniforms.colorBlendOperationProperty, colorBlendOp);
                    collector.AddEnumProperty(FullscreenUniforms.alphaBlendOperationProperty, alphaBlendOp);
                }
                collector.AddBoolProperty(FullscreenUniforms.depthWriteProperty, fullscreenData.depthWrite);

                if (fullscreenData.depthTestMode != CompareFunction.Disabled)
                    collector.AddEnumProperty(FullscreenUniforms.depthTestProperty, fullscreenData.depthTestMode);

                // When stencil is disabled, we can't override
                if (fullscreenData.enableStencil)
                {
                    collector.AddBoolProperty(FullscreenUniforms.stencilEnableProperty, fullscreenData.enableStencil);
                    collector.AddIntProperty(FullscreenUniforms.stencilReferenceProperty, fullscreenData.stencilReference);
                    collector.AddIntProperty(FullscreenUniforms.stencilReadMaskProperty, fullscreenData.stencilReadMask);
                    collector.AddIntProperty(FullscreenUniforms.stencilWriteMaskProperty, fullscreenData.stencilWriteMask);
                    collector.AddEnumProperty(FullscreenUniforms.stencilComparisonProperty, fullscreenData.stencilCompareFunction);
                    collector.AddEnumProperty(FullscreenUniforms.stencilPassProperty, fullscreenData.stencilPassOperation);
                    collector.AddEnumProperty(FullscreenUniforms.stencilFailProperty, fullscreenData.stencilFailOperation);
                    collector.AddEnumProperty(FullscreenUniforms.stencilDepthFailProperty, fullscreenData.stencilDepthTestFailOperation);
                }
            }
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddProperty("Allow Material Override", new Toggle() { value = fullscreenData.allowMaterialOverride }, (evt) =>
            {
                if (Equals(fullscreenData.allowMaterialOverride, evt.newValue))
                    return;

                registerUndo("Change Allow Material Override");
                fullscreenData.allowMaterialOverride = evt.newValue;
                onChange();
            });

            GetRenderStatePropertiesGUI(ref context, onChange, registerUndo);
        }

        protected virtual void GetRenderStatePropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            GetBlendingPropertiesGUI(ref context, onChange, registerUndo);

            context.AddProperty("Depth Test", new EnumField(fullscreenData.depthTestMode) { value = fullscreenData.depthTestMode }, (evt) =>
            {
                if (Equals(fullscreenData.depthTestMode, evt.newValue))
                    return;

                registerUndo("Change Depth Test");
                fullscreenData.depthTestMode = (CompareFunction)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Write", new Toggle { value = fullscreenData.depthWrite }, (evt) =>
            {
                if (Equals(fullscreenData.depthWrite, evt.newValue))
                    return;

                registerUndo("Change Depth Write");
                fullscreenData.depthWrite = evt.newValue;
                onChange();
            });

            if (fullscreenData.depthWrite || fullscreenData.depthTestMode != CompareFunction.Disabled)
            {
                context.AddProperty("Depth Write Mode", new EnumField(fullscreenData.depthWriteMode) { value = fullscreenData.depthWriteMode }, (evt) =>
                {
                    if (Equals(fullscreenData.depthWriteMode, evt.newValue))
                        return;

                    registerUndo("Change Depth Write Mode");
                    fullscreenData.depthWriteMode = (FullscreenDepthWriteMode)evt.newValue;
                    onChange();
                });
            }

            GetStencilPropertiesGUI(ref context, onChange, registerUndo);
        }

        protected virtual void GetBlendingPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddProperty("Blend Mode", new EnumField(fullscreenData.blendMode) { value = fullscreenData.blendMode }, (evt) =>
            {
                if (Equals(fullscreenData.blendMode, evt.newValue))
                    return;

                registerUndo("Change Blend Mode");
                fullscreenData.blendMode = (FullscreenBlendMode)evt.newValue;
                onChange();
            });

            if (fullscreenData.blendMode == FullscreenBlendMode.Custom)
            {
                context.globalIndentLevel++;
                context.AddLabel("Color Blend Mode", 0);

                context.AddProperty("Src Color", new EnumField(fullscreenData.srcColorBlendMode) { value = fullscreenData.srcColorBlendMode }, (evt) =>
                {
                    if (Equals(fullscreenData.srcColorBlendMode, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    fullscreenData.srcColorBlendMode = (BlendMode)evt.newValue;
                    onChange();
                });
                context.AddProperty("Dst Color", new EnumField(fullscreenData.dstColorBlendMode) { value = fullscreenData.dstColorBlendMode }, (evt) =>
                {
                    if (Equals(fullscreenData.dstColorBlendMode, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    fullscreenData.dstColorBlendMode = (BlendMode)evt.newValue;
                    onChange();
                });
                context.AddProperty("Color Operation", new EnumField(fullscreenData.colorBlendOperation) { value = fullscreenData.colorBlendOperation }, (evt) =>
                {
                    if (Equals(fullscreenData.colorBlendOperation, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    fullscreenData.colorBlendOperation = (BlendOp)evt.newValue;
                    onChange();
                });

                context.AddLabel("Alpha Blend Mode", 0);


                context.AddProperty("Src", new EnumField(fullscreenData.srcAlphaBlendMode) { value = fullscreenData.srcAlphaBlendMode }, (evt) =>
                {
                    if (Equals(fullscreenData.srcAlphaBlendMode, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    fullscreenData.srcAlphaBlendMode = (BlendMode)evt.newValue;
                    onChange();
                });
                context.AddProperty("Dst", new EnumField(fullscreenData.dstAlphaBlendMode) { value = fullscreenData.dstAlphaBlendMode }, (evt) =>
                {
                    if (Equals(fullscreenData.dstAlphaBlendMode, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    fullscreenData.dstAlphaBlendMode = (BlendMode)evt.newValue;
                    onChange();
                });
                context.AddProperty("Blend Operation Alpha", new EnumField(fullscreenData.alphaBlendOperation) { value = fullscreenData.alphaBlendOperation }, (evt) =>
                {
                    if (Equals(fullscreenData.alphaBlendOperation, evt.newValue))
                        return;

                    registerUndo("Change Blend Mode");
                    fullscreenData.alphaBlendOperation = (BlendOp)evt.newValue;
                    onChange();
                });

                context.globalIndentLevel--;
            }
        }

        protected virtual void GetStencilPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddProperty("Enable Stencil", new Toggle { value = fullscreenData.enableStencil }, (evt) =>
            {
                if (Equals(fullscreenData.enableStencil, evt.newValue))
                    return;

                registerUndo("Change Enable Stencil");
                fullscreenData.enableStencil = evt.newValue;
                onChange();
            });

            if (fullscreenData.enableStencil)
            {
                context.globalIndentLevel++;

                context.AddProperty("Reference", new IntegerField { value = fullscreenData.stencilReference, isDelayed = true }, (evt) =>
                {
                    if (Equals(fullscreenData.stencilReference, evt.newValue))
                        return;

                    registerUndo("Change Stencil Reference");
                    fullscreenData.stencilReference = evt.newValue;
                    onChange();
                });

                context.AddProperty("Read Mask", new IntegerField { value = fullscreenData.stencilReadMask, isDelayed = true }, (evt) =>
                {
                    if (Equals(fullscreenData.stencilReadMask, evt.newValue))
                        return;

                    registerUndo("Change Stencil Read Mask");
                    fullscreenData.stencilReadMask = evt.newValue;
                    onChange();
                });

                context.AddProperty("Write Mask", new IntegerField { value = fullscreenData.stencilWriteMask, isDelayed = true }, (evt) =>
                {
                    if (Equals(fullscreenData.stencilWriteMask, evt.newValue))
                        return;

                    registerUndo("Change Stencil Write Mask");
                    fullscreenData.stencilWriteMask = evt.newValue;
                    onChange();
                });

                context.AddProperty("Comparison", new EnumField(fullscreenData.stencilCompareFunction) { value = fullscreenData.stencilCompareFunction }, (evt) =>
                {
                    if (Equals(fullscreenData.stencilCompareFunction, evt.newValue))
                        return;

                    registerUndo("Change Stencil Comparison");
                    fullscreenData.stencilCompareFunction = (CompareFunction)evt.newValue;
                    onChange();
                });

                context.AddProperty("Pass", new EnumField(fullscreenData.stencilPassOperation) { value = fullscreenData.stencilPassOperation }, (evt) =>
                {
                    if (Equals(fullscreenData.stencilPassOperation, evt.newValue))
                        return;

                    registerUndo("Change Stencil Pass Operation");
                    fullscreenData.stencilPassOperation = (StencilOp)evt.newValue;
                    onChange();
                });

                context.AddProperty("Fail", new EnumField(fullscreenData.stencilFailOperation) { value = fullscreenData.stencilFailOperation }, (evt) =>
                {
                    if (Equals(fullscreenData.stencilFailOperation, evt.newValue))
                        return;

                    registerUndo("Change Stencil Fail Operation");
                    fullscreenData.stencilFailOperation = (StencilOp)evt.newValue;
                    onChange();
                });

                context.AddProperty("Depth Fail", new EnumField(fullscreenData.stencilDepthTestFailOperation) { value = fullscreenData.stencilDepthTestFailOperation }, (evt) =>
                {
                    if (Equals(fullscreenData.stencilDepthTestFailOperation, evt.newValue))
                        return;

                    registerUndo("Change Stencil Depth Fail Operation");
                    fullscreenData.stencilDepthTestFailOperation = (StencilOp)evt.newValue;
                    onChange();
                });

                context.globalIndentLevel--;
            }
        }
    }

    internal static class FullscreenPropertyCollectorExtension
    {
        public static void AddEnumProperty<T>(this PropertyCollector collector, string prop, T value, HLSLDeclaration hlslDeclaration = HLSLDeclaration.DoNotDeclare) where T : Enum
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Enum,
                enumType = EnumType.CSharpEnum,
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

        public static void AddBoolProperty(this PropertyCollector collector, string prop, bool value, HLSLDeclaration hlslDeclaration = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new BooleanShaderProperty
            {
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = hlslDeclaration,
                value = value,
                overrideReferenceName = prop,
            });
        }

        public static void AddFloatProperty(this PropertyCollector collector, string referenceName, float defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare, bool generatePropertyBlock = true)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Default,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = declarationType,
                value = defaultValue,
                generatePropertyBlock = generatePropertyBlock,
                overrideReferenceName = referenceName,
            });
        }
    }
}
