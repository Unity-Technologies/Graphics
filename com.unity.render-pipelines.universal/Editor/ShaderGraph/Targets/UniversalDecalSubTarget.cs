using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;
using UnityEngine.Rendering.Universal;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    [System.Serializable]
    class DecalData
    {
        [SerializeField]
        bool m_AffectsMetal = true;
        public bool affectsMetal
        {
            get => m_AffectsMetal;
            set => m_AffectsMetal = value;
        }

        [SerializeField]
        bool m_AffectsAO = false;
        public bool affectsAO
        {
            get => m_AffectsAO;
            set => m_AffectsAO = value;
        }

        [SerializeField]
        bool m_AffectsSmoothness = true;
        public bool affectsSmoothness
        {
            get => m_AffectsSmoothness;
            set => m_AffectsSmoothness = value;
        }

        [SerializeField]
        bool m_AffectsAlbedo = true;
        public bool affectsAlbedo
        {
            get => m_AffectsAlbedo;
            set => m_AffectsAlbedo = value;
        }

        [SerializeField]
        bool m_AffectsNormal = true;
        public bool affectsNormal
        {
            get => m_AffectsNormal;
            set => m_AffectsNormal = value;
        }

        [SerializeField]
        bool m_AffectsEmission = false;
        public bool affectsEmission
        {
            get => m_AffectsEmission;
            set => m_AffectsEmission = value;
        }

        [SerializeField]
        int m_DrawOrder;
        public int drawOrder
        {
            get => m_DrawOrder;
            set => m_DrawOrder = value;
        }

        [SerializeField]
        bool m_SupportLodCrossFade;
        public bool supportLodCrossFade
        {
            get => m_SupportLodCrossFade;
            set => m_SupportLodCrossFade = value;
        }

        public bool affectsMaskmap => affectsSmoothness || affectsMetal || affectsAO;
    }

    sealed class UniversalDecalSubTarget : SubTarget<UniversalTarget>
    {
        static readonly GUID kSourceCodeGuid = new GUID("d6c78107b64145745805d963de80cc17"); // UniversalLitSubTarget.cs

        [SerializeField]
        DecalData m_DecalData;

        public DecalData decalData
        {
            get
            {
                if (m_DecalData == null)
                    m_DecalData = new DecalData();
                return m_DecalData;
            }
        }

        public UniversalDecalSubTarget()
        {
            displayName = "Decal";
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddCustomEditorForRenderPipeline("UnityEditor.Rendering.Universal.DecalShaderGraphGUI", typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)); // TODO: This should be owned by URP

            // Process SubShaders
            SubShaderDescriptor subShader = SubShaders.Decal;

            var passes = new PassCollection();
            foreach (var pass in subShader.passes)
            {
                var passDescriptor = pass.descriptor;

                //passDescriptor.keywords = pass.descriptor.keywords == null ? new KeywordCollection() : new KeywordCollection() { pass.descriptor.keywords };
                //CollectPassKeywords(ref passDescriptor);

                passDescriptor.defines = pass.descriptor.defines == null ? new DefineCollection() : new DefineCollection() { pass.descriptor.defines };
                CollectPassDefines(ref passDescriptor);

                passes.Add(passDescriptor);
            }
            subShader.passes = passes;

            // Update Render State
            subShader.renderType = target.renderType;
            subShader.renderQueue = target.renderQueue;

            context.AddSubShader(subShader);
        }

        private void CollectPassDefines(ref PassDescriptor pass)
        {
            //pass.defines.Add(CoreKeywordDescriptors.AlphaTest, new FieldCondition(Fields.AlphaTest, true));

            // Emissive pass only have the emission keyword
            if (!(pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalProjectorForwardEmissive] ||
                  pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalMeshForwardEmissive]))
            {
                if (decalData.affectsAlbedo)
                    pass.defines.Add(DecalDefines.AffectsAlbedoDefine);
                if (decalData.affectsNormal)
                    pass.defines.Add(DecalDefines.AffectsNormalDefine);
                if (decalData.affectsMetal || decalData.affectsAO || decalData.affectsSmoothness)
                    pass.defines.Add(DecalDefines.AffectsMaskmapDefine);
            }

            if (pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalMeshForwardEmissive] ||
                pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh])
            {
                //pass.defines.Add(CoreKeywordDescriptors.LodFadeCrossfade, new FieldCondition(Fields.LodCrossFade, true));
            }
        }

        public struct HDMaterialProperties
        {
            internal const string kStencilRef = "_StencilRef";
            internal const string kStencilWriteMask = "_StencilWriteMask";
            internal const string kStencilRefDepth = "_StencilRefDepth";
            internal const string kStencilWriteMaskDepth = "_StencilWriteMaskDepth";
            internal const string kStencilRefGBuffer = "_StencilRefGBuffer";
            internal const string kStencilWriteMaskGBuffer = "_StencilWriteMaskGBuffer";
            internal const string kStencilRefMV = "_StencilRefMV";
            internal const string kStencilWriteMaskMV = "_StencilWriteMaskMV";
            internal const string kStencilRefDistortionVec = "_StencilRefDistortionVec";
            internal const string kStencilWriteMaskDistortionVec = "_StencilWriteMaskDistortionVec";
            internal const string kDecalStencilWriteMask = "_DecalStencilWriteMask";
            internal const string kDecalStencilRef = "_DecalStencilRef";

            internal const string kDecalColorMask0 = "_DecalColorMask0";
            internal const string kDecalColorMask1 = "_DecalColorMask1";
            internal const string kDecalColorMask2 = "_DecalColorMask2";
            internal const string kDecalColorMask3 = "_DecalColorMask3";
            internal const string kEnableDecals = "_SupportDecals";

            /// <summary>Enable affect Albedo (decal only).</summary>
            public const string kAffectAlbedo = "_AffectAlbedo";
            /// <summary>Enable affect Normal (decal only.</summary>
            public const string kAffectNormal = "_AffectNormal";
            /// <summary>Enable affect AO (decal only.</summary>
            public const string kAffectAO = "_AffectAO";
            /// <summary>Enable affect Metal (decal only.</summary>
            public const string kAffectMetal = "_AffectMetal";
            /// <summary>Enable affect Smoothness (decal only.</summary>
            public const string kAffectSmoothness = "_AffectSmoothness";
            /// <summary>Enable affect Emission (decal only.</summary>
            public const string kAffectEmission = "_AffectEmission";
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
                enumValues = { (int)DecalMeshDepthBiasType.DepthBias, (int)DecalMeshDepthBiasType.ViewBias },
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
            AddColorMaskProperty(HDMaterialProperties.kDecalColorMask0);
            AddColorMaskProperty(HDMaterialProperties.kDecalColorMask1);
            AddColorMaskProperty(HDMaterialProperties.kDecalColorMask2);
            AddColorMaskProperty(HDMaterialProperties.kDecalColorMask3);

            void AddAffectsProperty(string referenceName)
            {
                collector.AddShaderProperty(new BooleanShaderProperty
                {
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
                collector.AddShaderProperty(new Vector1ShaderProperty
                {
                    overrideReferenceName = referenceName,
                    floatType = FloatType.Integer,
                    hidden = true,
                });
            }
        }

        public const string kMaterial = "Material";
        public static FieldDescriptor AffectsAlbedo = new FieldDescriptor(kMaterial, "AffectsAlbedo", "");
        public static FieldDescriptor AffectsNormal = new FieldDescriptor(kMaterial, "AffectsNormal", "");
        public static FieldDescriptor AffectsEmission = new FieldDescriptor(kMaterial, "AffectsEmission", "");
        public static FieldDescriptor AffectsMetal = new FieldDescriptor(kMaterial, "AffectsMetal", "");
        public static FieldDescriptor AffectsAO = new FieldDescriptor(kMaterial, "AffectsAO", "");
        public static FieldDescriptor AffectsSmoothness = new FieldDescriptor(kMaterial, "AffectsSmoothness", "");
        public static FieldDescriptor AffectsMaskMap = new FieldDescriptor(kMaterial, "AffectsMaskMap", "");
        public static FieldDescriptor DecalDefault = new FieldDescriptor(kMaterial, "DecalDefault", "");

        public override void GetFields(ref TargetFieldContext context)
        {
            // Decal properties
            context.AddField(AffectsAlbedo, decalData.affectsAlbedo);
            context.AddField(AffectsNormal, decalData.affectsNormal);
            context.AddField(AffectsEmission, decalData.affectsEmission);
            context.AddField(AffectsMetal, decalData.affectsMetal);
            context.AddField(AffectsAO, decalData.affectsAO);
            context.AddField(AffectsSmoothness, decalData.affectsSmoothness);
            context.AddField(AffectsMaskMap, decalData.affectsMaskmap);
            context.AddField(DecalDefault, decalData.affectsAlbedo || decalData.affectsNormal || decalData.affectsMetal ||
                decalData.affectsAO || decalData.affectsSmoothness);
            context.AddField(Fields.LodCrossFade, decalData.supportLodCrossFade);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);

            // Decal
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor, decalData.affectsAlbedo);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha, decalData.affectsAlbedo);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS, decalData.affectsNormal);
            context.AddBlock(BlockFields.SurfaceDescription.NormalAlpha, decalData.affectsNormal);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic, decalData.affectsMetal);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion, decalData.affectsAO);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness, decalData.affectsSmoothness);
            context.AddBlock(BlockFields.SurfaceDescription.MAOSAlpha, decalData.affectsMetal | decalData.affectsSmoothness | decalData.affectsAO);
            context.AddBlock(BlockFields.SurfaceDescription.Emission, decalData.affectsEmission);
        }

        internal class Styles
        {
            public const string header = "Surface Options";

            public static GUIContent affectAlbedoText = new GUIContent("Affect BaseColor", "When enabled, this decal uses its base color. When disabled, the decal has no base color effect.");
            public static GUIContent affectNormalText = new GUIContent("Affect Normal", "When enabled, this decal uses its normal. When disabled, the decal has nonormal effect.");
            public static GUIContent affectMetalText = new GUIContent("Affect Metal", "When enabled, this decal uses the metallic channel of its Mask Map. When disabled, the decal has no metallic effect.");
            public static GUIContent affectAmbientOcclusionText = new GUIContent("Affect Ambient Occlusion", "When enabled, this decal uses the smoothness channel of its Mask Map. When disabled, the decal has no smoothness effect.");
            public static GUIContent affectSmoothnessText = new GUIContent("Affect Smoothness", "When enabled, this decal uses the ambient occlusion channel of its Mask Map. When disabled, the decal has no ambient occlusion effect.");
            public static GUIContent affectEmissionText = new GUIContent("Affect Emission", "When enabled, this decal becomes emissive and appears self-illuminated. Affect Emission does not support Affects Transparents option on Decal Projector.");
            public static GUIContent supportLodCrossFadeText = new GUIContent("Support LOD CrossFade", "When enabled, this decal material supports LOD Cross fade if use on a Mesh.");
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddProperty("Affect BaseColor", new Toggle() { value = decalData.affectsAlbedo }, (evt) =>
            {
                if (Equals(decalData.affectsAlbedo, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                decalData.affectsAlbedo = (bool)evt.newValue;
                onChange();
            });

            context.AddProperty("Affect Normal", new Toggle() { value = decalData.affectsNormal }, (evt) =>
            {
                if (Equals(decalData.affectsNormal, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                decalData.affectsNormal = (bool)evt.newValue;
                onChange();
            });

            context.AddProperty("Affect Metal", new Toggle() { value = decalData.affectsMetal }, (evt) =>
            {
                if (Equals(decalData.affectsMetal, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                decalData.affectsMetal = (bool)evt.newValue;
                onChange();
            });

            context.AddProperty("Affect Smoothness", new Toggle() { value = decalData.affectsSmoothness }, (evt) =>
            {
                if (Equals(decalData.affectsSmoothness, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                decalData.affectsSmoothness = (bool)evt.newValue;
                onChange();
            });

            context.AddProperty("Affect Ambient Occlusion", new Toggle() { value = decalData.affectsAO }, (evt) =>
            {
                if (Equals(decalData.affectsAO, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                decalData.affectsAO = (bool)evt.newValue;
                onChange();
            });
        }

        #region SubShader
        static class SubShaders
        {
            // Relies on the order shader passes are declared in DecalSystem.cs
            public static SubShaderDescriptor Decal = new SubShaderDescriptor()
            {
                pipelineTag = UniversalTarget.kPipelineTag,
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
        static class DecalPasses
        {
            // CAUTION: c# code relies on the order in which the passes are declared, any change will need to be reflected in Decalsystem.cs - enum MaterialDecalPass

            public static PassDescriptor ScenePicking = new PassDescriptor()
            {
                // Definition
                displayName = "ScenePickingPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "Picking",
                useInPreview = false,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Collections
                renderStates = DecalRenderStates.ScenePicking,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines.ScenePicking,
                includes = DecalIncludes.ScenePicking,
            };

            public static PassDescriptor DBufferProjector = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferProjector],
                referenceName = "SHADERPASS_DBUFFER_PROJECTOR",
                lightMode = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferProjector],
                useInPreview = false,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

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

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

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

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

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

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

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

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),


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

        #region PortMasks
        static class DecalBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentDefault = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalAlpha,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.MAOSAlpha,
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
                BlockFields.SurfaceDescription.NormalAlpha,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Smoothness,

                BlockFields.SurfaceDescription.MAOSAlpha,
                BlockFields.SurfaceDescription.Emission,
            };
        }
        #endregion

        #region RequiredFields
        static class DecalRequiredFields
        {
            public static FieldCollection Mesh = new FieldCollection()
            {
                StructFields.Attributes.normalOS,
                StructFields.Attributes.tangentOS,
                StructFields.Attributes.uv0,
                StructFields.Varyings.tangentToWorld,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.texCoord0,
            };
        }
        #endregion

        #region RenderStates
        static class DecalRenderStates
        {
            internal const string kDecalStencilWriteMask = "_DecalStencilWriteMask";
            internal const string kDecalStencilRef = "_DecalStencilRef";
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
                { Pragma.Target(ShaderModel.Target45) },
                { Pragma.ExcludeRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore }) },
                { Pragma.Vertex("Vert") },
                { Pragma.Fragment("Frag") },
                { Pragma.EnableD3D11DebugSymbols },
                { Pragma.MultiCompileInstancing },
#if ENABLE_HYBRID_RENDERER_V2
                { Pragma.DOTSInstancing },
#endif
            };
        }
        #endregion

        #region Keywords
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

                public static KeywordDescriptor ScenePickingPass = new KeywordDescriptor()
                {
                    displayName = "Scene Picking Pass",
                    referenceName = "SCENEPICKINGPASS",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Local,
                };
            }

            public static KeywordCollection Albedo = new KeywordCollection { { Descriptors.AffectsAlbedo, new FieldCondition(AffectsAlbedo, true) } };
            public static KeywordCollection Normal = new KeywordCollection { { Descriptors.AffectsNormal, new FieldCondition(AffectsNormal, true) } };
            public static KeywordCollection Maskmap = new KeywordCollection { { Descriptors.AffectsMaskmap, new FieldCondition(AffectsMaskMap, true) } };
            public static DefineCollection Emission = new DefineCollection { { Descriptors.AffectsEmission, 1 } };

            public static KeywordCollection Decals = new KeywordCollection { { Descriptors.Decals } };

            public static DefineCollection ScenePicking = new DefineCollection { { Descriptors.ScenePickingPass, 1 }, };

            public static DefineCollection AffectsAlbedoDefine = new DefineCollection { { Descriptors.AffectsAlbedo, 1 }, };
            public static DefineCollection AffectsNormalDefine = new DefineCollection { { Descriptors.AffectsNormal, 1 }, };
            public static DefineCollection AffectsMaskmapDefine = new DefineCollection { { Descriptors.AffectsMaskmap, 1 }, };
        }
        #endregion

        #region Includes
        static class DecalIncludes
        {
            const string kUnityInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl";
            const string kSpaceTransforms = "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl";
            const string kInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl";
            const string kPacking = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl";
            const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
            const string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
            const string kShaderVariablesDecal = "Packages/com.unity.render-pipelines.universal/Runtime/Decal/ShaderVariablesDecal.hlsl";
            const string kDecal = "Packages/com.unity.render-pipelines.universal/Runtime/Decal/Decal.hlsl";
            const string kPassDecal = "Packages/com.unity.render-pipelines.universal/Runtime/Decal/ShaderPassDecal.hlsl";

            public static IncludeCollection Default = new IncludeCollection
            {
                { kPacking, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
                { kUnityInput, IncludeLocation.Pregraph },
                //{ kVaryings, IncludeLocation.Pregraph },
                { kInput, IncludeLocation.Pregraph },
                { kShaderVariablesDecal, IncludeLocation.Pregraph },
                { kDecal, IncludeLocation.Pregraph },
                //{ kSpaceTransforms, IncludeLocation.Pregraph },
                { kPassDecal, IncludeLocation.Postgraph },
            };

            public static IncludeCollection ScenePicking = new IncludeCollection
            {
                { kPacking, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
                { kUnityInput, IncludeLocation.Pregraph },

                { kInput, IncludeLocation.Pregraph },
                { kShaderVariablesDecal, IncludeLocation.Pregraph },
                { kDecal, IncludeLocation.Pregraph },
                { kSpaceTransforms, IncludeLocation.Pregraph },
                { kPassDecal, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
