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

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class HDUnlitSubTarget : SubTarget<HDTarget>, IHasMetadata, ILegacyTarget,
        IRequiresData<SystemData>, IRequiresData<BuiltinData>, IRequiresData<HDUnlitData>
    {
        const string kAssetGuid = "4516595d40fa52047a77940183dc8e74";

        // Templates
        // TODO: Why do the raytracing passes use the template for the pipeline agnostic Unlit master node?
        // TODO: This should be resolved so we can delete the second pass template
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template";
        static string raytracingPassTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/UnlitPass.template";

        public HDUnlitSubTarget()
        {
            displayName = "Unlit";
        }

        // Render State
        string renderType => HDRenderTypeTags.HDUnlitShader.ToString();
        string renderQueue => HDRenderQueue.GetShaderTagValue(HDRenderQueue.ChangeType(systemData.renderingPass, systemData.sortPriority, systemData.alphaTest));

        // Material Data
        SystemData m_SystemData;
        BuiltinData m_BuiltinData;
        HDUnlitData m_UnlitData;

        // Interface Properties
        SystemData IRequiresData<SystemData>.data
        {
            get => m_SystemData;
            set => m_SystemData = value;
        }
        BuiltinData IRequiresData<BuiltinData>.data
        {
            get => m_BuiltinData;
            set => m_BuiltinData = value;
        }
        HDUnlitData IRequiresData<HDUnlitData>.data
        {
            get => m_UnlitData;
            set => m_UnlitData = value;
        }

        // Public properties
        public SystemData systemData
        {
            get => m_SystemData;
            set => m_SystemData = value;
        }
        public BuiltinData builtinData
        {
            get => m_BuiltinData;
            set => m_BuiltinData = value;
        }
        public HDUnlitData unlitData
        {
            get => m_UnlitData;
            set => m_UnlitData = value;
        }

        public override bool IsActive() => true;
        
        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));
            context.SetDefaultShaderGUI("Rendering.HighDefinition.HDUnlitGUI");
            
            // Process SubShaders
            SubShaderDescriptor[] subShaders = { SubShaders.Unlit, SubShaders.UnlitRaytracing };
            for(int i = 0; i < subShaders.Length; i++)
            {
                // Update Render State
                subShaders[i].renderType = renderType;
                subShaders[i].renderQueue = renderQueue;

                // Add
                context.AddSubShader(subShaders[i]);
            }
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            // Unlit
            context.AddField(HDFields.EnableShadowMatte,            unlitData.enableShadowMatte);

            // Distortion
            context.AddField(HDFields.DistortionAdd,                builtinData.distortionMode == DistortionMode.Add);
            context.AddField(HDFields.DistortionMultiply,           builtinData.distortionMode == DistortionMode.Multiply);
            context.AddField(HDFields.DistortionReplace,            builtinData.distortionMode == DistortionMode.Replace);
            context.AddField(HDFields.TransparentDistortion,        systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);
            context.AddField(HDFields.DistortionDepthTest,          builtinData.distortionDepthTest);

            // Alpha
            context.AddField(Fields.AlphaTest,                      systemData.alphaTest && context.pass.pixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold));
            context.AddField(HDFields.DoAlphaTest,                  systemData.alphaTest && context.pass.pixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold));
            context.AddField(Fields.AlphaToMask,                    systemData.alphaTest && context.pass.pixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) && builtinData.alphaToMask);
            context.AddField(HDFields.AlphaFog,                     systemData.surfaceType == SurfaceType.Transparent && builtinData.transparencyFog);

            // Misc
            context.AddField(Fields.VelocityPrecomputed,            builtinData.addPrecomputedVelocity);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);

            // Unlit
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, systemData.alphaTest);
            context.AddBlock(HDBlockFields.SurfaceDescription.ShadowTint,       unlitData.enableShadowMatte);

            // Distortion
            context.AddBlock(HDBlockFields.SurfaceDescription.Distortion,       systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);
            context.AddBlock(HDBlockFields.SurfaceDescription.DistortionBlur,   systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var settingsView = new HDUnlitSettingsView(this);
            settingsView.GetPropertiesGUI(ref context, onChange, registerUndo);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // Trunk currently relies on checking material property "_EmissionColor" to allow emissive GI. If it doesn't find that property, or it is black, GI is forced off.
            // ShaderGraph doesn't use this property, so currently it inserts a dummy color (white). This dummy color may be removed entirely once the following PR has been merged in trunk: Pull request #74105
            // The user will then need to explicitly disable emissive GI if it is not needed.
            // To be able to automatically disable emission based on the ShaderGraph config when emission is black,
            // we will need a more general way to communicate this to the engine (not directly tied to a material property).
            collector.AddShaderProperty(new ColorShaderProperty()
            {
                overrideReferenceName = "_EmissionColor",
                hidden = true,
                value = new Color(1.0f, 1.0f, 1.0f, 1.0f)
            });

            // ShaderGraph only property used to send the RenderQueueType to the material
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                overrideReferenceName = "_RenderQueueType",
                hidden = true,
                value = (int)systemData.renderingPass,
            });

            //See SG-ADDITIONALVELOCITY-NOTE
            if (builtinData.addPrecomputedVelocity)
            {
                collector.AddShaderProperty(new BooleanShaderProperty
                {
                    value  = true,
                    hidden = true,
                    overrideReferenceName = kAddPrecomputedVelocity,
                });
            }

            if (unlitData.enableShadowMatte)
            {
                uint mantissa = ((uint)LightFeatureFlags.Punctual | (uint)LightFeatureFlags.Directional | (uint)LightFeatureFlags.Area) & 0x007FFFFFu;
                uint exponent = 0b10000000u; // 0 as exponent
                collector.AddShaderProperty(new Vector1ShaderProperty
                {
                    hidden = true,
                    value = HDShadowUtils.Asfloat((exponent << 23) | mantissa),
                    overrideReferenceName = HDMaterialProperties.kShadowMatteFilter
                });
            }

            // Add all shader properties required by the inspector
            HDSubShaderUtilities.AddStencilShaderProperties(collector, false, false);
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                systemData.surfaceType,
                systemData.blendMode,
                systemData.sortPriority,
                systemData.alphaTest,
                systemData.zWrite,
                systemData.transparentCullMode,
                systemData.zTest,
                false,
                builtinData.transparencyFog
            );

            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, systemData.alphaTest, false);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, systemData.doubleSidedMode);
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            // Fixup the material settings:
            material.SetFloat(kSurfaceType, (int)(SurfaceType)systemData.surfaceType);
            material.SetFloat(kDoubleSidedEnable, systemData.doubleSidedMode != DoubleSidedMode.Disabled ? 1.0f : 0.0f);
            material.SetFloat(kAlphaCutoffEnabled, systemData.alphaTest ? 1 : 0);
            material.SetFloat(kBlendMode, (int)systemData.blendMode);
            material.SetFloat(kEnableFogOnTransparent, builtinData.transparencyFog ? 1.0f : 0.0f);
            material.SetFloat(kZTestTransparent, (int)systemData.zTest);
            material.SetFloat(kTransparentCullMode, (int)systemData.transparentCullMode);
            material.SetFloat(kZWrite, systemData.zWrite ? 1.0f : 0.0f);

            // No sorting priority for shader graph preview
            material.renderQueue = (int)HDRenderQueue.ChangeType(systemData.renderingPass, offset: 0, alphaTest: systemData.alphaTest);

            HDUnlitGUI.SetupMaterialKeywordsAndPass(material);
        }

        // IHasMetaData
        public string identifier => "HDUnlitSubTarget";

        public ScriptableObject GetMetadataObject()
        {
            var hdMetadata = ScriptableObject.CreateInstance<HDMetadata>();
            hdMetadata.shaderID = HDShaderUtils.ShaderID.SG_Unlit;
            return hdMetadata;
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            switch(masterNode)
            {
                case UnlitMasterNode1 unlitMasterNode:
                    UpgradeUnlitMasterNode(unlitMasterNode, out blockMap);
                    return true;
                case HDUnlitMasterNode1 hdUnlitMasterNode:
                    UpgradeHDUnlitMasterNode(hdUnlitMasterNode, out blockMap);
                    return true;
                default:
                    return false;
            }
        }

        void UpgradeUnlitMasterNode(UnlitMasterNode1 unlitMasterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            // Set data
            systemData.surfaceType = (SurfaceType)unlitMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)unlitMasterNode.m_AlphaMode);
            systemData.doubleSidedMode = unlitMasterNode.m_TwoSided ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled;
            systemData.alphaTest = HDSubShaderUtilities.UpgradeLegacyAlphaClip(unlitMasterNode);
            systemData.dotsInstancing = unlitMasterNode.m_DOTSInstancing;
            builtinData.addPrecomputedVelocity = unlitMasterNode.m_AddPrecomputedVelocity;
            target.customEditorGUI = unlitMasterNode.m_OverrideEnabled ? unlitMasterNode.m_ShaderGUIOverride : "";

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 9 },
                { BlockFields.VertexDescription.Normal, 10 },
                { BlockFields.VertexDescription.Tangent, 11 },
                { BlockFields.SurfaceDescription.BaseColor, 0 },
                { BlockFields.SurfaceDescription.Alpha, 7 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 8 },
            };
        }

        void UpgradeHDUnlitMasterNode(HDUnlitMasterNode1 hdUnlitMasterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            // Set data
            systemData.surfaceType = (SurfaceType)hdUnlitMasterNode.m_SurfaceType;
            systemData.blendMode = HDSubShaderUtilities.UpgradeLegacyAlphaModeToBlendMode((int)hdUnlitMasterNode.m_AlphaMode);
            systemData.renderingPass = hdUnlitMasterNode.m_RenderingPass;
            systemData.alphaTest = hdUnlitMasterNode.m_AlphaTest;
            systemData.sortPriority = hdUnlitMasterNode.m_SortPriority;
            systemData.doubleSidedMode = hdUnlitMasterNode.m_DoubleSided ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled;
            systemData.zWrite = hdUnlitMasterNode.m_ZWrite;
            systemData.transparentCullMode = hdUnlitMasterNode.m_transparentCullMode;
            systemData.zTest = hdUnlitMasterNode.m_ZTest;
            systemData.dotsInstancing = hdUnlitMasterNode.m_DOTSInstancing;

            builtinData.transparencyFog = hdUnlitMasterNode.m_TransparencyFog;
            builtinData.distortion = hdUnlitMasterNode.m_Distortion;
            builtinData.distortionMode = hdUnlitMasterNode.m_DistortionMode;
            builtinData.distortionDepthTest = hdUnlitMasterNode.m_DistortionDepthTest;
            builtinData.alphaToMask = hdUnlitMasterNode.m_AlphaToMask;
            builtinData.addPrecomputedVelocity = hdUnlitMasterNode.m_AddPrecomputedVelocity;

            unlitData.enableShadowMatte = hdUnlitMasterNode.m_EnableShadowMatte;
            target.customEditorGUI = hdUnlitMasterNode.m_OverrideEnabled ? hdUnlitMasterNode.m_ShaderGUIOverride : "";

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 9 },
                { BlockFields.VertexDescription.Normal, 13 },
                { BlockFields.VertexDescription.Tangent, 14 },
                { BlockFields.SurfaceDescription.BaseColor, 0 },
                { BlockFields.SurfaceDescription.Alpha, 7 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 8 },
                { BlockFields.SurfaceDescription.Emission, 12 },
            };

            // Distortion
            if(systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.Distortion, 10);
                blockMap.Add(HDBlockFields.SurfaceDescription.DistortionBlur, 11);
            }

            // Shadow Matte
            if(unlitData.enableShadowMatte)
            {
                blockMap.Add(HDBlockFields.SurfaceDescription.ShadowTint, 15);
            }
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor Unlit = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { UnlitPasses.ShadowCaster },
                    { UnlitPasses.META },
                    { UnlitPasses.SceneSelection },
                    { UnlitPasses.DepthForwardOnly },
                    { UnlitPasses.MotionVectors },
                    { UnlitPasses.Distortion, new FieldCondition(HDFields.TransparentDistortion, true) },
                    { UnlitPasses.ForwardOnly },
                },
            };

            public static SubShaderDescriptor UnlitRaytracing = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = false,
                passes = new PassCollection
                {
                    { UnlitPasses.RaytracingIndirect, new FieldCondition(Fields.IsPreview, false) },
                    { UnlitPasses.RaytracingVisibility, new FieldCondition(Fields.IsPreview, false) },
                    { UnlitPasses.RaytracingForward, new FieldCondition(Fields.IsPreview, false) },
                    { UnlitPasses.RaytracingGBuffer, new FieldCondition(Fields.IsPreview, false) },
                    { UnlitPasses.RaytracingPathTracing, new FieldCondition(Fields.IsPreview, false) },
                },
            };
        }
