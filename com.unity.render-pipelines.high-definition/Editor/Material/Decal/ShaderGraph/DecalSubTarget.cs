using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class DecalSubTarget : HDSubTarget, ILegacyTarget, IRequiresData<DecalData>
    {
        public DecalSubTarget() => displayName = "Decal";

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("3ec927dfcb5d60e4883b2c224857b6c2");  // DecalSubTarget.cs

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template";
        protected override string[] templateMaterialDirectories =>  new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/"
        };
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override string customInspector => "Rendering.HighDefinition.DecalGUI";
        protected override string renderType => HDRenderTypeTags.Opaque.ToString();
        protected override string renderQueue => HDRenderQueue.GetShaderTagValue(HDRenderQueue.ChangeType(HDRenderQueue.RenderQueueType.Opaque, decalData.drawOrder, false, false));
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Decal;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Decal Subshader", "");
        protected override string subShaderInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl";

        // Material Data
        DecalData m_DecalData;

        // Interface Properties
        DecalData IRequiresData<DecalData>.data
        {
            get => m_DecalData;
            set => m_DecalData = value;
        }

        // Public properties
        public DecalData decalData
        {
            get => m_DecalData;
            set => m_DecalData = value;
        }

        protected override IEnumerable<SubShaderDescriptor> EnumerateSubShaders()
        {
            yield return PostProcessSubShader(SubShaders.Decal);
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            pass.keywords.Add(CoreKeywordDescriptors.AlphaTest, new FieldCondition(Fields.AlphaTest, true));

            // Emissive pass only have the emission keyword
            if (!(pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalProjectorForwardEmissive] ||
                pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalMeshForwardEmissive]))
            {
                if (decalData.affectsAlbedo)
                    pass.keywords.Add(DecalDefines.Albedo);
                if (decalData.affectsNormal)
                    pass.keywords.Add(DecalDefines.Normal);
                if (decalData.affectsMaskmap)
                    pass.keywords.Add(DecalDefines.Maskmap);
            }

            if (pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalMeshForwardEmissive] ||
                pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh])
            {
                pass.keywords.Add(CoreKeywordDescriptors.LodFadeCrossfade, new FieldCondition(Fields.LodCrossFade, true));
            }
        }

        public static FieldDescriptor AffectsAlbedo =           new FieldDescriptor(kMaterial, "AffectsAlbedo", "");
        public static FieldDescriptor AffectsNormal =           new FieldDescriptor(kMaterial, "AffectsNormal", "");
        public static FieldDescriptor AffectsEmission =         new FieldDescriptor(kMaterial, "AffectsEmission", "");
        public static FieldDescriptor AffectsMetal =            new FieldDescriptor(kMaterial, "AffectsMetal", "");
        public static FieldDescriptor AffectsAO =               new FieldDescriptor(kMaterial, "AffectsAO", "");
        public static FieldDescriptor AffectsSmoothness =       new FieldDescriptor(kMaterial, "AffectsSmoothness", "");
        public static FieldDescriptor AffectsMaskMap =          new FieldDescriptor(kMaterial, "AffectsMaskMap", "");
        public static FieldDescriptor DecalDefault =            new FieldDescriptor(kMaterial, "DecalDefault", "");

        public override void GetFields(ref TargetFieldContext context)
        {
            // Decal properties
            context.AddField(AffectsAlbedo,        decalData.affectsAlbedo);
            context.AddField(AffectsNormal,        decalData.affectsNormal);
            context.AddField(AffectsEmission,      decalData.affectsEmission);
            context.AddField(AffectsMetal,         decalData.affectsMetal);
            context.AddField(AffectsAO,            decalData.affectsAO);
            context.AddField(AffectsSmoothness,    decalData.affectsSmoothness);
            context.AddField(AffectsMaskMap,       decalData.affectsMaskmap);
            context.AddField(DecalDefault,         decalData.affectsAlbedo || decalData.affectsNormal || decalData.affectsMetal ||
                                                                    decalData.affectsAO || decalData.affectsSmoothness );
            context.AddField(Fields.LodCrossFade, decalData.supportLodCrossFade);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);

            // Decal
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS);
            context.AddBlock(HDBlockFields.SurfaceDescription.NormalAlpha);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(HDBlockFields.SurfaceDescription.MAOSAlpha);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new DecalPropertyBlock(decalData));
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            Vector1ShaderProperty drawOrder = new Vector1ShaderProperty();
            drawOrder.overrideReferenceName = "_DrawOrder";
            drawOrder.displayName = "Draw Order";
            drawOrder.floatType = FloatType.Integer;
            drawOrder.hidden = true;
            drawOrder.value = 0;
            collector.AddShaderProperty(drawOrder);

            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                overrideReferenceName = "_DecalMeshBiasType",
                floatType = FloatType.Enum,
                value = (int)DecalMeshDepthBiasType.DepthBias,
                enumNames = { "Depth Bias", "View Bias" },
                enumValues = { (int)DecalMeshDepthBiasType.DepthBias, (int)DecalMeshDepthBiasType.ViewBias},
                hidden = true
            });


            Vector1ShaderProperty decalMeshDepthBias = new Vector1ShaderProperty();
            decalMeshDepthBias.overrideReferenceName = "_DecalMeshDepthBias";
            decalMeshDepthBias.displayName = "DecalMesh DepthBias";
            decalMeshDepthBias.hidden = true;
            decalMeshDepthBias.floatType = FloatType.Default;
            decalMeshDepthBias.value = 0;
            collector.AddShaderProperty(decalMeshDepthBias);

            Vector1ShaderProperty decalMeshViewBias = new Vector1ShaderProperty();
            decalMeshViewBias.overrideReferenceName = "_DecalMeshViewBias";
            decalMeshViewBias.displayName = "DecalMesh ViewBias";
            decalMeshViewBias.hidden = true;
            decalMeshViewBias.floatType = FloatType.Default;
            decalMeshViewBias.value = 0;
            collector.AddShaderProperty(decalMeshViewBias);

            AddStencilProperty(HDMaterialProperties.kDecalStencilWriteMask);
            AddStencilProperty(HDMaterialProperties.kDecalStencilRef);

            if (decalData.affectsAlbedo)
                AddAffectsProperty(HDMaterialProperties.kAffectAlbedo);
            if (decalData.affectsNormal)
                AddAffectsProperty(HDMaterialProperties.kAffectNormal);
            if (decalData.affectsAO)
                AddAffectsProperty(HDMaterialProperties.kAffectAO);
            if (decalData.affectsMetal)
                AddAffectsProperty(HDMaterialProperties.kAffectMetal);
            if (decalData.affectsSmoothness)
                AddAffectsProperty(HDMaterialProperties.kAffectSmoothness);
            if (decalData.affectsEmission)
                AddAffectsProperty(HDMaterialProperties.kAffectEmission);

            // Color mask configuration for writing to the mask map
            AddColorMaskProperty(kDecalColorMask0);
            AddColorMaskProperty(kDecalColorMask1);
            AddColorMaskProperty(kDecalColorMask2);
            AddColorMaskProperty(kDecalColorMask3);

            void AddAffectsProperty(string referenceName)
            {
                collector.AddShaderProperty(new BooleanShaderProperty{
                    overrideReferenceName = referenceName,
                    hidden = true,
                    value = true,
                });
            }

            void AddStencilProperty(string referenceName)
            {
                collector.AddShaderProperty(new Vector1ShaderProperty
                {
                    overrideReferenceName = referenceName,
                    floatType = FloatType.Integer,
                    hidden = true,
                });
            }

            void AddColorMaskProperty(string referenceName)
            {
                collector.AddShaderProperty(new Vector1ShaderProperty{
                    overrideReferenceName = referenceName,
                    floatType = FloatType.Integer,
                    hidden = true,
                });
            }
        }

