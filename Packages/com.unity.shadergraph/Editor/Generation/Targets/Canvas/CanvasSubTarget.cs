using UnityEditor.ShaderGraph;
using UnityEngine;
using System;
using UnityEngine.UIElements;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;

namespace UnityEditor.Rendering.Canvas.ShaderGraph
{
    #region CanvasUniforms
    internal static class CanvasUniforms
    {
        // Uniforms
        public static readonly string Lighting = "Off";
        public static  string ColorMask = "ColorMask [_ColorMask]";
        public static  string ZTest = "[unity_GUIZTestMode]";

        //stencil Props
        public static  string Ref = "[_Stencil]";
        public static  string Comp = "[_StencilComp]";
        public static  string Pass = "[_StencilOp]";
        public static  string ReadMask = "[_StencilReadMask]";
        public static  string WriteMask = "[_StencilWriteMask]";
    }
    #endregion

    internal abstract class CanvasSubTarget<T> : SubTarget<T>, IRequiresData<CanvasData>, IHasMetadata where T : Target
    {
        const string kAssetGuid = "3ab5e3f315cd4041b2aa8600df09dd3e"; // CanvasSubTarget.cs

        #region Includes
        static readonly string[] kSharedTemplateDirectories = GetCanvasTemplateDirectories();

        private static string[] GetCanvasTemplateDirectories()
        {
            var shared = GenerationUtils.GetDefaultSharedTemplateDirectories();

            var canvasTemplateDirectories = new string[shared.Length + 1];
            canvasTemplateDirectories[shared.Length] = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Canvas/Templates";
            for(int i = 0; i < shared.Length; ++i)
            {
                canvasTemplateDirectories[i] = shared[i];
            }
            return canvasTemplateDirectories;
        }

        //HLSL Includes
        protected static readonly string kTemplatePath = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Canvas/Templates/PassUI.template";
        protected static readonly string kCommon  = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl";
        protected static readonly string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
        protected static readonly string kTexture = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl";
        protected static readonly string kInstancing = "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl";
        protected static readonly string kPacking = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl";
        protected static readonly string kSpaceTransforms = "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl";
        protected static readonly string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
        protected static readonly string kTextureStack = "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl";
#endregion

        CanvasData m_CanvasData;

        CanvasData IRequiresData<CanvasData>.data
        {
            get => m_CanvasData;
            set => m_CanvasData = value;
        }

        public CanvasData canvasData
        {
            get => m_CanvasData;
            set => m_CanvasData = value;
        }
        protected bool TargetsVFX() => false;
        protected virtual IncludeCollection pregraphIncludes => new IncludeCollection();
        protected virtual IncludeCollection postgraphIncludes => new IncludeCollection();
        protected virtual KeywordCollection GetAdditionalKeywords() => new KeywordCollection();
        protected abstract string pipelineTag { get; }

        protected virtual Type GetDefaultShaderGUI() => typeof(CanvasShaderGUI);

        public virtual string identifier => GetType().Name;
        public virtual ScriptableObject GetMetadataObject(GraphDataReadOnly graph)
        {
            var canvasMetadata = ScriptableObject.CreateInstance<CanvasMetaData>();
            return canvasMetadata;
        }

        public override bool IsActive() => true;
        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(new GUID(kAssetGuid), AssetCollection.Flags.SourceDependency);
            context.AddSubShader(GenerateDefaultSubshader());
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            //Core Fragment Blocks
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
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
            collector.AddShaderProperty(CanvasProperties.MainTex);
            collector.AddShaderProperty(CanvasProperties.StencilComp);
            collector.AddShaderProperty(CanvasProperties.Stencil);
            collector.AddShaderProperty(CanvasProperties.StencilOp);
            collector.AddShaderProperty(CanvasProperties.StencilWriteMask);
            collector.AddShaderProperty(CanvasProperties.StencilReadMask);
            collector.AddShaderProperty(CanvasProperties.ColorMask);
            collector.AddShaderProperty(CanvasProperties.ClipRect);
            collector.AddShaderProperty(CanvasProperties.UIMaskSoftnessX);
            collector.AddShaderProperty(CanvasProperties.UIMaskSoftnessY);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            context.AddField(UnityEditor.ShaderGraph.Fields.GraphPixel);
        }

        protected virtual DefineCollection GetAdditionalDefines()
        {
            var result = new DefineCollection();
            if (canvasData.disableTint)
                result.Add(CanvasKeywords.DisableTint, 1);
            return result;
        }



        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
        {
            context.AddProperty("Alpha Clipping", new Toggle() { value = canvasData.alphaClip }, (evt) =>
            {
                if (Equals(canvasData.alphaClip, evt.newValue))
                    return;

                registerUndo("Alpha Clip");
                canvasData.alphaClip = evt.newValue;
                onChange();
            });

            context.AddProperty("Disable Color Tint", new Toggle() { value = canvasData.disableTint }, (evt) =>
            {
                if (Equals(canvasData.disableTint, evt.newValue))
                    return;

                registerUndo("Disable Tint");
                canvasData.disableTint = evt.newValue;
                onChange();
            });
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
                    CanUseSpriteAtlas = "True",
                    generatesPreview = true,
                    passes =  new PassCollection(),

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

            var DefaultCanvasPass = new PassDescriptor()
            {
                // Definition
                displayName = "Default",
                referenceName = "SHADERPASS_CUSTOM_UI",

                useInPreview = true,

                // Templates
                passTemplatePath =  kTemplatePath,
                sharedTemplateDirectories = kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CanvasBlockMasks.Vertex,
                validPixelBlocks = CanvasBlockMasks.Fragment,

                // Collections

                // Fields
                structs = CanvasStructCollections.Default,
                requiredFields = CanvasRequiredFields.Default,
                fieldDependencies = FieldDependencies.Default,

                //Conditional State
                renderStates = CanvasRenderStates.GenerateRenderStateDeclaration(),
                pragmas  = CanvasPragmas.Default,
                includes = isSRP ? SRPCoreIncludes() : AdditionalIncludesOnly(),

                //definitions
                defines  = GetPassDefines(),
                keywords = CanvasKeywords.GenerateCoreKeywords(),

            };
            DefaultCanvasPass.defines.Add(GetAdditionalDefines());
            DefaultCanvasPass.keywords.Add(GetAdditionalKeywords());
            return DefaultCanvasPass;
        }