#endregion

#region Passes
        static class UnlitPasses
        {
            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Block Mask
                pixelBlocks = UnlitBlockMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ CoreRequiredFields.Meta, HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = UnlitIncludes.Meta,
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Block Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = UnlitBlockMasks.FragmentOnlyAlpha,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.ShadowCaster,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = UnlitIncludes.DepthOnly,
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Block Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = UnlitBlockMasks.FragmentOnlyAlpha,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.SceneSelection,
                pragmas = CorePragmas.DotsInstancedInV2OnlyEditorSync,
                defines = CoreDefines.SceneSelection,
                keywords = CoreKeywords.HDBase,
                includes = UnlitIncludes.DepthOnly,
            };

            public static PassDescriptor DepthForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Block Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = UnlitBlockMasks.FragmentOnlyAlpha,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.DepthForwardOnly,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = UnlitKeywords.DepthMotionVectors,
                includes = UnlitIncludes.DepthOnly,
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Block Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = UnlitBlockMasks.FragmentOnlyAlpha,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ CoreRequiredFields.PositionRWS, HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.MotionVectors,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = UnlitKeywords.DepthMotionVectors,
                includes = UnlitIncludes.MotionVectors,
            };

            public static PassDescriptor Distortion = new PassDescriptor()
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Block Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = UnlitBlockMasks.FragmentDistortion,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.Distortion,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = CoreKeywords.HDBase,
                includes = UnlitIncludes.Distortion,
            };

            public static PassDescriptor ForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_UNLIT",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Block Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = UnlitBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Forward,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                keywords = UnlitKeywords.Forward,
                includes = UnlitIncludes.ForwardOnly,
            };

            public static PassDescriptor RaytracingIndirect = new PassDescriptor()
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                useInPreview = false,

                // Template
                passTemplatePath = raytracingPassTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Block Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = UnlitBlockMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                keywords = CoreKeywords.HDBase,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RaytracingIndirect },
            };

            public static PassDescriptor RaytracingVisibility = new PassDescriptor()
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                useInPreview = false,

                // Template
                passTemplatePath = raytracingPassTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Block Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = UnlitBlockMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                keywords = CoreKeywords.HDBase,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RaytracingVisibility },
            };

            public static PassDescriptor RaytracingForward = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                useInPreview = false,

                // Template
                passTemplatePath = raytracingPassTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Block Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = UnlitBlockMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                keywords = CoreKeywords.HDBase,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RaytracingForward },
            };

            public static PassDescriptor RaytracingGBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                useInPreview = false,

                // Template
                passTemplatePath = raytracingPassTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Block Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = UnlitBlockMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                keywords = CoreKeywords.HDBase,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RayTracingGBuffer },
            };

            public static PassDescriptor RaytracingPathTracing = new PassDescriptor()
            {
                //Definition
                displayName = "PathTracingDXR",
                referenceName = "SHADERPASS_PATH_TRACING",
                lightMode = "PathTracingDXR",
                useInPreview = false,

                // Template
                passTemplatePath = raytracingPassTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Block Mask
                vertexBlocks = CoreBlockMasks.Vertex,
                pixelBlocks = UnlitBlockMasks.FragmentDefault,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                keywords = CoreKeywords.HDBase,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RaytracingPathTracing },
            };
        }