#region SubShaders
        static class SubShaders
        {
            // Relies on the order shader passes are declared in DecalSystem.cs
            public static SubShaderDescriptor Decal = new SubShaderDescriptor()
            {
                generatesPreview = true,
                passes = new PassCollection
                {
                    { DecalPasses.DBufferProjector, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.DecalProjectorForwardEmissive, new FieldCondition(AffectsEmission, true) },
                    { DecalPasses.DBufferMesh, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.DecalMeshForwardEmissive, new FieldCondition(AffectsEmission, true) },
                    { DecalPasses.ScenePicking, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.Preview, new FieldCondition(Fields.IsPreview, true) },
                },
            };
        }
        #endregion

        #region Passes
        public static class DecalPasses
        {
            // CAUTION: c# code relies on the order in which the passes are declared, any change will need to be reflected in Decalsystem.cs - enum MaterialDecalPass

            public static PassDescriptor ScenePicking = new PassDescriptor()
            {
                // Definition
                displayName = "ScenePickingPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "Picking",
                useInPreview = false,

                // Collections
                renderStates = DecalRenderStates.ScenePicking,
                pragmas = DecalPragmas.Instanced,
                defines = CoreDefines.ScenePicking,
                includes = DecalIncludes.ScenePicking,
            };

            public static PassDescriptor DBufferProjector = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferProjector],
                referenceName = "SHADERPASS_DBUFFER_PROJECTOR",
                lightMode = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferProjector],
                useInPreview = false,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentDefault,

                //Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = DecalRenderStates.DBufferProjector,
                pragmas = DecalPragmas.Instanced,
                keywords = DecalDefines.Decals,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor DecalProjectorForwardEmissive = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalProjectorForwardEmissive],
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_PROJECTOR",
                lightMode = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalProjectorForwardEmissive],
                useInPreview = false,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentEmissive,

                //Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.DecalProjectorForwardEmissive,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines.Emission,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor DBufferMesh = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh],
                referenceName = "SHADERPASS_DBUFFER_MESH",
                lightMode = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh],
                useInPreview = false,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentDefault,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.DBufferMesh,
                pragmas = DecalPragmas.Instanced,
                keywords = DecalDefines.Decals,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor DecalMeshForwardEmissive = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalMeshForwardEmissive],
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_MESH",
                lightMode = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalMeshForwardEmissive],
                useInPreview = false,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentMeshEmissive,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.DecalMeshForwardEmissive,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines.Emission,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor Preview = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_PREVIEW",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentMeshEmissive,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Render state overrides
                renderStates = DecalRenderStates.Preview,
                pragmas = DecalPragmas.Instanced,
                includes = DecalIncludes.Default,
            };
        }
