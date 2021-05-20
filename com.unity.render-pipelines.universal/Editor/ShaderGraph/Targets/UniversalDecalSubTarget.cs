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
            public static GUIContent affectNormalBlendText = new GUIContent("Affect Normal Blend", "When enabled, this decal uses its normal blending. When disabled, the decal has no normal blending effect.");
            public static GUIContent affectMetalText = new GUIContent("Affect Metal", "When enabled, this decal uses the metallic channel of its Mask Map. When disabled, the decal has no metallic effect.");
            public static GUIContent affectAmbientOcclusionText = new GUIContent("Affect Ambient Occlusion", "When enabled, this decal uses the smoothness channel of its Mask Map. When disabled, the decal has no smoothness effect.");
            public static GUIContent affectSmoothnessText = new GUIContent("Affect Smoothness", "When enabled, this decal uses the ambient occlusion channel of its Mask Map. When disabled, the decal has no ambient occlusion effect.");
            public static GUIContent affectEmissionText = new GUIContent("Affect Emission", "When enabled, this decal becomes emissive and appears self-illuminated. Affect Emission does not support Affects Transparent option on Decal Projector.");
            public static GUIContent supportLodCrossFadeText = new GUIContent("Support LOD CrossFade", "When enabled, this decal material supports LOD Cross fade if use on a Mesh.");
        }

        private const string kMaterial = "Material";
        private static FieldDescriptor AffectsAlbedo = new FieldDescriptor(kMaterial, "AffectsAlbedo", "");
        private static FieldDescriptor AffectsNormal = new FieldDescriptor(kMaterial, "AffectsNormal", "");
        private static FieldDescriptor AffectsNormalBlend = new FieldDescriptor(kMaterial, "AffectsNormalBlend", "");
        private static FieldDescriptor AffectsMAOS = new FieldDescriptor(kMaterial, "AffectsAO", "");
        private static FieldDescriptor AffectsEmission = new FieldDescriptor(kMaterial, "AffectsEmission", "");
        private static FieldDescriptor AffectsDBuffer = new FieldDescriptor(kMaterial, "AffectsDBuffer", "");
        private static FieldDescriptor DecalDefault = new FieldDescriptor(kMaterial, "DecalDefault", "");
        private static FieldDescriptor AngleFade = new FieldDescriptor(kMaterial, "AngleFade", "");

        [System.Serializable]
        class DecalData
        {
            public bool affectsAlbedo = true;
            public bool affectsNormalBlend = true;
            public bool affectsNormal = true;
            public bool affectsMAOS;
            public bool affectsEmission;
            public int drawOrder;
            public bool supportLodCrossFade;
            public bool angleFade;
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

                    CollectPassRenderState(ref passDescriptor);

                    passes.Add(passDescriptor, pass.fieldConditions);
                }
                subShader.passes = passes;

                context.AddSubShader(subShader);
            }
        }

        private void CollectPassRenderState(ref PassDescriptor pass)
        {
            if (pass.lightMode == DecalShaderPassNames.DecalGBufferProjector ||
                pass.lightMode == DecalShaderPassNames.DecalGBufferMesh)
            {
                // Make copy to avoid overwriting static
                pass.renderStates = new RenderStateCollection() { pass.renderStates };

                if (decalData.affectsAlbedo)
                    pass.renderStates.Add(RenderState.ColorMask("ColorMask RGB"));
                else
                    pass.renderStates.Add(RenderState.ColorMask("ColorMask 0"));

                pass.renderStates.Add(RenderState.ColorMask("ColorMask 0 1"));

                if (decalData.affectsNormal)
                    pass.renderStates.Add(RenderState.ColorMask("ColorMask RGB 2"));
                else
                    pass.renderStates.Add(RenderState.ColorMask("ColorMask 0 2"));

                // GI needs it unconditionaly
                pass.renderStates.Add(RenderState.ColorMask("ColorMask RGB 3"));
            }

            if (pass.lightMode == DecalShaderPassNames.DBufferProjector ||
                pass.lightMode == DecalShaderPassNames.DBufferMesh)
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

                if (decalData.affectsMAOS)
                    pass.renderStates.Add(DecalColorMasks.ColorMaskRGBA2);
                else
                    pass.renderStates.Add(DecalColorMasks.ColorMaskNone2);
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            Vector1ShaderProperty drawOrder = new Vector1ShaderProperty();
            drawOrder.overrideReferenceName = "_DrawOrder";
            drawOrder.displayName = "Draw Order";
            drawOrder.floatType = FloatType.Slider;
            drawOrder.rangeValues = new Vector2(-50, 50);
            drawOrder.hidden = true;
            drawOrder.value = 0;
            collector.AddShaderProperty(drawOrder);

            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                overrideReferenceName = "_DecalMeshBiasType",
                displayName = "DecalMesh BiasType",
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

            if (decalData.angleFade)
            {
                Vector1ShaderProperty decalAngleFadeSupported = new Vector1ShaderProperty();
                decalAngleFadeSupported.overrideReferenceName = "_DecalAngleFadeSupported";
                decalAngleFadeSupported.displayName = "Decal Angle Fade Supported";
                decalAngleFadeSupported.hidden = true;
                decalAngleFadeSupported.floatType = FloatType.Default;
                decalAngleFadeSupported.value = 1;
                collector.AddShaderProperty(decalAngleFadeSupported);
            }
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            // Decal properties
            context.AddField(AffectsAlbedo, decalData.affectsAlbedo);
            context.AddField(AffectsNormal, decalData.affectsNormal);
            context.AddField(AffectsNormalBlend, decalData.affectsNormalBlend);
            context.AddField(AffectsMAOS, decalData.affectsMAOS);
            context.AddField(AffectsEmission, decalData.affectsEmission);
            context.AddField(AffectsDBuffer, decalData.affectsAlbedo || decalData.affectsNormal || decalData.affectsMAOS);
            context.AddField(DecalDefault, decalData.affectsAlbedo || decalData.affectsNormal || decalData.affectsMAOS || decalData.affectsEmission);
            context.AddField(Fields.LodCrossFade, decalData.supportLodCrossFade);
            context.AddField(AngleFade, decalData.angleFade);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);

            // Decal
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor, decalData.affectsAlbedo);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha, decalData.affectsAlbedo || decalData.affectsEmission);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS, decalData.affectsNormal);
            context.AddBlock(UniversalBlockFields.SurfaceDescription.NormalAlpha, decalData.affectsNormal && decalData.affectsNormalBlend);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic, decalData.affectsMAOS);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion, decalData.affectsMAOS);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness, decalData.affectsMAOS);
            context.AddBlock(UniversalBlockFields.SurfaceDescription.MAOSAlpha, decalData.affectsMAOS);
            context.AddBlock(BlockFields.SurfaceDescription.Emission, decalData.affectsEmission);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddProperty("Affect BaseColor", new Toggle() { value = decalData.affectsAlbedo }, (evt) =>
            {
                if (Equals(decalData.affectsAlbedo, evt.newValue))
                    return;

                registerUndo("Change Affect BaseColor");
                decalData.affectsAlbedo = (bool)evt.newValue;
                onChange();
            });

            context.AddProperty("Affect Normal", new Toggle() { value = decalData.affectsNormal }, (evt) =>
            {
                if (Equals(decalData.affectsNormal, evt.newValue))
                    return;

                registerUndo("Change Affects Normal");
                decalData.affectsNormal = (bool)evt.newValue;
                onChange();
            });

            context.globalIndentLevel++;
            context.AddProperty("Blend", new Toggle() { value = decalData.affectsNormalBlend }, (evt) =>
            {
                if (Equals(decalData.affectsNormalBlend, evt.newValue))
                    return;

                registerUndo("Change Affects Normal Blend");
                decalData.affectsNormalBlend = (bool)evt.newValue;
                onChange();
            });
            context.globalIndentLevel--;

            context.AddProperty("Affect MAOS", new Toggle() { value = decalData.affectsMAOS }, (evt) =>
            {
                if (Equals(decalData.affectsMAOS, evt.newValue))
                    return;

                registerUndo("Change Affect MAOS");
                decalData.affectsMAOS = (bool)evt.newValue;
                onChange();
            });

            context.AddProperty("Affect Emission", new Toggle() { value = decalData.affectsEmission }, (evt) =>
            {
                if (Equals(decalData.affectsEmission, evt.newValue))
                    return;

                registerUndo("Change Affect Emission");
                decalData.affectsEmission = (bool)evt.newValue;
                onChange();
            });

            context.AddProperty("Supports LOD Cross Fade", new Toggle() { value = decalData.supportLodCrossFade }, (evt) =>
            {
                if (Equals(decalData.supportLodCrossFade, evt.newValue))
                    return;

                registerUndo("Change Supports LOD Cross Fade");
                decalData.supportLodCrossFade = (bool)evt.newValue;
                onChange();
            });

            context.AddProperty("Angle Fade", new Toggle() { value = decalData.angleFade }, (evt) =>
            {
                if (Equals(decalData.angleFade, evt.newValue))
                    return;

                registerUndo("Change Angle Fade");
                decalData.angleFade = (bool)evt.newValue;
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
                customTags = "\"PreviewType\"=\"Plane\"",
                generatesPreview = true,
                passes = new PassCollection
                {
                    { DecalPasses.DBufferProjector, new FieldCondition(AffectsDBuffer, true) },
                    { DecalPasses.ForwardEmissiveProjector, new FieldCondition(AffectsEmission, true) },
                    { DecalPasses.ScreenSpaceProjector, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.GBufferProjector, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.DBufferMesh, new FieldCondition(AffectsDBuffer, true) },
                    { DecalPasses.ForwardEmissiveMesh, new FieldCondition(AffectsEmission, true) },
                    { DecalPasses.ScreenSpaceMesh, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.GBufferMesh, new FieldCondition(DecalDefault, true) },
                    { DecalPasses.ScenePicking, new FieldCondition(DecalDefault, true) },
                },
            };
        }
        #endregion

        #region Passes
        static class DecalPasses
        {
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
                pragmas = DecalPragmas.MultipleRenderTargets,
                defines = DecalDefines.ScenePicking,
                includes = DecalIncludes.ScenePicking,

                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
            };

            public static PassDescriptor DBufferProjector = new PassDescriptor()
            {
                // Definition
                displayName = DecalShaderPassNames.DBufferProjector,
                referenceName = "SHADERPASS_DBUFFER_PROJECTOR",
                lightMode = DecalShaderPassNames.DBufferProjector,
                useInPreview = false,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentWithoutEmessive,

                //Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = DecalRenderStates.DBufferProjector,
                pragmas = DecalPragmas.MultipleRenderTargets,
                keywords = DecalKeywords.DBufferProjector,
                defines = DecalDefines.Projector,
                includes = DecalIncludes.DBuffer,
            };

            public static PassDescriptor ForwardEmissiveProjector = new PassDescriptor()
            {
                // Definition
                displayName = DecalShaderPassNames.DecalProjectorForwardEmissive,
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_PROJECTOR",
                lightMode = DecalShaderPassNames.DecalProjectorForwardEmissive,
                useInPreview = false,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port mask
                validPixelBlocks = DecalBlockMasks.ForwardOnlyEmissive,

                //Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.ForwardEmissiveProjector,
                pragmas = DecalPragmas.MultipleRenderTargets,
                defines = DecalDefines.ProjectorWithEmission,
                includes = DecalIncludes.DBuffer,
            };

            public static PassDescriptor ScreenSpaceProjector = new PassDescriptor()
            {
                // Definition
                displayName = DecalShaderPassNames.DecalScreenSpaceProjector,
                referenceName = "SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR",
                lightMode = DecalShaderPassNames.DecalScreenSpaceProjector,
                useInPreview = false,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port mask
                validPixelBlocks = DecalBlockMasks.Fragment,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.ScreenSpaceProjector,
                fieldDependencies = CoreFieldDependencies.Default,

                renderStates = DecalRenderStates.ScreenSpaceProjector,
                pragmas = DecalPragmas.ScreenSpace,
                defines = DecalDefines.ProjectorWithEmission,
                keywords = DecalKeywords.ScreenSpaceProjector,
                includes = DecalIncludes.ScreenSpace,
            };

            public static PassDescriptor GBufferProjector = new PassDescriptor()
            {
                // Definition
                displayName = DecalShaderPassNames.DecalGBufferProjector,
                referenceName = "SHADERPASS_DECAL_GBUFFER_PROJECTOR",
                lightMode = DecalShaderPassNames.DecalGBufferProjector,
                useInPreview = false,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port mask
                validPixelBlocks = DecalBlockMasks.Fragment,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.GBufferProjector,
                fieldDependencies = CoreFieldDependencies.Default,

                renderStates = DecalRenderStates.GBufferProjector,
                pragmas = DecalPragmas.GBuffer,
                defines = DecalDefines.ProjectorWithEmission,
                keywords = DecalKeywords.GBufferProjector,
                includes = DecalIncludes.GBuffer,
            };

            public static PassDescriptor DBufferMesh = new PassDescriptor()
            {
                // Definition
                displayName = DecalShaderPassNames.DBufferMesh,
                referenceName = "SHADERPASS_DBUFFER_MESH",
                lightMode = DecalShaderPassNames.DBufferMesh,
                useInPreview = false,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentWithoutEmessive,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.DBufferMesh,
                pragmas = DecalPragmas.MultipleRenderTargets,
                defines = DecalDefines.Mesh,
                keywords = DecalKeywords.DBufferMesh,
                includes = DecalIncludes.DBuffer,
            };

            public static PassDescriptor ForwardEmissiveMesh = new PassDescriptor()
            {
                // Definition
                displayName = DecalShaderPassNames.DecalMeshForwardEmissive,
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_MESH",
                lightMode = DecalShaderPassNames.DecalMeshForwardEmissive,
                useInPreview = false,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port mask
                validPixelBlocks = DecalBlockMasks.Fragment,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.ForwardEmissiveMesh,
                pragmas = DecalPragmas.MultipleRenderTargets,
                defines = DecalDefines.MeshWithEmission,
                includes = DecalIncludes.DBuffer,
            };

            public static PassDescriptor ScreenSpaceMesh = new PassDescriptor()
            {
                // Definition
                displayName = DecalShaderPassNames.DecalScreenSpaceMesh,
                referenceName = "SHADERPASS_DECAL_SCREEN_SPACE_MESH",
                lightMode = DecalShaderPassNames.DecalScreenSpaceMesh,
                useInPreview = true,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port mask
                validPixelBlocks = DecalBlockMasks.Fragment, // todo

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.ScreenSpaceMesh,
                fieldDependencies = CoreFieldDependencies.Default,

                renderStates = DecalRenderStates.ScreenSpaceMesh,
                pragmas = DecalPragmas.ScreenSpace,
                defines = DecalDefines.MeshWithEmission,
                keywords = DecalKeywords.ScreenSpaceMesh,
                includes = DecalIncludes.ScreenSpace,
            };

            public static PassDescriptor GBufferMesh = new PassDescriptor()
            {
                // Definition
                displayName = DecalShaderPassNames.DecalGBufferMesh,
                referenceName = "SHADERPASS_DECAL_GBUFFER_MESH",
                lightMode = DecalShaderPassNames.DecalGBufferMesh,
                useInPreview = false,

                // Template
                passTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/Decal/DecalPass.template",
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port mask
                validPixelBlocks = DecalBlockMasks.Fragment, // todo

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.ScreenSpaceMesh,
                fieldDependencies = CoreFieldDependencies.Default,

                renderStates = DecalRenderStates.GBufferMesh,
                pragmas = DecalPragmas.GBuffer,
                defines = DecalDefines.MeshWithEmission,
                keywords = DecalKeywords.GBufferMesh,
                includes = DecalIncludes.GBuffer,
            };
        }
        #endregion

        #region PortMasks
        static class DecalBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentWithoutEmessive = new BlockFieldDescriptor[]
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

            public static BlockFieldDescriptor[] ForwardOnlyEmissive = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.Emission,
            };

            public static BlockFieldDescriptor[] Fragment = new BlockFieldDescriptor[]
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
            public static FieldCollection Mesh = new FieldCollection()
            {
                StructFields.Attributes.normalOS,
                StructFields.Attributes.tangentOS,
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.texCoord0,
            };

            public static FieldCollection ScreenSpaceProjector = new FieldCollection()
            {
                StructFields.Varyings.normalWS,
                StructFields.Varyings.viewDirectionWS,
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                // todo
                //UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static FieldCollection GBufferProjector = new FieldCollection()
            {
                StructFields.Varyings.normalWS,
                StructFields.Varyings.viewDirectionWS,
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                // todo
                //UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static FieldCollection ScreenSpaceMesh = new FieldCollection()
            {
                StructFields.Attributes.normalOS,
                StructFields.Attributes.tangentOS,
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.viewDirectionWS,
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                // todo
                //UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
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
            public static RenderStateDescriptor ColorMaskRGBA2 = RenderState.ColorMask("ColorMask RGBA 2");
            public static RenderStateDescriptor ColorMaskNone2 = RenderState.ColorMask("ColorMask 0 2");
        }
        #endregion

        #region RenderStates
        static class DecalRenderStates
        {
            private readonly static string[] s_DBufferBlends = new string[]
            {
                "Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha",
                "Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha",
                "Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha",
            };
            private readonly static string[] s_GBufferBlends = new string[]
            {
                "Blend 0 SrcAlpha OneMinusSrcAlpha",
                "Blend 1 SrcAlpha OneMinusSrcAlpha",
                "Blend 2 SrcAlpha OneMinusSrcAlpha",
                "Blend 3 SrcAlpha OneMinusSrcAlpha",
            };

            public static RenderStateCollection ScenePicking = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Back) },
            };

            public static RenderStateCollection DBufferProjector = new RenderStateCollection
            {
                { RenderState.Blend(s_DBufferBlends[0]) },
                { RenderState.Blend(s_DBufferBlends[1]) },
                { RenderState.Blend(s_DBufferBlends[2]) },
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection ForwardEmissiveProjector = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha One") },
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection ScreenSpaceProjector = new RenderStateCollection
            {
                { RenderState.Blend("Blend SrcAlpha OneMinusSrcAlpha") },
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection GBufferProjector = new RenderStateCollection
            {
                { RenderState.Blend(s_GBufferBlends[0]) },
                { RenderState.Blend(s_GBufferBlends[1]) },
                { RenderState.Blend(s_GBufferBlends[2]) },
                { RenderState.Blend(s_GBufferBlends[3]) },
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection DBufferMesh = new RenderStateCollection
            {
                { RenderState.Blend(s_DBufferBlends[0]) },
                { RenderState.Blend(s_DBufferBlends[1]) },
                { RenderState.Blend(s_DBufferBlends[2]) },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection ForwardEmissiveMesh = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha One") },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection ScreenSpaceMesh = new RenderStateCollection
            {
                { RenderState.Blend("Blend SrcAlpha OneMinusSrcAlpha") },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection GBufferMesh = new RenderStateCollection
            {
                { RenderState.Blend(s_GBufferBlends[0]) },
                { RenderState.Blend(s_GBufferBlends[1]) },
                { RenderState.Blend(s_GBufferBlends[2]) },
                { RenderState.Blend(s_GBufferBlends[3]) },
                { RenderState.ZWrite(ZWrite.Off) },
            };
        }
        #endregion

        #region Pragmas
        static class DecalPragmas
        {
            public static PragmaCollection ScreenSpace = new PragmaCollection
            {
                { Pragma.Target(ShaderModel.Target25) }, // Derivatives
                { Pragma.Vertex("Vert") },
                { Pragma.Fragment("Frag") },
                { Pragma.MultiCompileInstancing },
                { Pragma.MultiCompileFog },
            };

            public static PragmaCollection GBuffer = new PragmaCollection
            {
                { Pragma.Target(ShaderModel.Target35) }, // MRT4
                { Pragma.Vertex("Vert") },
                { Pragma.Fragment("Frag") },
                { Pragma.MultiCompileInstancing },
                { Pragma.MultiCompileFog },
            };

            public static PragmaCollection MultipleRenderTargets = new PragmaCollection
            {
                { Pragma.Target(ShaderModel.Target35) }, // MRT4
                { Pragma.Vertex("Vert") },
                { Pragma.Fragment("Frag") },
                { Pragma.MultiCompileInstancing },
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
                };

                public static KeywordDescriptor AffectsNormal = new KeywordDescriptor()
                {
                    displayName = "Affects Normal",
                    referenceName = "_MATERIAL_AFFECTS_NORMAL",
                    type = KeywordType.Boolean,
                };

                public static KeywordDescriptor AffectsNormalBlend = new KeywordDescriptor()
                {
                    displayName = "Affects Normal Blend",
                    referenceName = "_MATERIAL_AFFECTS_NORMAL_BLEND",
                    type = KeywordType.Boolean,
                };

                public static KeywordDescriptor AffectsMAOS = new KeywordDescriptor()
                {
                    displayName = "Affects Metal",
                    referenceName = "_MATERIAL_AFFECTS_MAOS",
                    type = KeywordType.Boolean,
                };

                public static KeywordDescriptor AffectsEmission = new KeywordDescriptor()
                {
                    displayName = "Affects Emission",
                    referenceName = "_MATERIAL_AFFECTS_EMISSION",
                    type = KeywordType.Boolean,
                };

                public static KeywordDescriptor AngleFade = new KeywordDescriptor()
                {
                    displayName = "Angle Fade",
                    referenceName = "DECAL_ANGLE_FADE",
                    type = KeywordType.Boolean,
                };

                public static KeywordDescriptor ScenePickingPass = new KeywordDescriptor()
                {
                    displayName = "Scene Picking Pass",
                    referenceName = "SCENEPICKINGPASS",
                    type = KeywordType.Boolean,
                };
            }

            public static DefineCollection Projector = new DefineCollection
            {
                { Descriptors.AffectsAlbedo, 1, new FieldCondition(AffectsAlbedo, true) },
                { Descriptors.AffectsNormal, 1, new FieldCondition(AffectsNormal, true) },
                { Descriptors.AffectsNormalBlend, 1, new FieldCondition(AffectsNormalBlend, true) },
                { Descriptors.AffectsMAOS, 1, new FieldCondition(AffectsMAOS, true) },
                { Descriptors.AngleFade, 1, new FieldCondition(AngleFade, true) }
            };

            public static DefineCollection ProjectorEmission = new DefineCollection
            {
                { Descriptors.AngleFade, 1, new FieldCondition(AngleFade, true) },
                { Descriptors.AffectsEmission, 1, new FieldCondition(AffectsEmission, true) },
            };

            public static DefineCollection ProjectorWithEmission = new DefineCollection
            {
                { Projector },
                { Descriptors.AffectsEmission, 1, new FieldCondition(AffectsEmission, true) },
            };

            public static DefineCollection Mesh = new DefineCollection
            {
                { Descriptors.AffectsAlbedo, 1, new FieldCondition(AffectsAlbedo, true) },
                { Descriptors.AffectsNormal, 1, new FieldCondition(AffectsNormal, true) },
                { Descriptors.AffectsNormalBlend, 1, new FieldCondition(AffectsNormalBlend, true) },
                { Descriptors.AffectsMAOS, 1, new FieldCondition(AffectsMAOS, true) },
            };

            public static DefineCollection MeshEmission = new DefineCollection
            {
                { Descriptors.AffectsEmission, 1, new FieldCondition(AffectsEmission, true) },
            };

            public static DefineCollection MeshWithEmission = new DefineCollection
            {
                { Mesh },
                { Descriptors.AffectsEmission, 1, new FieldCondition(AffectsEmission, true) },
            };

            public static DefineCollection ScenePicking = new DefineCollection { { Descriptors.ScenePickingPass, 1 }, };
        }
        #endregion

        #region Keywords
        static class DecalKeywords
        {
            static class Descriptors
            {
                public static KeywordDescriptor DecalsNormalBlend = new KeywordDescriptor()
                {
                    displayName = "Decal Normal Blend",
                    referenceName = "_DECAL_NORMAL_BLEND",
                    type = KeywordType.Enum,
                    definition = KeywordDefinition.MultiCompile,
                    scope = KeywordScope.Global,
                    entries = new KeywordEntry[]
                    {
                        new KeywordEntry() { displayName = "LOW", referenceName = "LOW" },
                        new KeywordEntry() { displayName = "MEDIUM", referenceName = "MEDIUM" },
                        new KeywordEntry() { displayName = "HIGH", referenceName = "HIGH" },
                    }
                };

                public static readonly KeywordDescriptor LodCrossFade = new KeywordDescriptor()
                {
                    displayName = "LOD Cross Fade",
                    referenceName = "LOD_FADE_CROSSFADE",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.MultiCompile,
                    scope = KeywordScope.Global,
                };
            }

            public static KeywordCollection DBufferMesh = new KeywordCollection
            {
                { CoreKeywordDescriptors.DBuffer },
                { Descriptors.LodCrossFade, new FieldCondition(Fields.LodCrossFade, true) },
            };

            public static KeywordCollection DBufferProjector = new KeywordCollection
            {
                { CoreKeywordDescriptors.DBuffer },
            };

            public static readonly KeywordCollection ScreenSpaceMesh = new KeywordCollection
            {
                { CoreKeywordDescriptors.StaticLightmap },
                { CoreKeywordDescriptors.DynamicLightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.AdditionalLights },
                { CoreKeywordDescriptors.AdditionalLightShadows },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { CoreKeywordDescriptors.ClusteredRendering },
                { Descriptors.DecalsNormalBlend },
                { Descriptors.LodCrossFade, new FieldCondition(Fields.LodCrossFade, true) },
            };

            public static readonly KeywordCollection ScreenSpaceProjector = new KeywordCollection
            {
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.AdditionalLights },
                { CoreKeywordDescriptors.AdditionalLightShadows },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.ClusteredRendering },
                { Descriptors.DecalsNormalBlend },
            };

            public static readonly KeywordCollection GBufferMesh = new KeywordCollection
            {
                { CoreKeywordDescriptors.StaticLightmap },
                { CoreKeywordDescriptors.DynamicLightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.MixedLightingSubtractive },
                { Descriptors.DecalsNormalBlend },
                { CoreKeywordDescriptors.GBufferNormalsOct },
                { Descriptors.LodCrossFade, new FieldCondition(Fields.LodCrossFade, true) },
            };

            public static readonly KeywordCollection GBufferProjector = new KeywordCollection
            {
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.ShadowsSoft },
                { Descriptors.DecalsNormalBlend },
                { CoreKeywordDescriptors.GBufferNormalsOct },
            };
        }
        #endregion

        #region Includes
        static class DecalIncludes
        {
            const string kDecalInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl";
            const string kShaderVariablesDecal = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl";
            const string kPassDecal = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPassDecal.hlsl";
            const string kShaderPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl";
            const string kVaryings = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl";
            const string kDBuffer = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl";
            const string kGBuffer = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl";

            public static IncludeCollection DecalPregraph = new IncludeCollection
            {
                { kShaderPass, IncludeLocation.Pregraph },
                { kDecalInput, IncludeLocation.Pregraph },
                { kShaderVariablesDecal, IncludeLocation.Pregraph },
            };

            public static IncludeCollection DecalPostgraph = new IncludeCollection
            {
                { kVaryings, IncludeLocation.Postgraph },
                { kPassDecal, IncludeLocation.Postgraph },
            };

            public static IncludeCollection DBuffer = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { DecalPregraph },
                { kDBuffer, IncludeLocation.Pregraph },

                // Post-graph
                { DecalPostgraph },
            };

            public static IncludeCollection ScreenSpace = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { DecalPregraph },

                // Post-graph
                { DecalPostgraph },
            };

            public static IncludeCollection GBuffer = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kGBuffer, IncludeLocation.Pregraph },
                { DecalPregraph },

                // Post-graph
                { DecalPostgraph },
            };

            public static IncludeCollection ScenePicking = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { DecalPregraph },

                // Post-graph
                { DecalPostgraph },
            };
        }
        #endregion
    }
}