#endregion

#region BlockMasks
        static class UnlitBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentDefault = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                BlockFields.SurfaceDescription.Emission,
            };

            public static BlockFieldDescriptor[] FragmentOnlyAlpha = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };

            public static BlockFieldDescriptor[] FragmentDistortion = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.Distortion,
                HDBlockFields.SurfaceDescription.DistortionBlur,
            };

            public static BlockFieldDescriptor[] FragmentForward = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                BlockFields.SurfaceDescription.Emission,
                HDBlockFields.SurfaceDescription.ShadowTint,
            };
        }
#endregion

#region RenderStates
        static class UnlitRenderStates
        {
            public static RenderStateCollection SceneSelection = new RenderStateCollection
            {
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.ColorMask("ColorMask 0") },
            };

            // Caution: When using MSAA we have normal and depth buffer bind.
            // Unlit objects need to NOT write in normal buffer (or write 0) - Disable color mask for this RT
            // Note: ShaderLab doesn't allow to have a variable on the second parameter of ColorMask
            // - When MSAA: disable target 1 (normal buffer)
            // - When no MSAA: disable target 0 (normal buffer) and 1 (unused)
            public static RenderStateCollection DepthForwardOnly = new RenderStateCollection
            {
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.ColorMask("ColorMask [_ColorMaskNormal]") },
                { RenderState.ColorMask("ColorMask 0 1") },
                { RenderState.AlphaToMask(CoreRenderStates.Uniforms.alphaToMask), new FieldCondition(Fields.AlphaToMask, true) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskDepth,
                    Ref = CoreRenderStates.Uniforms.stencilRefDepth,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            // Caution: When using MSAA we have motion vector, normal and depth buffer bind.
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            public static RenderStateCollection MotionVectors = new RenderStateCollection
            {
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.ColorMask("ColorMask [_ColorMaskNormal] 1") },
                { RenderState.ColorMask("ColorMask 0 2") },
                { RenderState.AlphaToMask(CoreRenderStates.Uniforms.alphaToMask), new FieldCondition(Fields.AlphaToMask, true) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskMV,
                    Ref = CoreRenderStates.Uniforms.stencilRefMV,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection Distortion = new RenderStateCollection
            {
                { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(HDFields.DistortionAdd, true) },
                { RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.DstAlpha, Blend.Zero), new FieldCondition(HDFields.DistortionMultiply, true) },
                { RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(HDFields.DistortionReplace, true) },
                { RenderState.BlendOp(BlendOp.Add, BlendOp.Add) },
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.ZTest(ZTest.Always), new FieldCondition(HDFields.DistortionDepthTest, false) },
                { RenderState.ZTest(ZTest.LEqual), new FieldCondition(HDFields.DistortionDepthTest, true) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMaskDistortionVec,
                    Ref = CoreRenderStates.Uniforms.stencilRefDistortionVec,
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };
        }