#endregion

#region BlockMasks
        static class DecalBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentDefault = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.NormalAlpha,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Smoothness,
                HDBlockFields.SurfaceDescription.MAOSAlpha,
            };

            public static BlockFieldDescriptor[] FragmentEmissive = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Emission,
            };

            public static BlockFieldDescriptor[] FragmentMeshEmissive = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.NormalAlpha,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Smoothness,
                HDBlockFields.SurfaceDescription.MAOSAlpha,
                BlockFields.SurfaceDescription.Emission,
            };
        }
#endregion

#region RequiredFields
        static class DecalRequiredFields
        {
            public static FieldCollection Mesh = new FieldCollection()
            {
                HDStructFields.AttributesMesh.normalOS,
                HDStructFields.AttributesMesh.tangentOS,
                HDStructFields.AttributesMesh.uv0,
                HDStructFields.FragInputs.tangentToWorld,
                HDStructFields.FragInputs.positionRWS,
                HDStructFields.FragInputs.texCoord0,
            };
        }
#endregion

#region RenderStates
        static class DecalRenderStates
        {
            readonly static string s_DecalColorMask = "ColorMask [_DecalColorMask0]\n\tColorMask [_DecalColorMask1] 1\n\tColorMask [_DecalColorMask2] 2\n\tColorMask [_DecalColorMask3] 3";
            readonly static string s_DecalBlend = "Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha \n\tBlend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha \n\tBlend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha \n\tBlend 3 Zero OneMinusSrcColor";

            public static RenderStateCollection ScenePicking = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Back) },
            };

            public static RenderStateCollection DBufferProjector = new RenderStateCollection
            {
                { RenderState.Blend(s_DecalBlend) },
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"[{kDecalStencilWriteMask}]",
                    Ref = $"[{kDecalStencilRef}]",
                    Comp = "Always",
                    Pass = "Replace",
                }) },

                { RenderState.ColorMask(s_DecalColorMask) }
            };

            public static RenderStateCollection DecalProjectorForwardEmissive = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha One") },
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection DBufferMesh = new RenderStateCollection
            {
                { RenderState.Blend(s_DecalBlend) },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"[{kDecalStencilWriteMask}]",
                    Ref = $"[{kDecalStencilRef}]",
                    Comp = "Always",
                    Pass = "Replace",
                }) },
                { RenderState.ColorMask(s_DecalColorMask) }
            };

            public static RenderStateCollection DecalMeshForwardEmissive = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha One") },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection Preview = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
            };
        }