        // We don't need the save context / update materials for now
        public override object saveContext => null;

        public CanvasSubTarget()
        {
            displayName = "Canvas";
        }
        public override bool IsNodeAllowedBySubTarget(Type nodeType)
        {
            var interfaces = nodeType.GetInterfaces();
            int numInterfaces = interfaces.Length;

            // Subgraph nodes inherits all the interfaces including vertex ones.
            if (nodeType == typeof(BitangentVectorNode))
                return false;
            if (nodeType == typeof(TangentVectorNode))
                return false;
            if (nodeType == typeof(SubGraphNode))
                return true;
            if (nodeType == typeof(BakedGINode))
                return false;
            if (nodeType == typeof(ParallaxMappingNode))
                return false;
            if (nodeType == typeof(ParallaxOcclusionMappingNode))
                return false;
            if (nodeType == typeof(TriplanarNode))
                return false;
            if (nodeType == typeof(IsFrontFaceNode))
                return false;

            for (int i = 0; i < numInterfaces; i++)
            {
                if (interfaces[i] == typeof(IMayRequireVertexSkinning)) return false;
            }

            return true;
        }

        #region KeywordDescriptors
        internal static class CanvasKeywords
        {
            public static KeywordCollection GenerateCoreKeywords()
            {
                return new  KeywordCollection()
                {
                    UIAlphaClipKeyword,
                    UIClipRectKeyword,
                };
            }

            static  KeywordDescriptor UIClipRectKeyword = new KeywordDescriptor()
            {
                displayName = "UIClipRect",
                referenceName = "UNITY_UI_CLIP_RECT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
            };

            static KeywordDescriptor UIAlphaClipKeyword = new KeywordDescriptor()
            {
                displayName = "UIAlphaClip",
                referenceName = "UNITY_UI_ALPHACLIP",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordDescriptor DisableTint = new KeywordDescriptor()
            {
                displayName = "Disable Tint",
                referenceName = "_DISABLE_COLOR_TINT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.All,
            };
        }
        #endregion

    }

#region PortMasks
    class CanvasBlockMasks
    {
        // Port Mask
        public static BlockFieldDescriptor[] Vertex = null;

        public static  BlockFieldDescriptor[] Fragment = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.BaseColor,
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.Emission,
            BlockFields.SurfaceDescription.AlphaClipThreshold

        };
    }
#endregion

#region StructCollections
    static class CanvasStructCollections
    {
        public static StructCollection Default = new StructCollection()
        {
            CanvasStructs.Attributes,
            Structs.SurfaceDescriptionInputs,
            CanvasStructs.Varyings,
            Structs.VertexDescriptionInputs,
        };
    }

#endregion

#region RequiredFields
    static class CanvasRequiredFields
    {
        public static  FieldCollection Default = new FieldCollection()
        {
            StructFields.Varyings.positionCS,
            StructFields.Varyings.normalWS,
            StructFields.Varyings.positionWS,
            StructFields.Varyings.color,
            StructFields.Varyings.texCoord0,
            StructFields.Varyings.texCoord1,
            StructFields.Attributes.color,
            StructFields.Attributes.uv0, // Always need texCoord0, for UI image
            StructFields.Attributes.uv1, // Always need texCoord1 for UI Clip Mask
            StructFields.Attributes.positionOS,
            StructFields.Attributes.normalOS,
            StructFields.Attributes.instanceID,
            StructFields.Attributes.vertexID,
        };
    }
#endregion

#region RenderStates
    static class CanvasRenderStates
    {
        public static RenderStateCollection GenerateRenderStateDeclaration()
        {
            return  new RenderStateCollection
            {
                {RenderState.Cull(Cull.Off)},
                {RenderState.ZWrite(ZWrite.Off)},
                {RenderState.ZTest(CanvasUniforms.ZTest)},
                {RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha)},
                {RenderState.ColorMask(CanvasUniforms.ColorMask)},
                {RenderState.Stencil(new StencilDescriptor()
                    {
                        Ref = CanvasUniforms.Ref,
                        Comp = CanvasUniforms.Comp,
                        Pass = CanvasUniforms.Pass,
                        ReadMask = CanvasUniforms.ReadMask,
                        WriteMask = CanvasUniforms.WriteMask,
                    })
                }
            };
        }
    }
#endregion

#region Pragmas
    static class CanvasPragmas
    {
        public static  PragmaCollection Default = new PragmaCollection
        {
            {Pragma.Target(ShaderModel.Target20)},
            {Pragma.Vertex("vert")},
            {Pragma.Fragment("frag")},
        };
        public static  PragmaCollection DefaulDebugt = new PragmaCollection
        {
            {Pragma.Target(ShaderModel.Target20)},
            {Pragma.Vertex("vert")},
            {Pragma.Fragment("frag")},
            {Pragma.DebugSymbols},
        };
    }
#endregion
}
