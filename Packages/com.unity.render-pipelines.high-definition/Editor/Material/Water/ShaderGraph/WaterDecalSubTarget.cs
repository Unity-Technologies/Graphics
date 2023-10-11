using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    internal class WaterDecalSubTarget : SubTarget<HDTarget>, IRequiresData<WaterDecalData>, IHasMetadata
    {
        static readonly GUID kSourceCodeGuid = new GUID("f6f7d8e2ebc19744cac1ede091471ffd");  // WaterDecalSubTarget.cs
        static readonly string[] kSharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories().Union(new string[] { "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Templates" }).ToArray();

        // HLSL includes
        protected static readonly string kFullscreenCommon = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenCommon.hlsl";
        protected static readonly string kTemplatePath = "Packages/com.unity.render-pipelines.high-definition/Editor/Material/Water/ShaderGraph/WaterDecalShaderPass.template";
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

        WaterDecalData m_Data;

        WaterDecalData IRequiresData<WaterDecalData>.data
        {
            get => m_Data;
            set => m_Data = value;
        }

        public virtual string identifier => GetType().Name;
        public WaterDecalSubTarget() => displayName = "Water Decal";

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddSubShader(GenerateSubShader());
        }

        public override bool IsActive() => true;
        public ScriptableObject GetMetadataObject(GraphDataReadOnly graph) => null;

        public override void GetFields(ref TargetFieldContext context)
        {
            context.AddField(Fields.GraphPixel);
        }



        protected IncludeCollection pregraphIncludes => new IncludeCollection
        {
            { CoreIncludes.MinimalCorePregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassSampling.hlsl", IncludeLocation.Pregraph},
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph}, // Need this to make the scene color/depth nodes work
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fullscreen/HDFullscreenFunctions.hlsl", IncludeLocation.Pregraph},
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph }
        };


        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(WaterDecalBlocks.Deformation);
            context.AddBlock(WaterDecalBlocks.SurfaceFoam);
            context.AddBlock(WaterDecalBlocks.DeepFoam);
        }

        public virtual SubShaderDescriptor GenerateSubShader()
        {
            var result = new SubShaderDescriptor()
            {
                generatesPreview = true,
                passes = new PassCollection(),
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
            };

            result.passes.Add(GeneratePass(WaterDeformer.PassType.Deformer));
            result.passes.Add(GeneratePass(WaterDeformer.PassType.FoamGenerator));

            return result;
        }

        public RenderStateCollection GetRenderState()
        {
            return new RenderStateCollection()
            {
                RenderState.ZWrite(ZWrite.Off),
                RenderState.ZTest(ZTest.Always),
                RenderState.Cull(Cull.Off),
            };
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


        [GenerateBlocks]
        public struct WaterDecalBlocks
        {
            // Water specific block descriptors
            public static BlockFieldDescriptor Deformation = new BlockFieldDescriptor(HDFields.kMaterial, "Deformation", "Deformation", "SURFACEDESCRIPTION_DEFORMATION", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SurfaceFoam = new BlockFieldDescriptor(HDFields.kMaterial, "SurfaceFoam", "SurfaceFoam", "SURFACEDESCRIPTION_SURFACE_FOAM", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor DeepFoam = new BlockFieldDescriptor(HDFields.kMaterial, "DeepFoam", "DeepFoam", "SURFACEDESCRIPTION_DEEP_FOAM", new FloatControl(0.0f), ShaderStage.Fragment);
        }

        public static BlockFieldDescriptor[] GetPixelBlocks()
        {
            return new BlockFieldDescriptor[]
            {
                WaterDecalBlocks.Deformation,
                WaterDecalBlocks.SurfaceFoam,
                WaterDecalBlocks.DeepFoam,
            };
        }

        PassDescriptor GeneratePass(WaterDeformer.PassType type)
        {
            var fullscreenPass = new PassDescriptor
            {
                // Definition
                displayName = type.ToString(),
                referenceName = "SHADERPASS_DRAWPROCEDURAL",
                useInPreview = true,

                // Template
                passTemplatePath = kTemplatePath,
                sharedTemplateDirectories = kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = null,
                validPixelBlocks = GetPixelBlocks(),

                // Fields
                structs = new StructCollection
                {
                    new StructDescriptor() {
                        name = "Attributes",
                        packFields = false,
                        fields = new FieldDescriptor[]
                        {
                            StructFields.Attributes.vertexID,
                            StructFields.Attributes.instanceID,
                        }
                    },

                    new StructDescriptor()
                    {
                        name = "SurfaceDescriptionInputs",
                        packFields = false,
                        populateWithCustomInterpolators = true,
                        fields = new FieldDescriptor[]
                        {
                            StructFields.SurfaceDescriptionInputs.ScreenPosition,
                            StructFields.SurfaceDescriptionInputs.NDCPosition,

                            StructFields.SurfaceDescriptionInputs.uv0,
                            StructFields.SurfaceDescriptionInputs.uv1,
                            StructFields.SurfaceDescriptionInputs.uv2,
                            StructFields.SurfaceDescriptionInputs.uv3,

                            StructFields.SurfaceDescriptionInputs.TimeParameters,
                        }
                    },

                    new StructDescriptor()
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
                        }
                    },

                    { Structs.VertexDescriptionInputs },
                },
                fieldDependencies = FieldDependencies.Default,
                requiredFields = new FieldCollection
                {
                    StructFields.Varyings.texCoord0, // Always need texCoord0 to calculate the other properties in fullscreen node code
                    StructFields.Varyings.texCoord1, // We store the view direction computed in the vertex in the texCoord1
                    StructFields.Attributes.vertexID, // Need the vertex Id for the DrawProcedural case
                },

                renderStates = GetRenderState(),
                pragmas = new PragmaCollection
                {
                    { Pragma.Target(ShaderModel.Target45) },
                    { Pragma.Vertex("Vert") },
                    { Pragma.Fragment("Frag") },
                    { Pragma.EditorSyncCompilation },
                    //{ Pragma.DebugSymbols },
                },
                defines = new DefineCollection
                {
                    { s_passKeyword[type], 1 },
                },
                includes = new IncludeCollection
                {
                    GetPreGraphIncludes(),
                },
            };

            return fullscreenPass;
        }

        #region keywords
        static readonly KeywordDescriptor deformationKeyword = new KeywordDescriptor
        {
            displayName = "Deformation Pass",
            referenceName = "PASS_DEFORMATION",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            stages = KeywordShaderStage.Fragment,
        };

        static readonly KeywordDescriptor foamKeyword = new KeywordDescriptor
        {
            displayName = "Foam Pass",
            referenceName = "PASS_FOAM",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
            stages = KeywordShaderStage.Fragment,
        };

        static Dictionary<WaterDeformer.PassType, KeywordDescriptor> s_passKeyword = new()
        {
            { WaterDeformer.PassType.Deformer, deformationKeyword },
            { WaterDeformer.PassType.FoamGenerator, foamKeyword },
        };
        #endregion


        public override bool IsNodeAllowedBySubTarget(Type nodeType)
        {
            if (nodeType == typeof(BakedGINode))
                return false;

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

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Default,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.Global,
                value = 0.0f,
                generatePropertyBlock = false,
                overrideReferenceName = "_FlipY",
            });
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
        {
        }


        [MenuItem("Assets/Create/Shader Graph/HDRP/Water Decal Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 7)]
        internal static void CreateWaterDecalGraph()
        {
            // Create an empty graph from scratch
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(WaterDecalSubTarget));

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, GetPixelBlocks());
        }

        internal static Shader CreateWaterDecalGraphAtPath(string path)
        {
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(WaterDecalSubTarget));

            var graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new[] { target }, GetPixelBlocks());

            graph.path = "Shader Graphs";
            FileUtilities.WriteShaderGraphToDisk(path, graph);
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<Shader>(path);
        }
    }
}
