using System;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using static UnityEditor.Rendering.Universal.ShaderGraph.SubShaderUtils;
using UnityEngine.Rendering.Universal;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalSixWaySubTarget : UniversalSubTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("d6c78107b64145745805d963de80cc17"); // UniversalLitSubTarget.cs

        public override int latestVersion => 2;

        public UniversalSixWaySubTarget()
        {
            displayName = "Six-way Smoke Lit";
        }

        protected override ShaderID shaderID => ShaderID.SG_SixWaySmokeLit;

        public override bool IsActive() => true;

        [SerializeField]
        bool m_UseColorAbsorption = true;

        bool useColorAbsorption
        {
            get => m_UseColorAbsorption;
            set => m_UseColorAbsorption = value;
        }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);

            var universalRPType = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset);
            if (!context.HasCustomEditorForRenderPipeline(universalRPType))
            {
                var gui = typeof(SixWayGUI);
#if HAS_VFX_GRAPH
                if (TargetsVFX())
                    gui = typeof(VFXSixWayGUI);
#endif
                context.AddCustomEditorForRenderPipeline(gui.FullName, universalRPType);
            }

            // Process SubShaders
            context.AddSubShader(PostProcessSubShader(SubShaders.SixWaySubShader(target,  target.renderType, target.renderQueue, target.disableBatching, useColorAbsorption)));
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.allowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set

                material.SetFloat(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.ReceiveShadows, target.receiveShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.SurfaceType, (float)target.surfaceType);
                material.SetFloat(Property.BlendMode, (float)target.alphaMode);
                material.SetFloat(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                material.SetFloat(Property.CullMode, (int)target.renderFace);
                material.SetFloat(Property.ZWriteControl, (float)target.zWriteControl);
                material.SetFloat(Property.ZTest, (float)target.zTestMode);
                material.SetFloat(SixWayProperties.UseColorAbsorption, useColorAbsorption ? 1.0f : 0.0f );
            }

            // We always need these properties regardless of whether the material is allowed to override
            // Queue control & offset enable correct automatic render queue behavior
            // Control == 0 is automatic, 1 is user-specified render queue
            material.SetFloat(Property.QueueOffset, 0.0f);
            material.SetFloat(Property.QueueControl, (float)BaseShaderGUI.QueueControl.Auto);

            // call the full unlit material setup function
            ShaderGraphLitGUI.UpdateMaterial(material, MaterialUpdateType.CreatedNewMaterial);
            bool useColorAbsorptionValue = false;
            if (material.HasProperty(SixWayProperties.UseColorAbsorption))
                useColorAbsorptionValue = material.GetFloat(SixWayProperties.UseColorAbsorption) > 0.0f;
            CoreUtils.SetKeyword(material, "_SIX_WAY_COLOR_ABSORPTION", useColorAbsorptionValue);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            bool containsNormalDescriptor = false;
            foreach (var block in context.blocks)
            {
                if (Equals(block.descriptor, BlockFields.SurfaceDescription.NormalOS) ||
                    Equals(block.descriptor, BlockFields.SurfaceDescription.NormalTS) ||
                    Equals(block.descriptor, BlockFields.SurfaceDescription.NormalWS))
                {
                    containsNormalDescriptor = true;
                    break;
                }
            }
            context.AddField(UniversalFields.Normal, containsNormalDescriptor);

        }

        public struct Varyings
        {
            public static string name = "Varyings";

            public static FieldDescriptor diffuseGIData0 = new FieldDescriptor(Varyings.name, "diffuseGIData0",
                "VARYINGS_NEED_SIX_WAY_DIFFUSE_GI_DATA", ShaderValueType.Float4 ,subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor diffuseGIData1 = new FieldDescriptor(Varyings.name, "diffuseGIData1",
                "VARYINGS_NEED_SIX_WAY_DIFFUSE_GI_DATA", ShaderValueType.Float4 ,subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor diffuseGIData2 = new FieldDescriptor(Varyings.name, "diffuseGIData2",
                "VARYINGS_NEED_SIX_WAY_DIFFUSE_GI_DATA", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Optional);
        }


        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
            context.AddBlock(BlockFields.SurfaceDescription.MapRightTopBack);
            context.AddBlock(BlockFields.SurfaceDescription.MapLeftBottomFront);
            context.AddBlock(BlockFields.SurfaceDescription.AbsorptionStrength, useColorAbsorption || target.allowMaterialOverride);

            // when the surface options are material controlled, we must show all of these blocks
            // when target controlled, we can cull the unnecessary blocks
            context.AddBlock(BlockFields.SurfaceDescription.Alpha, (target.surfaceType == SurfaceType.Transparent || target.alphaClip) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, (target.alphaClip) || target.allowMaterialOverride);

        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // if using material control, add the material property to control workflow mode
            if (target.allowMaterialOverride)
            {
                collector.AddFloatProperty(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                collector.AddToggleProperty(Property.ReceiveShadows, target.receiveShadows);

                // setup properties using the defaults
                collector.AddFloatProperty(Property.SurfaceType, (float)target.surfaceType);
                collector.AddFloatProperty(Property.BlendMode, (float)target.alphaMode);
                collector.AddFloatProperty(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.SrcBlend, 1.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddFloatProperty(Property.DstBlend, 0.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddFloatProperty(Property.SrcBlendAlpha, 1.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddFloatProperty(Property.DstBlendAlpha, 0.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddToggleProperty(Property.ZWrite, (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.ZWriteControl, (float)target.zWriteControl);
                collector.AddFloatProperty(Property.ZTest, (float)target.zTestMode);    // ztest mode is designed to directly pass as ztest
                collector.AddFloatProperty(Property.CullMode, (float)target.renderFace);    // render face enum is designed to directly pass as a cull mode

                bool enableAlphaToMask = (target.alphaClip && (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.AlphaToMask, enableAlphaToMask ? 1.0f : 0.0f);

                collector.AddToggleProperty(SixWayProperties.UseColorAbsorption, useColorAbsorption);
            }

            // We always need these properties regardless of whether the material is allowed to override other shader properties.
            // Queue control & offset enable correct automatic render queue behavior.  Control == 0 is automatic, 1 is user-specified.
            // We initialize queue control to -1 to indicate to UpdateMaterial that it needs to initialize it properly on the material.
            collector.AddFloatProperty(Property.QueueOffset, 0.0f);
            collector.AddFloatProperty(Property.QueueControl, -1.0f);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var universalTarget = (target as UniversalTarget);
            universalTarget.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);

            universalTarget.AddDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo, showReceiveShadows: true);

            context.AddProperty("Use Color Absorption", new Toggle() { value = useColorAbsorption }, (evt) =>
            {
                if (Equals(useColorAbsorption, evt.newValue))
                    return;

                registerUndo("Change Use Color Absorption");
                useColorAbsorption = evt.newValue;
                onChange();
            });

        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            int hash = base.ComputeMaterialNeedsUpdateHash();
            hash = hash * 23 + target.allowMaterialOverride.GetHashCode();
            return hash;
        }

        #region SubShader
        static class SubShaders
        {
            const string kSixWayMaterialTypeTag = "\"UniversalMaterialType\" = \"SixWayLit\"";

            public static SubShaderDescriptor SixWaySubShader(UniversalTarget target, string renderType, string renderQueue, string disableBatchingTag, bool useColorAbsorption)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = kSixWayMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    disableBatchingTag = disableBatchingTag,
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                result.passes.Add(SixWayPasses.ForwardOnly(target,  CoreBlockMasks.Vertex, SixWayBlockMasks.FragmentLit, CorePragmas.Forward, SixWayKeywords.Forward, useColorAbsorption));

                // cull the shadowcaster pass if we know it will never be used
                if (target.castShadows || target.allowMaterialOverride)
                    result.passes.Add(PassVariant(CorePasses.ShadowCaster(target), CorePragmas.Instanced));

                if (target.mayWriteDepth)
                    result.passes.Add(PassVariant(CorePasses.DepthOnly(target), CorePragmas.Instanced));

                result.passes.Add(PassVariant(SixWayPasses.Meta(target), CorePragmas.Default));

                // Currently neither of these passes (selection/picking) can be last for the game view for
                // UI shaders to render correctly. Verify [1352225] before changing this order.
                result.passes.Add(PassVariant(CorePasses.SceneSelection(target), CorePragmas.Default));
                result.passes.Add(PassVariant(CorePasses.ScenePicking(target), CorePragmas.Default));

                return result;
            }
        }
        #endregion

        #region Passes
        static class SixWayPasses
        {
            static void AddReceiveShadowsControlToPass(ref PassDescriptor pass, UniversalTarget target, bool receiveShadows)
            {
                if (target.allowMaterialOverride)
                    pass.keywords.Add(SixWayKeywords.ReceiveShadowsOff);
                else if (!receiveShadows)
                    pass.defines.Add(SixWayKeywords.ReceiveShadowsOff, 1);
            }

            static void AddColorAbsorptionControlToPass(ref PassDescriptor pass, UniversalTarget target,
                bool useColorAbsorption)
            {
                if (target.allowMaterialOverride)
                    pass.keywords.Add(SixWayKeywords.UseColorAbsorption);
                else if (useColorAbsorption)
                    pass.defines.Add(SixWayKeywords.UseColorAbsorption, 1);
            }

            static StructDescriptor ForwardVaryings
            {
                get
                {
                    var baseVaryingsDescriptor = UniversalStructs.Varyings;

                    FieldDescriptor[] varyingsDescriptorFields =
                        new FieldDescriptor[baseVaryingsDescriptor.fields.Length + 3];

                    varyingsDescriptorFields[0] = Varyings.diffuseGIData0;
                    varyingsDescriptorFields[1] = Varyings.diffuseGIData1;
                    varyingsDescriptorFields[2] = Varyings.diffuseGIData2;
                    baseVaryingsDescriptor.fields.CopyTo(varyingsDescriptorFields,3);
                    baseVaryingsDescriptor.fields = varyingsDescriptorFields;

                    return baseVaryingsDescriptor;
                }
            }

            static readonly StructCollection ForwardStructCollection = new StructCollection
            {
                { Structs.Attributes },
                { ForwardVaryings },
                { Structs.SurfaceDescriptionInputs },
                { Structs.VertexDescriptionInputs },
            };

            public static PassDescriptor ForwardOnly(
                UniversalTarget target,
                BlockFieldDescriptor[] vertexBlocks,
                BlockFieldDescriptor[] pixelBlocks,
                PragmaCollection pragmas,
                KeywordCollection keywords,
                bool useColorAbsorption)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Universal Forward Only",
                    referenceName = "SHADERPASS_FORWARDONLY",
                    lightMode = "UniversalForwardOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = vertexBlocks,
                    validPixelBlocks = pixelBlocks,

                    // Fields
                    structs = ForwardStructCollection,
                    requiredFields = SixWayRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = pragmas,
                    defines = new DefineCollection { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection { keywords },
                    includes = new IncludeCollection { SixWayIncludes.Forward },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                CorePasses.AddAlphaToMaskControlToPass(ref result, target);
                AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);
                AddColorAbsorptionControlToPass(ref result, target, useColorAbsorption);

                return result;
            }



            public static PassDescriptor Meta(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Meta",
                    referenceName = "SHADERPASS_META",
                    lightMode = "Meta",

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = SixWayBlockMasks.FragmentMeta,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = SixWayRequiredFields.Meta,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Meta,
                    pragmas = CorePragmas.Default,
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection() { CoreKeywordDescriptors.EditorVisualization },
                    includes = SixWayIncludes.Meta,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }
        }
        #endregion

        #region PortMasks
        static class SixWayBlockMasks
        {
            public static readonly BlockFieldDescriptor[] FragmentLit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.MapRightTopBack,
                BlockFields.SurfaceDescription.MapLeftBottomFront,
                BlockFields.SurfaceDescription.AbsorptionStrength,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };


            public static readonly BlockFieldDescriptor[] FragmentMeta = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };
        }
        #endregion

        #region RequiredFields
        static class SixWayRequiredFields
        {
            public static readonly FieldCollection Forward = new FieldCollection()
            {
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.probeOcclusion,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
                Varyings.diffuseGIData0,
                Varyings.diffuseGIData1,
                Varyings.diffuseGIData2,
            };

            public static readonly FieldCollection Meta = new FieldCollection()
            {
                StructFields.Attributes.positionOS,
                StructFields.Attributes.normalOS,
                StructFields.Attributes.uv0,                            //
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Attributes.uv2,                            // needed for meta UVs
                StructFields.Attributes.instanceID,                     // needed for rendering instanced terrain
                StructFields.Varyings.positionCS,
                StructFields.Varyings.texCoord0,                        // needed for meta UVs
                StructFields.Varyings.texCoord1,                        // VizUV
                StructFields.Varyings.texCoord2,                        // LightCoord
            };
        }
        #endregion

        #region Defines

        public static class SixWayProperties
        {
            public static readonly string UseColorAbsorption = "_UseColorAbsorption";
        }

        #endregion

        #region Keywords
        static class SixWayKeywords
        {
            public static readonly KeywordDescriptor ReceiveShadowsOff = new KeywordDescriptor()
            {
                displayName = "Receive Shadows Off",
                referenceName = ShaderKeywordStrings._RECEIVE_SHADOWS_OFF,
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordDescriptor UseColorAbsorption = new KeywordDescriptor()
            {
                displayName = "Use Color Absorption",
                referenceName = "_SIX_WAY_COLOR_ABSORPTION",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment
            };

            public static readonly KeywordCollection Forward = new KeywordCollection
            {
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.AdditionalLights },
                { CoreKeywordDescriptors.AdditionalLightShadows },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { CoreKeywordDescriptors.LightLayers },
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywordDescriptors.LightCookies },
                { CoreKeywordDescriptors.ForwardPlus },
            };

        }
        #endregion

        #region Includes
        static class SixWayIncludes
        {
            const string kShadows = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl";
            const string kMetaInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl";
            const string kForwardPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SixWayForwardPass.hlsl";
            const string kLightingMetaPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl";
            const string kSixWayLighting = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SixWayLighting.hlsl";

            public static readonly IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.DOTSPregraph },
                { CoreIncludes.WriteRenderLayersPregraph },
                { CoreIncludes.ProbeVolumePregraph },
                { CoreIncludes.CorePregraph },
                { kShadows, IncludeLocation.Pregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },


                // Post-graph
                { kSixWayLighting, IncludeLocation.Postgraph},
                { CoreIncludes.CorePostgraph },
                { kForwardPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection Meta = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kMetaInput, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kLightingMetaPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