#endregion

#region Keywords
        static class UnlitKeywords
        {
            public static KeywordCollection DepthMotionVectors = new KeywordCollection
            {
                { CoreKeywords.HDBase },
                { CoreKeywordDescriptors.WriteMsaaDepth },
                { CoreKeywordDescriptors.AlphaToMask, new FieldCondition(Fields.AlphaToMask, true) },
            };

            public static KeywordCollection Forward = new KeywordCollection
            {
                { CoreKeywords.HDBase },
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywordDescriptors.Shadow, new FieldCondition(HDFields.EnableShadowMatte, true) },
            };
        }
#endregion

#region Includes
        static class UnlitIncludes
        {
            const string kPassForwardUnlit = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl";
            
            public static IncludeCollection Meta = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kUnlit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kPassLightTransport, IncludeLocation.Postgraph },
            };

            public static IncludeCollection DepthOnly = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kUnlit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph },
            };

            public static IncludeCollection MotionVectors = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kUnlit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kPassMotionVectors, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Distortion = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kUnlit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kDisortionVectors, IncludeLocation.Postgraph },
            };

            public static IncludeCollection ForwardOnly = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kUnlit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kHDShadow, IncludeLocation.Pregraph, new FieldCondition(HDFields.EnableShadowMatte, true) },
                { CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph, new FieldCondition(HDFields.EnableShadowMatte, true) },
                { CoreIncludes.kPunctualLightCommon, IncludeLocation.Pregraph, new FieldCondition(HDFields.EnableShadowMatte, true) },
                { CoreIncludes.kHDShadowLoop, IncludeLocation.Pregraph, new FieldCondition(HDFields.EnableShadowMatte, true) },
                { kPassForwardUnlit, IncludeLocation.Postgraph },
            };
        }
#endregion
    }
}
