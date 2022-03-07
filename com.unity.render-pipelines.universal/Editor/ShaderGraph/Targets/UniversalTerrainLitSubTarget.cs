using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;
using UnityEngine.Assertions;
using static UnityEditor.Rendering.Universal.ShaderGraph.SubShaderUtils;
using UnityEngine.Rendering.Universal;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalTerrainLitSubTarget : UniversalSubTarget, ILegacyTarget
    {
        private static readonly GUID kSourceCodeGuid = new GUID("ea07e558bc2741a5ba7f1d5470eb242b"); // UniversalTerrainLitSubTarget.cs

        public override int latestVersion => 1;

        [SerializeField]
        private bool m_EnableHeightBlend;

        [SerializeField]
        float m_HeightTransition;

        [SerializeField]
        bool m_EnableInstancedPerPixelNormal;

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace = NormalDropOffSpace.Tangent;

        [SerializeField]
        bool m_BlendModePreserveSpecular = true;

        protected override ShaderID shaderID => ShaderID.SG_TerrainLit;

        public bool enableHeightBlend
        {
            get => m_EnableHeightBlend;
            set => m_EnableHeightBlend = value;
        }

        public float heightTransition
        {
            get => m_HeightTransition;
            set => m_HeightTransition = value;
        }

        public bool enableInstancedPerPixelNormal
        {
            get => m_EnableInstancedPerPixelNormal;
            set => m_EnableInstancedPerPixelNormal = value;
        }

        public NormalDropOffSpace normalDropOffSpace
        {
            get => m_NormalDropOffSpace;
            set => m_NormalDropOffSpace = value;
        }

        public bool blendModePreserveSpecular
        {
            get => m_BlendModePreserveSpecular;
            set => m_BlendModePreserveSpecular = value;
        }

        public UniversalTerrainLitSubTarget()
        {
            displayName = "TerrainLit";
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);

            var universalRPType = typeof(UniversalRenderPipelineAsset);
            if (!context.HasCustomEditorForRenderPipeline(universalRPType))
                context.AddCustomEditorForRenderPipeline(typeof(ShaderGraphTerrainLitGUI).FullName, universalRPType);

            context.AddSubShader(PostProcessSubShader(SubShaders.LitComputeDotsSubShader(target, target.renderType, target.renderQueue, blendModePreserveSpecular)));
            context.AddSubShader(PostProcessSubShader(SubShaders.LitGLESSubShader(target, target.renderType, target.renderQueue, blendModePreserveSpecular)));
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.allowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                material.SetFloat(Property.SpecularWorkflowMode, 1.0f);
                material.SetFloat(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.ReceiveShadows, target.receiveShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.SurfaceType, 0.0f);
                material.SetFloat(Property.BlendMode, (float)target.alphaMode);
                material.SetFloat(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                material.SetFloat(Property.CullMode, 2.0f);
                material.SetFloat(Property.ZWriteControl, (float)target.zWriteControl);
                material.SetFloat(Property.ZTest, (float)target.zTestMode);
            }

            // We always need these properties regardless of whether the material is allowed to override
            // Queue control & offset enable correct automatic render queue behavior
            // Control == 0 is automatic, 1 is user-specified render queue
            material.SetFloat(Property.QueueOffset, 0.0f);
            material.SetFloat(Property.QueueControl, (float)BaseShaderGUI.QueueControl.Auto);

            // call the full unlit material setup function
            ShaderGraphLitGUI.UpdateMaterial(material, MaterialUpdateType.CreatedNewMaterial);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            var descs = context.blocks.Select(x => x.descriptor);

            // TerrainLit -- always controlled by subtarget
            context.AddField(UniversalFields.NormalDropOffOS, normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(UniversalFields.NormalDropOffTS, normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(UniversalFields.NormalDropOffWS, normalDropOffSpace == NormalDropOffSpace.World);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.NormalOS,           normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS,           normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS,           normalDropOffSpace == NormalDropOffSpace.World);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);

            // when the surface options are material controlled, we must show all of these blocks
            // when target controlled, we can cull the unnecessary blocks
            context.AddBlock(BlockFields.SurfaceDescription.Specular,           target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha,              (target.alphaClip) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, (target.alphaClip) || target.allowMaterialOverride);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = enableHeightBlend,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_EnableHeightBlend",
                displayName = "Enable Height Blend",
            });

            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Slider,
                value = heightTransition,
                hidden = false,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_HeightTransition",
                displayName = "Height Transition",
            });

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = enableInstancedPerPixelNormal,
                hidden = false,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_EnableInstancedPerPixelNormal",
                displayName = "Enable Instanced per Pixel Normal",
            });

            collector.AddShaderProperty(new Texture2DShaderProperty
            {
                defaultType = Texture2DShaderProperty.DefaultType.White,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = "_TerrainHolesTexture",
                displayName = "Holes Map (RGB)",
                useTilingAndOffset = true,
            });

            // if using material control, add the material property to control workflow mode
            if (target.allowMaterialOverride)
            {
                // force to set metallic workflow
                collector.AddFloatProperty(Property.SpecularWorkflowMode, 1.0f);
                collector.AddFloatProperty(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.ReceiveShadows, target.receiveShadows ? 1.0f : 0.0f);

                // setup properties using the defaults
                collector.AddFloatProperty(Property.SurfaceType, 0.0f);
                collector.AddFloatProperty(Property.BlendMode, (float)target.alphaMode);
                collector.AddFloatProperty(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.SrcBlend, 1.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddFloatProperty(Property.DstBlend, 0.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddToggleProperty(Property.ZWrite, (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.ZWriteControl, (float)target.zWriteControl);
                collector.AddFloatProperty(Property.ZTest, (float)target.zTestMode);    // ztest mode is designed to directly pass as ztest
                collector.AddFloatProperty(Property.CullMode, 2.0f);    // render face enum is designed to directly pass as a cull mode
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

            context.AddProperty("Blending Mode", new EnumField(AlphaMode.Alpha) { value = target.alphaMode }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.alphaMode, evt.newValue))
                    return;

                registerUndo("Change Blend");
                target.alphaMode = (AlphaMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Write", new EnumField(ZWriteControl.Auto) { value = target.zWriteControl }, (evt) =>
            {
                if (Equals(target.zWriteControl, evt.newValue))
                    return;

                registerUndo("Change Depth Write Control");
                target.zWriteControl = (ZWriteControl)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Test", new EnumField(ZTestModeForUI.LEqual) { value = (ZTestModeForUI)target.zTestMode }, (evt) =>
            {
                if (Equals(target.zTestMode, evt.newValue))
                    return;

                registerUndo("Change Depth Test");
                target.zTestMode = (ZTestMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Alpha Clipping", new Toggle() { value = target.alphaClip }, (evt) =>
            {
                if (Equals(target.alphaClip, evt.newValue))
                    return;

                registerUndo("Change Alpha Clip");
                target.alphaClip = evt.newValue;
                onChange();
            });

            context.AddProperty("Cast Shadows", new Toggle() { value = target.castShadows }, (evt) =>
            {
                if (Equals(target.castShadows, evt.newValue))
                    return;

                registerUndo("Change Cast Shadows");
                target.castShadows = evt.newValue;
                onChange();
            });

            context.AddProperty("Receive Shadows", new Toggle() {value = target.receiveShadows}, (evt) =>
            {
                if (Equals(target.receiveShadows, evt.newValue))
                    return;

                registerUndo("Change Receive Shadows");
                target.receiveShadows = evt.newValue;
                onChange();
            });

            context.AddProperty("Fragment Normal Space", new EnumField(NormalDropOffSpace.Tangent) { value = normalDropOffSpace }, (evt) =>
            {
                if (Equals(normalDropOffSpace, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
                onChange();
            });
        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            int hash = base.ComputeMaterialNeedsUpdateHash();
            hash = hash * 23 + target.allowMaterialOverride.GetHashCode();
            return hash;
        }

        // this is a copy of ZTestMode, but hides the "Disabled" option, which is invalid
        enum ZTestModeForUI
        {
            Never = 1,
            Less = 2,
            Equal = 3,
            LEqual = 4,     // default for most rendering
            Greater = 5,
            NotEqual = 6,
            GEqual = 7,
            Always = 8,
        };

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if (!(masterNode is PBRMasterNode1 pbrMasterNode))
                return false;

            m_NormalDropOffSpace = (NormalDropOffSpace)pbrMasterNode.m_NormalDropOffSpace;

            // Handle mapping of Normal block specifically
            BlockFieldDescriptor normalBlock;
            switch (m_NormalDropOffSpace)
            {
                case NormalDropOffSpace.Object:
                    normalBlock = BlockFields.SurfaceDescription.NormalOS;
                    break;
                case NormalDropOffSpace.World:
                    normalBlock = BlockFields.SurfaceDescription.NormalWS;
                    break;
                default:
                    normalBlock = BlockFields.SurfaceDescription.NormalTS;
                    break;
            }

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 8 },
                { BlockFields.VertexDescription.Normal, 9 },
                { BlockFields.SurfaceDescription.BaseColor, 0 },
                { normalBlock, 1 },
                { BlockFields.SurfaceDescription.Emission, 3 },
                { BlockFields.SurfaceDescription.Smoothness, 4 },
                { BlockFields.SurfaceDescription.Occlusion, 5 },
                { BlockFields.SurfaceDescription.Alpha, 6 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 7 },
            };

            blockMap.Add(BlockFields.SurfaceDescription.Metallic, 2);

            return true;
        }

        internal override void OnAfterParentTargetDeserialized()
        {
            Assert.IsNotNull(target);

            if (this.sgVersion < latestVersion)
            {
                // Upgrade old incorrect Premultiplied blend into
                // equivalent Alpha + Preserve Specular blend mode.
                if (this.sgVersion < 1)
                {
                    if (target.alphaMode == AlphaMode.Premultiply)
                    {
                        target.alphaMode = AlphaMode.Alpha;
                        blendModePreserveSpecular = true;
                    }
                    else
                        blendModePreserveSpecular = false;
                }
                ChangeVersion(latestVersion);
            }
        }

        static class UniversalTerrainStructs
        {
            public static StructDescriptor Varyings = new StructDescriptor()
            {
                name = "Varyings",
                packFields = true,
                populateWithCustomInterpolators = true,
                fields = new FieldDescriptor[]
                {
                    StructFields.Varyings.positionCS,
                    StructFields.Varyings.positionWS,
                    StructFields.Varyings.normalWS,
                    StructFields.Varyings.texCoord0,
                    StructFields.Varyings.texCoord1,
                    StructFields.Varyings.texCoord2,
                    StructFields.Varyings.texCoord3,
                    StructFields.Varyings.color,
                    StructFields.Varyings.screenPosition,
                    TerrainStructFields.Varyings.uvSplat01,
                    TerrainStructFields.Varyings.uvSplat23,
                    UniversalStructFields.Varyings.staticLightmapUV,
                    UniversalStructFields.Varyings.dynamicLightmapUV,
                    UniversalStructFields.Varyings.sh,
                    UniversalStructFields.Varyings.fogFactorAndVertexLight,
                    UniversalStructFields.Varyings.shadowCoord,
                    StructFields.Varyings.instanceID,
                    UniversalStructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,
                    UniversalStructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,
                    StructFields.Varyings.cullFace,
                }
            };
        }

        #region Template
        static class TerrainLitTemplate
        {
            public static readonly string kPassTemplate = "Packages/com.unity.render-pipelines.universal/Editor/Terrain/TerrainPass.template";
            public static readonly string[] kSharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories.Union(new []
            {
                "Packages/com.unity.render-pipelines.universal/Editor/Terrain/",
            }).ToArray();
        }
        #endregion

        #region StructCollections
        static class TerrainStructFields
        {
            public struct Varyings
            {
                public static FieldDescriptor uvSplat01 = new FieldDescriptor(
                    StructFields.Varyings.name, "uvSplat01", "", ShaderValueType.Float4, "TEXCOORD1", preprocessor: "defined(UNIVERSAL_TERRAIN_SPLAT01)", subscriptOptions: StructFieldOptions.Optional);
                public static FieldDescriptor uvSplat23 = new FieldDescriptor(
                    StructFields.Varyings.name, "uvSplat23", "", ShaderValueType.Float4, "TEXCOORD0", preprocessor: "defined(UNIVERSAL_TERRAIN_SPLAT23)", subscriptOptions: StructFieldOptions.Optional);
            }

            public struct SurfaceDescriptionInputs
            {
                public static FieldDescriptor uvSplat01 = new FieldDescriptor(
                    StructFields.SurfaceDescriptionInputs.name, "uvSplat01", "", ShaderValueType.Float4, "TEXCOORD1", "defined(UNIVERSAL_TERRAIN_SPLAT01)");
                public static FieldDescriptor uvSplat23 = new FieldDescriptor(
                    StructFields.SurfaceDescriptionInputs.name, "uvSplat23", "", ShaderValueType.Float4, "TEXCOORD2", "defined(UNIVERSAL_TERRAIN_SPLAT23)");
            }
        }

        internal static class TerrainStructs
        {
            public static StructDescriptor Attributes = new StructDescriptor()
            {
                name = "Attributes",
                packFields = false,
                fields = new FieldDescriptor[]
                {
                    StructFields.Attributes.positionOS,
                    StructFields.Attributes.normalOS,
                    StructFields.Attributes.uv0,
                    StructFields.Attributes.color,
                    StructFields.Attributes.instanceID,
                    StructFields.Attributes.vertexID,
                }
            };

            public static StructDescriptor SurfaceDescriptionInputs = new StructDescriptor()
            {
                name = "SurfaceDescriptionInputs",
                packFields = false,
                populateWithCustomInterpolators = true,
                fields = new FieldDescriptor[]
                {
                    StructFields.SurfaceDescriptionInputs.ObjectSpaceNormal,
                    StructFields.SurfaceDescriptionInputs.ViewSpaceNormal,
                    StructFields.SurfaceDescriptionInputs.WorldSpaceNormal,
                    StructFields.SurfaceDescriptionInputs.TangentSpaceNormal,

                    StructFields.SurfaceDescriptionInputs.uv0,
                    StructFields.SurfaceDescriptionInputs.uv1,
                    StructFields.SurfaceDescriptionInputs.uv2,
                    StructFields.SurfaceDescriptionInputs.uv3,

                    TerrainStructFields.SurfaceDescriptionInputs.uvSplat01,
                    TerrainStructFields.SurfaceDescriptionInputs.uvSplat23,
                }
            };
        }

        static class TerrainStructCollections
        {
            public static readonly StructCollection Default = new StructCollection
            {
                { TerrainStructs.Attributes },
                { UniversalTerrainStructs.Varyings },
                { TerrainStructs.SurfaceDescriptionInputs },
                { Structs.VertexDescriptionInputs },
            };
        }
        #endregion

        #region SubShader
        static class SubShaders
        {
            // SM 4.5, compute with dots instancing
            public static SubShaderDescriptor LitComputeDotsSubShader(UniversalTarget target, string renderType, string renderQueue, bool blendModePreserveSpecular)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = new List<string>() { UniversalTarget.kLitMaterialTypeTag, UniversalTarget.kTerrainMaterialTypeTag },
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection(),
                    shaderDependencies = new List<ShaderDependency>(),
                };

                result.passes.Add(TerrainLitPasses.Forward(target, blendModePreserveSpecular, TerrainCorePragmas.DOTSForward));
                result.passes.Add(TerrainLitPasses.GBuffer(target, blendModePreserveSpecular));

                // cull the shadowcaster pass if we know it will never be used
                if (target.castShadows || target.allowMaterialOverride)
                    result.passes.Add(PassVariant(TerrainLitPasses.ShadowCaster(target), TerrainCorePragmas.DOTSInstanced));

                if (target.mayWriteDepth)
                    result.passes.Add(PassVariant(TerrainLitPasses.DepthOnly(target), TerrainCorePragmas.DOTSInstanced));

                result.passes.Add(PassVariant(TerrainLitPasses.DepthNormal(target), TerrainCorePragmas.DOTSInstanced));
                result.passes.Add(PassVariant(TerrainLitPasses.Meta(target), TerrainCorePragmas.DOTSInstanced));
                // Currently neither of these passes (selection/picking) can be last for the game view for
                // UI shaders to render correctly. Verify [1352225] before changing this order.
                result.passes.Add(PassVariant(TerrainLitPasses.SceneSelection(target), TerrainCorePragmas.DOTSDefault));
                result.passes.Add(PassVariant(TerrainLitPasses.ScenePicking(target), TerrainCorePragmas.DOTSDefault));

                result.shaderDependencies.Add(TerrainDependencies.AddPassShader());
                result.shaderDependencies.Add(TerrainDependencies.BaseMapShader());
                result.shaderDependencies.Add(TerrainDependencies.BaseMapGenShader());

                return result;
            }

            public static SubShaderDescriptor LitGLESSubShader(UniversalTarget target, string renderType, string renderQueue, bool blendModePreserveSpecular)
            {
                // SM 2.0, GLES

                // ForwardOnly pass is used as complex Lit SM 2.0 fallback for GLES.
                // Drops advanced features and renders materials as Lit.

                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = new List<string>() { UniversalTarget.kLitMaterialTypeTag, UniversalTarget.kTerrainMaterialTypeTag },
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection(),
                    shaderDependencies = new List<ShaderDependency>(),
                };

                result.passes.Add(TerrainLitPasses.Forward(target, blendModePreserveSpecular));

                // cull the shadowcaster pass if we know it will never be used
                if (target.castShadows || target.allowMaterialOverride)
                    result.passes.Add(TerrainLitPasses.ShadowCaster(target));

                if (target.mayWriteDepth)
                    result.passes.Add(TerrainLitPasses.DepthOnly(target));
                result.passes.Add(TerrainLitPasses.DepthNormal(target));
                result.passes.Add(TerrainLitPasses.Meta(target));
                // Currently neither of these passes (selection/picking) can be last for the game view for
                // UI shaders to render correctly. Verify [1352225] before changing this order.
                result.passes.Add(TerrainLitPasses.SceneSelection(target));
                result.passes.Add(TerrainLitPasses.ScenePicking(target));

                result.shaderDependencies.Add(TerrainDependencies.AddPassShader());
                result.shaderDependencies.Add(TerrainDependencies.BaseMapShader());
                result.shaderDependencies.Add(TerrainDependencies.BaseMapGenShader());

                return result;
            }
        }
        #endregion

        #region Pragmas
        static class TerrainCorePragmas
        {
            private static InstancingOptions[] InstancingOptionList()
            {
                return new []
                {
                    InstancingOptions.AssumeUniformScaling,
                    InstancingOptions.NoMatrices,
                    InstancingOptions.NoLightProbe,
                    InstancingOptions.NoLightmap,
                };
            }

            public static readonly PragmaCollection Default = new PragmaCollection
            {
                { Pragma.Target(ShaderModel.Target20) },
                { Pragma.OnlyRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore, Platform.D3D11 }) },
                { Pragma.MultiCompileInstancing },
                { Pragma.InstancingOptions(InstancingOptionList()) },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };

            public static readonly PragmaCollection Instanced = new PragmaCollection
            {
                { Pragma.Target(ShaderModel.Target20) },
                { Pragma.OnlyRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore, Platform.D3D11 }) },
                { Pragma.MultiCompileInstancing },
                { Pragma.InstancingOptions(InstancingOptionList()) },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };

            public static readonly PragmaCollection Forward = new PragmaCollection
            {
                { Pragma.Target(ShaderModel.Target20) },
                { Pragma.OnlyRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore, Platform.D3D11 }) },
                { Pragma.MultiCompileInstancing },
                { Pragma.MultiCompileFog },
                { Pragma.InstancingOptions(InstancingOptionList()) },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };

            public static readonly PragmaCollection DOTSDefault = new PragmaCollection
            {
                { Pragma.Target(ShaderModel.Target45) },
                { Pragma.ExcludeRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore }) },
                { Pragma.MultiCompileInstancing },
                { Pragma.InstancingOptions(InstancingOptionList()) },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };

            public static readonly PragmaCollection DOTSInstanced = new PragmaCollection
            {
                { Pragma.Target(ShaderModel.Target45) },
                { Pragma.ExcludeRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore }) },
                { Pragma.MultiCompileInstancing },
                { Pragma.InstancingOptions(InstancingOptionList()) },
                { Pragma.DOTSInstancing },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };

            public static readonly PragmaCollection DOTSForward = new PragmaCollection
            {
                { Pragma.Target(ShaderModel.Target45) },
                { Pragma.ExcludeRenderers(new[] {Platform.GLES, Platform.GLES3, Platform.GLCore}) },
                { Pragma.MultiCompileInstancing },
                { Pragma.MultiCompileFog },
                { Pragma.InstancingOptions(InstancingOptionList()) },
                { Pragma.DOTSInstancing },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };

            public static readonly PragmaCollection DOTSGBuffer = new PragmaCollection
            {
                { Pragma.Target(ShaderModel.Target45) },
                { Pragma.ExcludeRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore }) },
                { Pragma.MultiCompileInstancing },
                { Pragma.MultiCompileFog },
                { Pragma.InstancingOptions(InstancingOptionList()) },
                { Pragma.DOTSInstancing },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };
        }
        #endregion

        #region Passes
        static class TerrainLitPasses
        {
            static void AddReceiveShadowsControlToPass(ref PassDescriptor pass, UniversalTarget target, bool receiveShadows)
            {
                if (target.allowMaterialOverride)
                    pass.keywords.Add(LitKeywords.ReceiveShadowsOff);
                else if (!receiveShadows)
                    pass.defines.Add(LitKeywords.ReceiveShadowsOff, 1);
            }

            public static PassDescriptor Forward(UniversalTarget target, bool blendModePreserveSpecular, PragmaCollection pragmas = null)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Universal Forward",
                    referenceName = "SHADERPASS_FORWARD",
                    lightMode = "UniversalForward",
                    useInPreview = true,

                    // Template
                    passTemplatePath = TerrainLitTemplate.kPassTemplate,
                    sharedTemplateDirectories = TerrainLitTemplate.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = TerrainBlockMasks.Vertex,
                    validPixelBlocks = TerrainBlockMasks.FragmentLit,

                    // Fields
                    structs = TerrainStructCollections.Default,
                    requiredFields = TerrainRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target, blendModePreserveSpecular),
                    pragmas = pragmas ?? TerrainCorePragmas.Forward,
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog, },
                    keywords = new KeywordCollection() { LitKeywords.Forward },
                    includes = TerrainCoreIncludes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                result.defines.Add(TerrainDefines.TerrainEnabled, 1);
                result.defines.Add(TerrainDefines.TerrainSplat01, 1);
                result.defines.Add(TerrainDefines.TerrainSplat23, 1);
                result.keywords.Add(TerrainDefines.TerrainNormalmap);
                result.keywords.Add(TerrainDefines.TerrainMaskmap);
                result.keywords.Add(TerrainDefines.TerrainBlendHeight);
                result.keywords.Add(TerrainDefines.TerrainInstancedPerPixelNormal);
                result.defines.Add(TerrainDefines.MetallicSpecGlossMap, 1);
                result.defines.Add(TerrainDefines.SmoothnessTextureAlbedoChannelA, 1);

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target, blendModePreserveSpecular);
                AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);

                return result;
            }

            public static PassDescriptor DepthNormal(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "DepthNormals",
                    referenceName = "SHADERPASS_DEPTHNORMALS",
                    lightMode = "DepthNormals",
                    useInPreview = false,

                    // Template
                    passTemplatePath = TerrainLitTemplate.kPassTemplate,
                    sharedTemplateDirectories = TerrainLitTemplate.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = TerrainBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentDepthNormals,

                    // Fields
                    structs = TerrainStructCollections.Default,
                    requiredFields = TerrainRequiredFields.DepthNormals,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthNormalsOnly(target),
                    pragmas = TerrainCorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = TerrainCoreIncludes.DepthNormalsOnly,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                result.defines.Add(TerrainDefines.TerrainEnabled, 1);
                result.defines.Add(TerrainDefines.TerrainSplat01, 1);
                result.defines.Add(TerrainDefines.TerrainSplat23, 1);
                result.keywords.Add(TerrainDefines.TerrainNormalmap);
                result.keywords.Add(TerrainDefines.TerrainBlendHeight);
                result.keywords.Add(TerrainDefines.TerrainInstancedPerPixelNormal);

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            // Deferred only in SM4.5, MRT not supported in GLES2
            public static PassDescriptor GBuffer(UniversalTarget target, bool blendModePreserveSpecular)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "GBuffer",
                    referenceName = "SHADERPASS_GBUFFER",
                    lightMode = "UniversalGBuffer",

                    // Template
                    passTemplatePath = TerrainLitTemplate.kPassTemplate,
                    sharedTemplateDirectories = TerrainLitTemplate.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = TerrainBlockMasks.Vertex,
                    validPixelBlocks = TerrainBlockMasks.FragmentLit,

                    // Fields
                    structs = TerrainStructCollections.Default,
                    requiredFields = TerrainRequiredFields.GBuffer,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target, blendModePreserveSpecular),
                    pragmas = TerrainCorePragmas.DOTSGBuffer,
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection() { LitKeywords.GBuffer },
                    includes = TerrainCoreIncludes.GBuffer,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                result.defines.Add(TerrainDefines.TerrainEnabled, 1);
                result.defines.Add(TerrainDefines.TerrainSplat01, 1);
                result.defines.Add(TerrainDefines.TerrainSplat23, 1);
                result.keywords.Add(TerrainDefines.TerrainNormalmap);
                result.keywords.Add(TerrainDefines.TerrainMaskmap);
                result.keywords.Add(TerrainDefines.TerrainBlendHeight);
                result.keywords.Add(TerrainDefines.TerrainInstancedPerPixelNormal);
                result.defines.Add(TerrainDefines.MetallicSpecGlossMap, 1);
                result.defines.Add(TerrainDefines.SmoothnessTextureAlbedoChannelA, 1);

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target, blendModePreserveSpecular);
                AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);

                return result;
            }

            public static PassDescriptor ShadowCaster(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "ShadowCaster",
                    referenceName = "SHADERPASS_SHADOWCASTER",
                    lightMode = "ShadowCaster",

                    // Template
                    passTemplatePath = TerrainLitTemplate.kPassTemplate,
                    sharedTemplateDirectories = TerrainLitTemplate.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = TerrainBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = CoreRequiredFields.ShadowCaster,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.ShadowCaster(target),
                    pragmas = TerrainCorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection() { CoreKeywords.ShadowCaster },
                    includes = TerrainCoreIncludes.ShadowCaster,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                result.defines.Add(TerrainDefines.TerrainEnabled, 1);

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor DepthOnly(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "DepthOnly",
                    referenceName = "SHADERPASS_DEPTHONLY",
                    lightMode = "DepthOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = TerrainLitTemplate.kPassTemplate,
                    sharedTemplateDirectories = TerrainLitTemplate.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = TerrainBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = TerrainRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthOnly(target),
                    pragmas = TerrainCorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = TerrainCoreIncludes.DepthOnly,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                result.defines.Add(TerrainDefines.TerrainEnabled, 1);

                CorePasses.AddAlphaClipControlToPass(ref result, target);

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
                    passTemplatePath = TerrainLitTemplate.kPassTemplate,
                    sharedTemplateDirectories = TerrainLitTemplate.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = TerrainBlockMasks.Vertex,
                    validPixelBlocks = TerrainBlockMasks.FragmentMeta,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = TerrainRequiredFields.Meta,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Meta,
                    pragmas = TerrainCorePragmas.Default,
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection() { CoreKeywordDescriptors.EditorVisualization },
                    includes = TerrainCoreIncludes.Meta,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                result.defines.Add(TerrainDefines.TerrainEnabled, 1);
                result.defines.Add(TerrainDefines.MetallicSpecGlossMap, 1);
                result.defines.Add(TerrainDefines.SmoothnessTextureAlbedoChannelA, 1);

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor SceneSelection(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "SceneSelectionPass",
                    referenceName = "SHADERPASS_DEPTHONLY",
                    lightMode = "SceneSelectionPass",
                    useInPreview = false,

                    // Template
                    passTemplatePath = TerrainLitTemplate.kPassTemplate,
                    sharedTemplateDirectories = TerrainLitTemplate.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = TerrainBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                    // Fields
                    structs = CoreStructCollections.Default,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.SceneSelection(target),
                    pragmas = TerrainCorePragmas.Instanced,
                    defines = new DefineCollection { CoreDefines.SceneSelection, { CoreKeywordDescriptors.AlphaClipThreshold, 1 } },
                    keywords = new KeywordCollection(),
                    includes = TerrainCoreIncludes.SceneSelection,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                result.defines.Add(TerrainDefines.TerrainEnabled, 1);

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor ScenePicking(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "ScenePickingPass",
                    referenceName = "SHADERPASS_DEPTHONLY",
                    lightMode = "Picking",
                    useInPreview = false,

                    // Template
                    passTemplatePath = TerrainLitTemplate.kPassTemplate,
                    sharedTemplateDirectories = TerrainLitTemplate.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = TerrainBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                    // Fields
                    structs = CoreStructCollections.Default,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.ScenePicking(target),
                    pragmas = TerrainCorePragmas.Instanced,
                    defines = new DefineCollection { CoreDefines.ScenePicking, { CoreKeywordDescriptors.AlphaClipThreshold, 1 } },
                    keywords = new KeywordCollection(),
                    includes = TerrainCoreIncludes.ScenePicking,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                result.defines.Add(TerrainDefines.TerrainEnabled, 1);

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }
        }
        #endregion

        #region Dependencies
        static class TerrainDependencies
        {
            public static ShaderDependency AddPassShader()
            {
                return new ShaderDependency()
                {
                    dependencyName = "AddPassShader",
                    shaderName = "",
                };
            }
            public static ShaderDependency BaseMapShader()
            {
                return new ShaderDependency()
                {
                    dependencyName = "BaseMapShader",
                    shaderName = "",
                };
            }
            public static ShaderDependency BaseMapGenShader()
            {
                return new ShaderDependency()
                {
                    dependencyName = "BaseMapGenShader",
                    shaderName = "",
                };
            }
        }
        #endregion

        #region PortMasks
        static class TerrainBlockMasks
        {
            public static readonly BlockFieldDescriptor[] Vertex = new BlockFieldDescriptor[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
            };

            public static readonly BlockFieldDescriptor[] FragmentLit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Smoothness,
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

        #region RequiredField
        static class TerrainRequiredFields
        {
            public static readonly FieldCollection Forward = new FieldCollection()
            {
                StructFields.Attributes.instanceID,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.instanceID,
                TerrainStructFields.Varyings.uvSplat01,
                TerrainStructFields.Varyings.uvSplat23,
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
                TerrainStructFields.SurfaceDescriptionInputs.uvSplat01,
                TerrainStructFields.SurfaceDescriptionInputs.uvSplat23,
            };

            public static readonly FieldCollection DepthNormals = new FieldCollection()
            {
                StructFields.Attributes.instanceID,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.instanceID,
                TerrainStructFields.Varyings.uvSplat01,
                TerrainStructFields.Varyings.uvSplat23,
                TerrainStructFields.SurfaceDescriptionInputs.uvSplat01,
                TerrainStructFields.SurfaceDescriptionInputs.uvSplat23,
            };

            public static readonly FieldCollection GBuffer = new FieldCollection()
            {
                StructFields.Attributes.instanceID,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.instanceID,
                TerrainStructFields.Varyings.uvSplat01,
                TerrainStructFields.Varyings.uvSplat23,
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
                TerrainStructFields.SurfaceDescriptionInputs.uvSplat01,
                TerrainStructFields.SurfaceDescriptionInputs.uvSplat23,
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
                StructFields.Varyings.instanceID,
            };
        }
        #endregion

        #region Defines
        static class TerrainDefines
        {
            public static readonly KeywordDescriptor TerrainEnabled = new KeywordDescriptor()
            {
                displayName = "Universal Terrain",
                referenceName = "UNIVERSAL_TERRAIN_ENABLED",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordDescriptor TerrainSplat01 = new KeywordDescriptor()
            {
                displayName = "Universal Terrain Splat01",
                referenceName = "UNIVERSAL_TERRAIN_SPLAT01",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordDescriptor TerrainSplat23 = new KeywordDescriptor()
            {
                displayName = "Universal Terrain Splat23",
                referenceName = "UNIVERSAL_TERRAIN_SPLAT23",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor TerrainNormalmap = new KeywordDescriptor()
            {
                displayName = "Terrain Normal Map",
                referenceName = "_NORMALMAP",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static KeywordDescriptor TerrainMaskmap = new KeywordDescriptor()
            {
                displayName = "Terrain Mask Map",
                referenceName = "_MASKMAP",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment,
            };

            public static KeywordDescriptor TerrainBlendHeight = new KeywordDescriptor()
            {
                displayName = "Terrain Blend Height",
                referenceName = "_TERRAIN_BLEND_HEIGHT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment,
            };

            public static KeywordDescriptor TerrainInstancedPerPixelNormal = new KeywordDescriptor()
            {
                displayName = "Instanced PerPixel Normal",
                referenceName = "_TERRAIN_INSTANCED_PERPIXEL_NORMAL",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordDescriptor MetallicSpecGlossMap = new KeywordDescriptor()
            {
                displayName = "Metallic SpecGloss Map",
                referenceName = "_METALLICSPECGLOSSMAP",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordDescriptor SmoothnessTextureAlbedoChannelA = new KeywordDescriptor()
            {
                displayName = "Smoothness Texture Albedo Channel A",
                referenceName = "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };
        }
        #endregion

        #region Keywords
        static class LitKeywords
        {
            public static readonly KeywordDescriptor ReceiveShadowsOff = new KeywordDescriptor()
            {
                displayName = "Receive Shadows Off",
                referenceName = ShaderKeywordStrings._RECEIVE_SHADOWS_OFF,
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordDescriptor ScreenSpaceAmbientOcclusion = new KeywordDescriptor()
            {
                displayName = "Screen Space Ambient Occlusion",
                referenceName = "_SCREEN_SPACE_OCCLUSION",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static readonly KeywordCollection Forward = new KeywordCollection
            {
                { ScreenSpaceAmbientOcclusion },
                { CoreKeywordDescriptors.StaticLightmap },
                { CoreKeywordDescriptors.DynamicLightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.AdditionalLights },
                { CoreKeywordDescriptors.AdditionalLightShadows },
                { CoreKeywordDescriptors.ReflectionProbeBlending },
                { CoreKeywordDescriptors.ReflectionProbeBoxProjection },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { CoreKeywordDescriptors.DBuffer },
                { CoreKeywordDescriptors.LightLayers },
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywordDescriptors.LightCookies },
                { CoreKeywordDescriptors.ClusteredRendering },
            };

            public static readonly KeywordCollection GBuffer = new KeywordCollection
            {
                { CoreKeywordDescriptors.StaticLightmap },
                { CoreKeywordDescriptors.DynamicLightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.MixedLightingSubtractive },
                { CoreKeywordDescriptors.DBuffer },
                { CoreKeywordDescriptors.GBufferNormalsOct },
                { CoreKeywordDescriptors.LightLayers },
                { CoreKeywordDescriptors.RenderPassEnabled },
                { CoreKeywordDescriptors.DebugDisplay },
            };
        }
        #endregion

        #region Includes
        static class TerrainCoreIncludes
        {
            const string kShadows = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl";
            const string kMetaInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl";
            const string kDepthOnlyPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Terrain/DepthOnlyPass.hlsl";
            const string kDepthNormalsOnlyPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Terrain/DepthNormalsOnlyPass.hlsl";
            const string kShadowCasterPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Terrain/ShadowCasterPass.hlsl";
            const string kForwardPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Terrain/PBRForwardPass.hlsl";
            const string kGBuffer = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl";
            const string kPBRGBufferPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Terrain/PBRGBufferPass.hlsl";
            const string kLightingMetaPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl";
            const string kSelectionPickingPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl";

            const string kTerrainLitInput = "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl";
            const string kTerrainPassUtils = "Packages/com.unity.render-pipelines.universal/Editor/Terrain/TerrainPassUtils.hlsl";

            public static readonly IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { kShadows, IncludeLocation.Pregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },
                { kTerrainLitInput, IncludeLocation.Pregraph },
                { kTerrainPassUtils, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kForwardPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection DepthOnly = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kTerrainLitInput, IncludeLocation.Pregraph },
                { kTerrainPassUtils, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kDepthOnlyPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection DepthNormalsOnly = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kTerrainLitInput, IncludeLocation.Pregraph },
                { kTerrainPassUtils, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kDepthNormalsOnlyPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection ShadowCaster = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kTerrainLitInput, IncludeLocation.Pregraph },
                { kTerrainPassUtils, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kShadowCasterPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection GBuffer = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { kShadows, IncludeLocation.Pregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },
                { kTerrainLitInput, IncludeLocation.Pregraph },
                { kTerrainPassUtils, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kGBuffer, IncludeLocation.Postgraph },
                { kPBRGBufferPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection Meta = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kMetaInput, IncludeLocation.Pregraph },
                { kTerrainLitInput, IncludeLocation.Pregraph },
                { kTerrainPassUtils, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kLightingMetaPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection SceneSelection = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kTerrainLitInput, IncludeLocation.Pregraph },
                { kTerrainPassUtils, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSelectionPickingPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection ScenePicking = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kTerrainLitInput, IncludeLocation.Pregraph },
                { kTerrainPassUtils, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kSelectionPickingPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
