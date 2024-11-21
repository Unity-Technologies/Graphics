using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

using static UnityEditor.Rendering.HighDefinition.WaterDecalSurfaceOptionsUIBlock;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    internal class WaterDecalSubTarget : SubTarget<HDTarget>, IRequiresData<WaterDecalData>, IHasMetadata
    {
        static readonly GUID kSourceCodeGuid = new GUID("f6f7d8e2ebc19744cac1ede091471ffd");  // WaterDecalSubTarget.cs
        static readonly string[] kSharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories().Union(new string[] { "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Templates" }).ToArray();
        static readonly string kCustomInspector = "Rendering.HighDefinition.WaterDecalShaderGraphGUI";

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

            if (!context.HasCustomEditorForRenderPipeline(typeof(HDRenderPipelineAsset)))
                context.AddCustomEditorForRenderPipeline(kCustomInspector, typeof(HDRenderPipelineAsset));
        }

        public override bool IsActive() => true;
        public ScriptableObject GetMetadataObject(GraphDataReadOnly graph) => null;

        struct WaterDecalFields
        {
            public static string name = "Fields";

            public static FieldDescriptor AffectsDeformation = new FieldDescriptor(name, "AffectsDeformation", "");
            public static FieldDescriptor AffectsFoam = new FieldDescriptor(name, "AffectsFoam", "");
            public static FieldDescriptor AffectsSimulationMask = new FieldDescriptor(name, "AffectsSimulationMask", "");
            public static FieldDescriptor AffectsLargeCurrent = new FieldDescriptor(name, "AffectsLargeCurrent", "");
            public static FieldDescriptor AffectsRipplesCurrent = new FieldDescriptor(name, "AffectsRipplesCurrent", "");
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            context.AddField(WaterDecalFields.AffectsDeformation, m_Data.affectsDeformation);
            context.AddField(WaterDecalFields.AffectsFoam, m_Data.affectsFoam);
            context.AddField(WaterDecalFields.AffectsSimulationMask, m_Data.affectsSimulationMask);
            context.AddField(WaterDecalFields.AffectsLargeCurrent, m_Data.affectsLargeCurrent);
            context.AddField(WaterDecalFields.AffectsRipplesCurrent, m_Data.affectsLargeCurrent);

            context.AddField(Fields.GraphPixel);
        }


        public virtual SubShaderDescriptor GenerateSubShader()
        {
            var result = new SubShaderDescriptor()
            {
                generatesPreview = true,
                passes = new PassCollection(),
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
            };

            if (m_Data.affectsDeformation || m_Data.affectsFoam)
                result.passes.Add(GeneratePass(WaterDecal.PassType.DeformationAndFoam));
            if (m_Data.affectsSimulationMask)
                result.passes.Add(GeneratePass(WaterDecal.PassType.SimulationMask));
            if (m_Data.affectsLargeCurrent)
                result.passes.Add(GeneratePass(WaterDecal.PassType.LargeCurrent));
            if (m_Data.affectsRipplesCurrent)
                result.passes.Add(GeneratePass(WaterDecal.PassType.RipplesCurrent));

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

        protected IncludeCollection pregraphIncludes => new IncludeCollection
        {
            { CoreIncludes.MinimalCorePregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassSampling.hlsl", IncludeLocation.Pregraph},
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph}, // Need this to make the scene color/depth nodes work
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fullscreen/HDFullscreenFunctions.hlsl", IncludeLocation.Pregraph},
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph }
        };

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
            public static string name = "Material";

            // Water specific block descriptors
            public static BlockFieldDescriptor Deformation = new BlockFieldDescriptor(name, "Deformation", "Deformation", "SURFACEDESCRIPTION_DEFORMATION", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SurfaceFoam = new BlockFieldDescriptor(name, "SurfaceFoam", "SurfaceFoam", "SURFACEDESCRIPTION_SURFACE_FOAM", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor DeepFoam = new BlockFieldDescriptor(name, "DeepFoam", "DeepFoam", "SURFACEDESCRIPTION_DEEP_FOAM", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SimulationMask = new BlockFieldDescriptor(name, "SimulationMask", "SimulationMask", "SURFACEDESCRIPTION_SIMULATION_MASK", new Vector3Control(Vector3.one), ShaderStage.Fragment);
            public static BlockFieldDescriptor SimulationFoamMask = new BlockFieldDescriptor(name, "SimulationFoamMask", "SimulationFoamMask", "SURFACEDESCRIPTION_FOAM_MASK", new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor LargeCurrent = new BlockFieldDescriptor(name, "LargeCurrent", "LargeCurrent", "SURFACEDESCRIPTION_LARGE_CURRENT", new Vector2Control(new Vector2(0, 0)), ShaderStage.Fragment);
            public static BlockFieldDescriptor LargeCurrentInfluence = new BlockFieldDescriptor(name, "LargeCurrentInfluence", "LargeCurrentInfluence", "SURFACEDESCRIPTION_LARGE_CURRENT_INFLUENCE", new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor RipplesCurrent = new BlockFieldDescriptor(name, "RipplesCurrent", "RipplesCurrent", "SURFACEDESCRIPTION_RIPPLES_CURRENT", new Vector2Control(new Vector2(0, 0)), ShaderStage.Fragment);
            public static BlockFieldDescriptor RipplesCurrentInfluence = new BlockFieldDescriptor(name, "RipplesCurrentInfluence", "RipplesCurrentInfluence", "SURFACEDESCRIPTION_RIPPLES_CURRENT_INFLUENCE", new FloatControl(1.0f), ShaderStage.Fragment);

            public static BlockFieldDescriptor[] GetPixelBlocks()
            {
                return new BlockFieldDescriptor[]
                {
                    Deformation,
                    SurfaceFoam,
                    DeepFoam,
                    SimulationMask,
                    SimulationFoamMask,
                    LargeCurrent,
                    LargeCurrentInfluence,
                    RipplesCurrent,
                    RipplesCurrentInfluence,
                };
            }
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            if (m_Data.affectsDeformation)
                context.AddBlock(WaterDecalBlocks.Deformation);
            if (m_Data.affectsFoam)
            {
                context.AddBlock(WaterDecalBlocks.SurfaceFoam);
                context.AddBlock(WaterDecalBlocks.DeepFoam);
            }
            if (m_Data.affectsSimulationMask)
            {
                context.AddBlock(WaterDecalBlocks.SimulationMask);
                context.AddBlock(WaterDecalBlocks.SimulationFoamMask);
            }
            if (m_Data.affectsLargeCurrent)
            {
                context.AddBlock(WaterDecalBlocks.LargeCurrent);
                context.AddBlock(WaterDecalBlocks.LargeCurrentInfluence);
            }
            if (m_Data.affectsRipplesCurrent)
            {
                context.AddBlock(WaterDecalBlocks.RipplesCurrent);
                context.AddBlock(WaterDecalBlocks.RipplesCurrentInfluence);
            }
        }

        PassDescriptor GeneratePass(WaterDecal.PassType type)
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
                validPixelBlocks = WaterDecalBlocks.GetPixelBlocks(),

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
                            StructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,
                            StructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,
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
                    { Pragma.DOTSInstancing },
                    Pragma.MultiCompileInstancing,
                    //{ Pragma.DebugSymbols },
                },
                defines = WaterDecalDefines.GetPassDefines(type),
                keywords = WaterDecalDefines.GetPassKeywords(type, m_Data),
                includes = new IncludeCollection
                {
                    GetPreGraphIncludes(),
                },
            };

            return fullscreenPass;
        }

        #region keywords

        #endregion

        #region Defines
        struct WaterDecalDefines
        {
            static readonly KeywordDescriptor deformFoamKeyword = new KeywordDescriptor
            {
                displayName = "Deformation And Foam Pass",
                referenceName = "PASS_DEFORMATION_AND_FOAM",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                stages = KeywordShaderStage.Fragment,
            };

            static readonly KeywordDescriptor maskKeyword = new KeywordDescriptor
            {
                displayName = "Mask Pass",
                referenceName = "PASS_MASK",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                stages = KeywordShaderStage.Fragment,
            };

            static readonly KeywordDescriptor largeKeyword = new KeywordDescriptor
            {
                displayName = "Large Current Pass",
                referenceName = "PASS_LARGE_CURRENT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                stages = KeywordShaderStage.Fragment,
            };

            static readonly KeywordDescriptor ripplesKeyword = new KeywordDescriptor
            {
                displayName = "Ripples Current Pass",
                referenceName = "PASS_RIPPLES_CURRENT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                stages = KeywordShaderStage.Fragment,
            };

            static Dictionary<WaterDecal.PassType, KeywordDescriptor> s_PassDefines = new()
            {
                { WaterDecal.PassType.DeformationAndFoam, deformFoamKeyword },
                { WaterDecal.PassType.SimulationMask, maskKeyword },
                { WaterDecal.PassType.LargeCurrent, largeKeyword },
                { WaterDecal.PassType.RipplesCurrent, ripplesKeyword },
            };

            public static DefineCollection GetPassDefines(WaterDecal.PassType pass)
            {
                return new DefineCollection
                {
                    { s_PassDefines[pass], 1 }
                };
            }

            static readonly KeywordDescriptor AffectsDeformation = new KeywordDescriptor()
            {
                displayName = "Affects Deformation",
                referenceName = "_AFFECTS_DEFORMATION",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };
            static readonly KeywordDescriptor AffectsFoam = new KeywordDescriptor()
            {
                displayName = "Affects Foam",
                referenceName = "_AFFECTS_FOAM",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };
            static readonly KeywordDescriptor AffectsSimulationMask = new KeywordDescriptor()
            {
                displayName = "Affects Simulation Mask",
                referenceName = "_AFFECTS_MASK",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };
            static readonly KeywordDescriptor AffectsLargeCurrent = new KeywordDescriptor()
            {
                displayName = "Affects Large Current",
                referenceName = "_AFFECTS_LARGE_CURRENT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };
            static readonly KeywordDescriptor AffectsRipplesCurrent = new KeywordDescriptor()
            {
                displayName = "Affects Ripples Current",
                referenceName = "_AFFECTS_RIPPLES_CURRENT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };

            public static KeywordCollection GetPassKeywords(WaterDecal.PassType pass, WaterDecalData data)
            {
                KeywordCollection keywords = new();
                switch (pass)
                {
                    case WaterDecal.PassType.DeformationAndFoam:
                        keywords.Add(new KeywordCollection { { AffectsDeformation, new FieldCondition(WaterDecalFields.AffectsDeformation, data.affectsDeformation) } });
                        keywords.Add(new KeywordCollection { { AffectsFoam, new FieldCondition(WaterDecalFields.AffectsFoam, data.affectsFoam) } });
                        break;
                    case WaterDecal.PassType.SimulationMask:
                        keywords.Add(new KeywordCollection { { AffectsSimulationMask, new FieldCondition(WaterDecalFields.AffectsSimulationMask, data.affectsSimulationMask) } });
                        break;
                    case WaterDecal.PassType.LargeCurrent:
                        keywords.Add(new KeywordCollection { { AffectsLargeCurrent, new FieldCondition(WaterDecalFields.AffectsLargeCurrent, data.affectsLargeCurrent) } });
                        break;
                    case WaterDecal.PassType.RipplesCurrent:
                        keywords.Add(new KeywordCollection { { AffectsRipplesCurrent, new FieldCondition(WaterDecalFields.AffectsRipplesCurrent, data.affectsRipplesCurrent) } });
                        break;
                }

                return keywords;
            }
        }

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
            void AddHiddenProperty(string referenceName, bool value)
            {
                collector.AddShaderProperty(new BooleanShaderProperty
                {
                    overrideReferenceName = referenceName,
                    overrideHLSLDeclaration = true,
                    hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                    hidden = true,
                    value = value,
                });
            }

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

            bool decalWorkflow = GraphicsSettings.GetRenderPipelineSettings<WaterSystemGlobalSettings>()?.waterDecalMaskAndCurrent ?? false;

            if (m_Data.affectsDeformation)
                AddHiddenProperty(HDShaderIDs.kAffectsDeformation, true);
            if (m_Data.affectsFoam)
                AddHiddenProperty(HDShaderIDs.kAffectsFoam, true);
            if (m_Data.affectsSimulationMask)
                AddHiddenProperty(HDShaderIDs.kAffectsSimulationMask, decalWorkflow);
            if (m_Data.affectsLargeCurrent)
                AddHiddenProperty(HDShaderIDs.kAffectsLargeCurrent, decalWorkflow);
            if (m_Data.affectsRipplesCurrent)
                AddHiddenProperty(HDShaderIDs.kAffectsRipplesCurrent, decalWorkflow);
        }


        void AddProperty<F, T>(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo, GUIContent style, BaseField<F> field, Func<T> getter, Action<T> setter)
        {
            field.value = (F)(object)getter();

            context.AddProperty(style.text, style.tooltip, 0, field, (evt) =>
            {
                if (Equals(getter(), evt.newValue))
                    return;

                registerUndo(style.text);
                setter((T)(object)evt.newValue);
                onChange();
            });
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
        {
            AddProperty(ref context, onChange, registerUndo, Styles.affectsDeformationText, new Toggle(), () => m_Data.affectsDeformation, (v) => { m_Data.affectsDeformation = v; });
            AddProperty(ref context, onChange, registerUndo, Styles.affectsFoamText, new Toggle(), () => m_Data.affectsFoam, (v) => { m_Data.affectsFoam = v; });
            AddProperty(ref context, onChange, registerUndo, Styles.affectsSimulationMaskText, new Toggle(), () => m_Data.affectsSimulationMask, (v) => { m_Data.affectsSimulationMask = v; });
            AddProperty(ref context, onChange, registerUndo, Styles.affectsLargeCurrentText, new Toggle(), () => m_Data.affectsLargeCurrent, (v) => { m_Data.affectsLargeCurrent = v; });
            AddProperty(ref context, onChange, registerUndo, Styles.affectsRipplesCurrentText, new Toggle(), () => m_Data.affectsRipplesCurrent, (v) => { m_Data.affectsRipplesCurrent = v; });
        }

        [MenuItem("Assets/Create/Shader Graph/HDRP/Water Decal Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 7)]
        internal static void CreateWaterDecalGraph()
        {
            // Create an empty graph from scratch
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(WaterDecalSubTarget));

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, WaterDecalBlocks.GetPixelBlocks());
        }

        internal static Shader CreateWaterDecalGraphAtPath(string path)
        {
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(WaterDecalSubTarget));

            var graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new[] { target }, WaterDecalBlocks.GetPixelBlocks());

            graph.path = "Shader Graphs";
            FileUtilities.WriteShaderGraphToDisk(path, graph);
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<Shader>(path);
        }
    }
}