#endregion

#region Pragmas
        static class DecalPragmas
        {
            public static PragmaCollection Instanced = new PragmaCollection
            {
                { CorePragmas.Basic },
                { Pragma.MultiCompileInstancing },
#if ENABLE_HYBRID_RENDERER_V2
                { Pragma.DOTSInstancing },
#endif
            };
        }
        #endregion

        #region Defines
        static class DecalDefines
        {
            static class Descriptors
            {
                public static KeywordDescriptor Decals = new KeywordDescriptor()
                {
                    displayName = "Decals",
                    referenceName = "DECALS",
                    type = KeywordType.Enum,
                    definition = KeywordDefinition.MultiCompile,
                    scope = KeywordScope.Global,
                    entries = new KeywordEntry[]
                    {
                        new KeywordEntry() { displayName = "3RT", referenceName = "3RT" },
                        new KeywordEntry() { displayName = "4RT", referenceName = "4RT" },
                    }
                };

                public static KeywordDescriptor AffectsAlbedo = new KeywordDescriptor()
                {
                    displayName = "Affects Albedo",
                    referenceName = "_MATERIAL_AFFECTS_ALBEDO",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Global,
                };

                public static KeywordDescriptor AffectsNormal = new KeywordDescriptor()
                {
                    displayName = "Affects Normal",
                    referenceName = "_MATERIAL_AFFECTS_NORMAL",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Global,
                };

                public static KeywordDescriptor AffectsMaskmap = new KeywordDescriptor()
                {
                    displayName = "Affects Maskmap",
                    referenceName = "_MATERIAL_AFFECTS_MASKMAP",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Global,
                };

                public static KeywordDescriptor AffectsEmission = new KeywordDescriptor()
                {
                    displayName = "Affects Emission",
                    referenceName = "_MATERIAL_AFFECTS_EMISSION",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Global,
                };
            }

            public static KeywordCollection Albedo = new KeywordCollection { { Descriptors.AffectsAlbedo, new FieldCondition(AffectsAlbedo, true) } };
            public static KeywordCollection Normal = new KeywordCollection { { Descriptors.AffectsNormal, new FieldCondition(AffectsNormal, true) } };
            public static KeywordCollection Maskmap = new KeywordCollection { { Descriptors.AffectsMaskmap, new FieldCondition(AffectsMaskMap, true) } };
            public static DefineCollection Emission = new DefineCollection { { Descriptors.AffectsEmission, 1 } };

            public static KeywordCollection Decals = new KeywordCollection { { Descriptors.Decals } };
        }
#endregion

#region Includes
        static class DecalIncludes
        {
            const string kPacking = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl";
            const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
            const string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
            const string kDecal = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl";
            const string kPassDecal = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl";

            public static IncludeCollection Default = new IncludeCollection
            {
                { kPacking, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.MinimalCorePregraph },
                { kDecal, IncludeLocation.Pregraph },
                { kPassDecal, IncludeLocation.Postgraph },
            };

            public static IncludeCollection ScenePicking = new IncludeCollection
            {
                { kPacking, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.MinimalCorePregraph },
                { kDecal, IncludeLocation.Pregraph },
                { CoreIncludes.kPickingSpaceTransforms, IncludeLocation.Pregraph },
                { kPassDecal, IncludeLocation.Postgraph },
            };
        }
#endregion
    }
}
