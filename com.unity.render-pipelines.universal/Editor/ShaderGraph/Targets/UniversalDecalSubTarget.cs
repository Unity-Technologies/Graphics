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
    sealed class UniversalDecalSubTarget : SubTarget<UniversalTarget>
    {
        static readonly GUID kSourceCodeGuid = new GUID("f6cdcb0f9c306bf4895b74013d29ed47"); // UniversalDecalSubTarget.cs

        internal class Styles
        {
            public const string header = "Surface Options";

            public static GUIContent affectAlbedoText = new GUIContent("Affect BaseColor", "When enabled, this decal uses its base color. When disabled, the decal has no base color effect.");
            public static GUIContent affectNormalText = new GUIContent("Affect Normal", "When enabled, this decal uses its normal. When disabled, the decal has no normal effect.");
            public static GUIContent affectMetalText = new GUIContent("Affect Metal", "When enabled, this decal uses the metallic channel of its Mask Map. When disabled, the decal has no metallic effect.");
            public static GUIContent affectAmbientOcclusionText = new GUIContent("Affect Ambient Occlusion", "When enabled, this decal uses the smoothness channel of its Mask Map. When disabled, the decal has no smoothness effect.");
            public static GUIContent affectSmoothnessText = new GUIContent("Affect Smoothness", "When enabled, this decal uses the ambient occlusion channel of its Mask Map. When disabled, the decal has no ambient occlusion effect.");
            public static GUIContent affectEmissionText = new GUIContent("Affect Emission", "When enabled, this decal becomes emissive and appears self-illuminated. Affect Emission does not support Affects Transparent option on Decal Projector.");
            public static GUIContent supportLodCrossFadeText = new GUIContent("Support LOD CrossFade", "When enabled, this decal material supports LOD Cross fade if use on a Mesh.");
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
        }

        private const string kMaterial = "Material";
        private static FieldDescriptor AffectsAlbedo = new FieldDescriptor(kMaterial, "AffectsAlbedo", "");
        private static FieldDescriptor AffectsNormal = new FieldDescriptor(kMaterial, "AffectsNormal", "");
        private static FieldDescriptor AffectsMetal = new FieldDescriptor(kMaterial, "AffectsMetal", "");
        private static FieldDescriptor AffectsAO = new FieldDescriptor(kMaterial, "AffectsAO", "");
        private static FieldDescriptor AffectsSmoothness = new FieldDescriptor(kMaterial, "AffectsSmoothness", "");
        private static FieldDescriptor AffectsEmission = new FieldDescriptor(kMaterial, "AffectsEmission", "");
        private static FieldDescriptor DecalDefault = new FieldDescriptor(kMaterial, "DecalDefault", "");

        [System.Serializable]
        class DecalData
        {
            public bool affectsAlbedo = true;
            public bool affectsNormal = true;
            public bool affectsMetal;
            public bool affectsAO;
            public bool affectsSmoothness;
            public bool affectsEmission;
            public int drawOrder;
            public bool supportLodCrossFade;
        }

        [SerializeField]
        DecalData m_DecalData;

        [SerializeField]
        private DecalData decalData
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

            {
                SubShaderDescriptor subShader = SubShaders.Decal;

                var passes = new PassCollection();
                foreach (var pass in subShader.passes)
                {
                    var passDescriptor = pass.descriptor;

                    CollectPassDefines(ref passDescriptor);

                    CollectPassRenderState(ref passDescriptor);

                    passes.Add(passDescriptor, pass.fieldConditions);
                }
                subShader.passes = passes;

                context.AddSubShader(subShader);
            }
        }

        private void CollectPassDefines(ref PassDescriptor pass)
        {
            // Make copy to avoid overwriting static
            pass.defines = pass.defines == null ? new DefineCollection() : new DefineCollection() { pass.defines };

            // TODO: Check if we can move this to conditional fields

            // Emissive pass only have the emission keyword
            if (pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferProjector] ||
                pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh] ||
                pass.lightMode == "DecalPreview" ||
                pass.lightMode == "DecalScreenSpaceProjector" ||
                pass.lightMode == "DecalScreenSpaceMesh"
            )
            {
                if (decalData.affectsAlbedo)
                    pass.defines.Add(DecalDefines.AffectsAlbedo);
                if (decalData.affectsNormal)
                    pass.defines.Add(DecalDefines.AffectsNormal);
                if (decalData.affectsMetal)
                    pass.defines.Add(DecalDefines.AffectsMetal);
                if (decalData.affectsAO)
                    pass.defines.Add(DecalDefines.AffectsAO);
                if (decalData.affectsSmoothness)
                    pass.defines.Add(DecalDefines.AffectsSmoothness);
            }

            if (pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalMeshForwardEmissive] ||
                pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh] ||
                pass.lightMode == "DecalScreenSpaceMesh")
            {
                if (decalData.supportLodCrossFade)
                    pass.defines.Add(DecalDefines.SupportsLodCrossFade);
            }
        }

        private void CollectPassRenderState(ref PassDescriptor pass)
        {
            if (pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferProjector] ||
                pass.lightMode == DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh])
            {
                // Make copy to avoid overwriting static
                pass.renderStates = new RenderStateCollection() { pass.renderStates };

                if (decalData.affectsAlbedo)
                    pass.renderStates.Add(DecalColorMasks.ColorMaskRGBA0);
                else
                    pass.renderStates.Add(DecalColorMasks.ColorMaskNone0);

                if (decalData.affectsNormal)
                    pass.renderStates.Add(DecalColorMasks.ColorMaskRGBA1);
                else
                    pass.renderStates.Add(DecalColorMasks.ColorMaskNone1);

                string colorWriteMask = "";
                if (decalData.affectsMetal)
                    colorWriteMask += 'R';
                if (decalData.affectsAO)
                    colorWriteMask += 'G';
                if (decalData.affectsSmoothness)
                    colorWriteMask += 'B';
                if (colorWriteMask != "")
                    colorWriteMask += 'A';
                else
                    colorWriteMask = "0";
                pass.renderStates.Add(new RenderStateDescriptor() { type = RenderStateType.ColorMask, value = $"ColorMask {colorWriteMask} 2" });
            }
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

            void AddStencilProperty(string referenceName)
            {
                collector.AddShaderProperty(new Vector1ShaderProperty
                {
                    overrideReferenceName = referenceName,
                    floatType = FloatType.Integer,
                    hidden = true,
                });
            }
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            // Decal properties
            context.AddField(AffectsAlbedo, decalData.affectsAlbedo);
            context.AddField(AffectsNormal, decalData.affectsNormal);
            context.AddField(AffectsMetal, decalData.affectsMetal);
            context.AddField(AffectsAO, decalData.affectsAO);
            context.AddField(AffectsSmoothness, decalData.affectsSmoothness);
            context.AddField(AffectsEmission, decalData.affectsEmission);
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
            context.AddBlock(UniversalBlockFields.SurfaceDescription.NormalAlpha, decalData.affectsNormal);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic, decalData.affectsMetal);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion, decalData.affectsAO);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness, decalData.affectsSmoothness);
            context.AddBlock(UniversalBlockFields.SurfaceDescription.MAOSAlpha, decalData.affectsMetal | decalData.affectsSmoothness | decalData.affectsAO);
            context.AddBlock(BlockFields.SurfaceDescription.Emission, decalData.affectsEmission);
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

            context.AddProperty("Affect Ambient Occlusion", new Toggle() { value = decalData.affectsAO }, (evt) =>
            {
                if (Equals(decalData.affectsAO, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                decalData.affectsAO = (bool)evt.newValue;
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

            context.AddProperty("Affect Emission", new Toggle() { value = decalData.affectsEmission }, (evt) =>
            {
                if (Equals(decalData.affectsEmission, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                decalData.affectsEmission = (bool)evt.newValue;
                onChange();
            });

            context.AddProperty("Supports LOD Cross Fade", new Toggle() { value = decalData.supportLodCrossFade }, (evt) =>
            {
                if (Equals(decalData.supportLodCrossFade, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                decalData.supportLodCrossFade = (bool)evt.newValue;
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
                    { DecalPasses.DecalScreenSpaceProjector, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.DBufferMesh, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.DecalMeshForwardEmissive, new FieldCondition(AffectsEmission, true) },
                    { DecalPasses.DecalScreenSpaceMesh, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.ScenePicking, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.Preview, new FieldCondition(DecalDefault, true) },
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
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "Picking",
                useInPreview = false,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Collections
                renderStates = DecalRenderStates.ScenePicking,
                pragmas = DecalPragmas.Instanced,
                defines = DecalKeywords.ScenePicking,
                includes = DecalIncludes.ScenePicking,

                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
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
                keywords = DecalKeywords.Decals,
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
                defines = DecalDefines.AffectsEmission,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor DecalScreenSpaceProjector = new PassDescriptor()
            {
                // Definition
                displayName = "DecalScreenSpaceProjector",
                referenceName = "SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR",
                lightMode = "DecalScreenSpaceProjector",
                useInPreview = false,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentMeshEmissive, // todo

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.DecalScreenSpaceProjector,
                fieldDependencies = CoreFieldDependencies.Default,

                renderStates = DecalRenderStates.Preview,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines.DecalScreenSpace,
                keywords = DecalKeywords.Forward,
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
                requiredFields = DecalRequiredFields.DBufferMesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.DBufferMesh,
                pragmas = DecalPragmas.Instanced,
                keywords = DecalKeywords.Decals,
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
                requiredFields = DecalRequiredFields.DBufferMesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.DecalMeshForwardEmissive,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines.AffectsEmission,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor DecalScreenSpaceMesh = new PassDescriptor()
            {
                // Definition
                displayName = "DecalScreenSpaceMesh",
                referenceName = "SHADERPASS_DECAL_SCREEN_SPACE_MESH",
                lightMode = "DecalScreenSpaceMesh",
                useInPreview = true,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentMeshEmissive, // todo

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.DecalScreenSpaceMesh,
                fieldDependencies = CoreFieldDependencies.Default,

                renderStates = DecalRenderStates.Preview,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines.DecalScreenSpace,
                keywords = DecalKeywords.Forward,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor Preview = new PassDescriptor()
            {
                // Definition
                displayName = "DecalPreview",
                referenceName = "SHADERPASS_FORWARD_PREVIEW",
                lightMode = "DecalPreview",
                useInPreview = true,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentMeshEmissive,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Preview,
                fieldDependencies = CoreFieldDependencies.Default,

                // Render state overrides
                renderStates = DecalRenderStates.Preview,
                pragmas = DecalPragmas.Preview,
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
                UniversalBlockFields.SurfaceDescription.NormalAlpha,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Smoothness,
                UniversalBlockFields.SurfaceDescription.MAOSAlpha,
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
                UniversalBlockFields.SurfaceDescription.NormalAlpha,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Smoothness,

                UniversalBlockFields.SurfaceDescription.MAOSAlpha,
                BlockFields.SurfaceDescription.Emission,
            };
        }
        #endregion

        #region RequiredFields
        static class DecalRequiredFields
        {
            public static FieldCollection DBufferMesh = new FieldCollection()
            {
                StructFields.Attributes.normalOS,
                StructFields.Attributes.tangentOS,
                StructFields.Attributes.uv0,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.texCoord0,
            };

            public static FieldCollection DecalScreenSpaceProjector = new FieldCollection()
            {
                StructFields.Varyings.viewDirectionWS,
                UniversalStructFields.Varyings.lightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static FieldCollection DecalScreenSpaceMesh = new FieldCollection()
            {
                StructFields.Attributes.normalOS,
                StructFields.Attributes.tangentOS,
                StructFields.Attributes.uv0,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.viewDirectionWS,
                UniversalStructFields.Varyings.lightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static FieldCollection Preview = new FieldCollection()
            {
                StructFields.Attributes.normalOS,
                StructFields.Attributes.tangentOS,
                StructFields.Attributes.uv0,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.texCoord0,
            };
        }
        #endregion

        #region ColorMask
        static class DecalColorMasks
        {
            public static RenderStateDescriptor ColorMaskRGBA0 = RenderState.ColorMask("ColorMask RGBA");
            public static RenderStateDescriptor ColorMaskNone0 = RenderState.ColorMask("ColorMask 0");
            public static RenderStateDescriptor ColorMaskRGBA1 = RenderState.ColorMask("ColorMask RGBA 1");
            public static RenderStateDescriptor ColorMaskNone1 = RenderState.ColorMask("ColorMask 0 1");
            public static RenderStateDescriptor ColorMaskNone2 = RenderState.ColorMask("ColorMask 0 2");
        }
        #endregion

        #region RenderStates
        static class DecalRenderStates
        {
            private const string kDecalStencilWriteMask = "_DecalStencilWriteMask";
            private const string kDecalStencilRef = "_DecalStencilRef";
            private readonly static string[] s_DecalBlends = new string[4]
            {
                "Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha",
                "Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha",
                "Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha",
                "Blend 3 Zero OneMinusSrcColor"
            };

            public static RenderStateCollection ScenePicking = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Back) },
            };

            public static RenderStateCollection DBufferProjector = new RenderStateCollection
            {
                { RenderState.Blend(s_DecalBlends[0]) },
                { RenderState.Blend(s_DecalBlends[1]) },
                { RenderState.Blend(s_DecalBlends[2]) },
                { RenderState.Blend(s_DecalBlends[3]) },
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
                // Render State setup in dynamically
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
                { RenderState.Blend(s_DecalBlends[0]) },
                { RenderState.Blend(s_DecalBlends[1]) },
                { RenderState.Blend(s_DecalBlends[2]) },
                { RenderState.Blend(s_DecalBlends[3]) },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"[{kDecalStencilWriteMask}]",
                    Ref = $"[{kDecalStencilRef}]",
                    Comp = "Always",
                    Pass = "Replace",
                }) },
                // Render State setup in dynamically
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
                //{ new RenderStateDescriptor { type = RenderStateType.pr, value = "PreviewType Plane" } }
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

            public static PragmaCollection Preview = new PragmaCollection
            {
                { Pragma.Vertex("Vert") },
                { Pragma.Fragment("Frag") },
                { Pragma.EnableD3D11DebugSymbols },
            };
        }
        #endregion

        #region Defines
        static class DecalDefines
        {
            private static class Descriptors
            {
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

                public static KeywordDescriptor AffectsMetal = new KeywordDescriptor()
                {
                    displayName = "Affects Metal",
                    referenceName = "_MATERIAL_AFFECTS_METAL",
                    type = KeywordType.Boolean,
                };

                public static KeywordDescriptor AffectsAO = new KeywordDescriptor()
                {
                    displayName = "Affects Ambient Occlusion",
                    referenceName = "_MATERIAL_AFFECTS_AO",
                    type = KeywordType.Boolean,
                };

                public static KeywordDescriptor AffectsSmoothness = new KeywordDescriptor()
                {
                    displayName = "Affects Smoothness",
                    referenceName = "_MATERIAL_AFFECTS_SMOOTHNESS",
                    type = KeywordType.Boolean,
                };

                public static KeywordDescriptor AffectsMaskmap = new KeywordDescriptor()
                {
                    displayName = "Affects Maskmap",
                    referenceName = "_MATERIAL_AFFECTS_MASKMAP",
                    type = KeywordType.Boolean,
                };

                public static KeywordDescriptor AffectsEmission = new KeywordDescriptor()
                {
                    displayName = "Affects Emission",
                    referenceName = "_MATERIAL_AFFECTS_EMISSION",
                    type = KeywordType.Boolean,
                };

                public static KeywordDescriptor SupportsLodCrossFade = new KeywordDescriptor()
                {
                    displayName = "Supports LOD Cross Fade",
                    referenceName = "LOD_FADE_CROSSFADE",
                    type = KeywordType.Boolean,
                };

                public static KeywordDescriptor Decals3RT = new KeywordDescriptor()
                {
                    displayName = "Decals 3RT",
                    referenceName = "DECALS_3RT",
                    type = KeywordType.Boolean,
                };
            }

            public static DefineCollection AffectsAlbedo = new DefineCollection { { Descriptors.AffectsAlbedo, 1 }, };
            public static DefineCollection AffectsNormal = new DefineCollection { { Descriptors.AffectsNormal, 1 }, };
            public static DefineCollection AffectsMetal = new DefineCollection { { Descriptors.AffectsMetal, 1 }, };
            public static DefineCollection AffectsAO = new DefineCollection { { Descriptors.AffectsAO, 1 }, };
            public static DefineCollection AffectsSmoothness = new DefineCollection { { Descriptors.AffectsSmoothness, 1 }, };
            public static DefineCollection AffectsEmission = new DefineCollection { { Descriptors.AffectsEmission, 1 }, };
            public static DefineCollection SupportsLodCrossFade = new DefineCollection { { Descriptors.SupportsLodCrossFade, 1 }, };
            public static DefineCollection DecalScreenSpace = new DefineCollection { { Descriptors.AffectsEmission, 1 }, };
        }
        #endregion

        #region Keywords
        static class DecalKeywords
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
                        new KeywordEntry() { displayName = "1RT", referenceName = "1RT" },
                        new KeywordEntry() { displayName = "2RT", referenceName = "2RT" },
                        new KeywordEntry() { displayName = "3RT", referenceName = "3RT" },
                    }
                };

                public static KeywordDescriptor ScenePickingPass = new KeywordDescriptor()
                {
                    displayName = "Scene Picking Pass",
                    referenceName = "SCENEPICKINGPASS",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Local,
                };

                public static KeywordDescriptor DecalsNormalBlend = new KeywordDescriptor()
                {
                    displayName = "Decal Normal Blend",
                    referenceName = "DECALS_NORMAL_BLEND",
                    type = KeywordType.Enum,
                    definition = KeywordDefinition.MultiCompile,
                    scope = KeywordScope.Global,
                    entries = new KeywordEntry[]
                    {
                        new KeywordEntry() { displayName = "OFF", referenceName = "OFF" },
                        new KeywordEntry() { displayName = "LOW", referenceName = "LOW" },
                        new KeywordEntry() { displayName = "MEDIUM", referenceName = "MEDIUM" },
                        new KeywordEntry() { displayName = "HIGH", referenceName = "HIGH" },
                    }
                };
            }

            public static KeywordCollection Decals = new KeywordCollection { { Descriptors.Decals } };
            public static DefineCollection ScenePicking = new DefineCollection { { Descriptors.ScenePickingPass, 1 }, };

            public static readonly KeywordCollection Forward = new KeywordCollection
            {
                { CoreKeywordDescriptors.Lightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.AdditionalLights },
                { CoreKeywordDescriptors.AdditionalLightShadows },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { Descriptors.DecalsNormalBlend },
            };
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
