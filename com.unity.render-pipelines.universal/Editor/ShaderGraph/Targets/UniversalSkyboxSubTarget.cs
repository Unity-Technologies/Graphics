using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;
using static UnityEditor.Rendering.Universal.ShaderGraph.SubShaderUtils;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalSkyboxSubTarget : UniversalSubTarget, ILegacyTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("07d253789a9086344a2a6570843e17db"); // UniversalSkyboxSubTarget.cs

        public UniversalSkyboxSubTarget()
        {
            displayName = "Skybox";
        }

        protected override ShaderID shaderID => ShaderID.SG_Skybox;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);

            var universalRPType = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset);
            if (!context.HasCustomEditorForRenderPipeline(universalRPType))
            {
                context.AddCustomEditorForRenderPipeline(typeof(ShaderGraphSkyboxGUI).FullName, universalRPType);
            }

            // Skybox need to be opaque and not cast or receive shadows
            target.surfaceType = SurfaceType.Opaque;
            target.castShadows = false;
            target.receiveShadows = false;

            // Process SubShaders
            context.AddSubShader(SubShaders.Skybox(target));
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.allowMaterialOverride)
            {
                material.SetFloat(Property.SurfaceType, (float)target.surfaceType);
                material.SetFloat(Property.BlendMode, (float)target.alphaMode);
                material.SetFloat(Property.AlphaClip, 0.0f);
                material.SetFloat(Property.CullMode, (int)target.renderFace);
                material.SetFloat(Property.CastShadows, 0.0f);
                material.SetFloat(Property.ZWriteControl, (float)target.zWriteControl);
                material.SetFloat(Property.ZTest, (float)ZTestMode.LEqual);
            }

            ShaderGraphSkyboxGUI.UpdateMaterial(material, MaterialUpdateType.CreatedNewMaterial);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            // There is no legacy IMasterNode1 for Skybox shaders, so we don't need to try an upgrade
            return false;
        }

        #region SubShader

        private static class SubShaders
        {
            public static SubShaderDescriptor Skybox(UniversalTarget target)
            {
                var result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kUnlitMaterialTypeTag,
                    renderType = "Background",
                    renderQueue = "Background",
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                result.passes.Add(SkyboxPasses.Forward(target));

                return result;
            }
        }
        #endregion

        #region Pass

        private static class SkyboxPasses
        {
            public static PassDescriptor Forward(UniversalTarget target)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Universal Forward",
                    referenceName = "SHADERPASS_SKYBOX",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = SkyboxBlockMasks.Vertex,
                    validPixelBlocks = SkyboxBlockMasks.FragmentColor,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = SkyboxRequiredFields.Skybox,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = SkyboxPragmas.Skybox,
                    defines = new DefineCollection()
                    {
                        { SkyboxKeywords.BuiltInStereoMatricesKeyword, 1 },
                    },
                    keywords = new KeywordCollection() { SkyboxKeywords.SkyboxBaseKeywords },
                    includes = SkyboxIncludes.Skybox,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);

                return result;
            }

            #region RequiredFields

            private static class SkyboxRequiredFields
            {
                public static readonly FieldCollection Skybox = new FieldCollection()
                {
                    StructFields.Varyings.positionWS
                };
            }
            #endregion
        }
        #endregion

        #region PortMasks

        private static class SkyboxBlockMasks
        {
            public static readonly BlockFieldDescriptor[] Vertex = new BlockFieldDescriptor[]
            {
                BlockFields.VertexDescription.Position,
            };

            public static readonly BlockFieldDescriptor[] FragmentColor = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
            };
        }
        #endregion

        #region Keywords

        private static class SkyboxKeywords
        {
            public static readonly KeywordCollection SkyboxBaseKeywords = new KeywordCollection()
            {
                CoreKeywordDescriptors.DebugDisplay,
            };

            // XRTODO : Port the skybox renderpass to URP (instead of using the built-in skybox renderpass)
            public static readonly KeywordDescriptor BuiltInStereoMatricesKeyword = new KeywordDescriptor()
            {
                displayName = "Use built-in stereo matrices",
                referenceName = "USING_BUILTIN_STEREO_MATRICES",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Global,
                stages = KeywordShaderStage.All,
            };
        }
        #endregion

        #region Pragmas

        private static class SkyboxPragmas
        {
            public static readonly PragmaCollection Skybox = new PragmaCollection
            {
                {Pragma.MultiCompileInstancing},
                {Pragma.Vertex("vert")},
                {Pragma.Fragment("frag")},
            };
        }
        #endregion

        #region Includes
        private static class SkyboxIncludes
        {
            private const string kSkyboxPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SkyboxPass.hlsl";

            public static readonly IncludeCollection Skybox = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSkyboxPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
