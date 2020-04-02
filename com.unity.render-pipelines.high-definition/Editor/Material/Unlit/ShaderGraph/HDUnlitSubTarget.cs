using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.UIElements;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class HDUnlitSubTarget : SubTarget<HDTarget>
    {
        const string kAssetGuid = "4516595d40fa52047a77940183dc8e74";

        // Why do the raytracing passes use the template for the pipeline agnostic Unlit master node?
        // This should be resolved so we can delete the second pass template
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template";
        static string raytracingPassTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/UnlitPass.template";

        // TODO: This isnt used anywhere?
        [SerializeField]
        bool m_DistortionOnly = true;

        [SerializeField]
        bool m_EnableShadowMatte = false;

        public HDUnlitSubTarget()
        {
            displayName = "Unlit";
        }

        string renderType => HDRenderTypeTags.HDUnlitShader.ToString();
        string renderQueue => HDRenderQueue.GetShaderTagValue(HDRenderQueue.ChangeType(target.renderingPass, target.sortPriority, target.alphaTest));

        public bool distortionOnly
        {
            get => m_DistortionOnly;
            set => m_DistortionOnly = value;
        }

        public bool enableShadowMatte
        {
            get => m_EnableShadowMatte;
            set => m_EnableShadowMatte = value;
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
            context.AddField(HDFields.EnableShadowMatte, enableShadowMatte);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Unlit
            context.AddBlock(HDBlockFields.SurfaceDescription.ShadowTint, enableShadowMatte);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange)
        {
            context.AddProperty("Surface Type", 0, new EnumField(SurfaceType.Opaque) { value = target.surfaceType }, (evt) =>
            {
                if (Equals(target.surfaceType, evt.newValue))
                    return;

                target.surfaceType = (SurfaceType)evt.newValue;
                target.UpdateRenderingPassValue(target.renderingPass);
                onChange();
            });

            var renderingPassList = HDSubShaderUtilities.GetRenderingPassList(target.surfaceType == SurfaceType.Opaque, true);
            var renderingPassValue = target.surfaceType == SurfaceType.Opaque ? HDRenderQueue.GetOpaqueEquivalent(target.renderingPass) : HDRenderQueue.GetTransparentEquivalent(target.renderingPass);
            var renderQueueType = target.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            context.AddProperty("Rendering Pass", 1, new PopupField<HDRenderQueue.RenderQueueType>(renderingPassList, renderQueueType, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName) { value = renderingPassValue }, (evt) =>
            {
                if(target.ChangeRenderingPass(evt.newValue))
                {
                    onChange();
                }
            });

            context.AddProperty("Blending Mode", 1, new EnumField(AlphaMode.Alpha) { value = target.alphaMode }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.alphaMode, evt.newValue))
                    return;

                target.alphaMode = (AlphaMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Test", 1, new EnumField(target.zTest) { value = target.zTest }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.zTest, evt.newValue))
                    return;

                target.zTest = (CompareFunction)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Write", 1, new Toggle() { value = target.zWrite }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.zWrite, evt.newValue))
                    return;

                target.zWrite = evt.newValue;
                onChange();
            });

            context.AddProperty("Cull Mode", 1, new EnumField(target.transparentCullMode) { value = target.transparentCullMode }, target.surfaceType == SurfaceType.Transparent && target.doubleSidedMode != DoubleSidedMode.Disabled, (evt) =>
            {
                if (Equals(target.transparentCullMode, evt.newValue))
                    return;

                target.transparentCullMode = (TransparentCullMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Sorting Priority", 1, new IntegerField() { value = target.sortPriority }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.sortPriority, evt.newValue))
                    return;

                target.sortPriority = evt.newValue;
                onChange();
            });

            context.AddProperty("Receive Fog", 1, new Toggle() { value = target.transparencyFog }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.transparencyFog, evt.newValue))
                    return;

                target.transparencyFog = evt.newValue;
                onChange();
            });

            context.AddProperty("Distortion", 1, new Toggle() { value = target.distortion }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.distortion, evt.newValue))
                    return;

                target.distortion = evt.newValue;
                onChange();
            });

            context.AddProperty("Distortion Blend Mode", 2, new EnumField(DistortionMode.Add) { value = target.distortionMode }, target.surfaceType == SurfaceType.Transparent && target.distortion, (evt) =>
            {
                if (Equals(target.distortionMode, evt.newValue))
                    return;

                target.distortionMode = (DistortionMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Distortion Only", 2, new Toggle() { value = distortionOnly }, target.surfaceType == SurfaceType.Transparent && target.distortion, (evt) =>
            {
                if (Equals(distortionOnly, evt.newValue))
                    return;

                distortionOnly = evt.newValue;
                onChange();
            });

            context.AddProperty("Distortion Depth Test", 2, new Toggle() { value = target.distortionDepthTest }, target.surfaceType == SurfaceType.Transparent && target.distortion, (evt) =>
            {
                if (Equals(target.distortionDepthTest, evt.newValue))
                    return;

                target.distortionDepthTest = evt.newValue;
                onChange();
            });

            context.AddProperty("Double-Sided", 0, new EnumField(DoubleSidedMode.Disabled) { value = target.doubleSidedMode }, (evt) =>
            {
                if (Equals(target.doubleSidedMode, evt.newValue))
                    return;

                target.doubleSidedMode = (DoubleSidedMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Alpha Clipping", 0, new Toggle() { value = target.alphaTest }, (evt) =>
            {
                if (Equals(target.alphaTest, evt.newValue))
                    return;

                target.alphaTest = evt.newValue;
                onChange();
            });

            context.AddProperty("Add Precomputed Velocity", 0, new Toggle() { value = target.addPrecomputedVelocity }, (evt) =>
            {
                if (Equals(target.addPrecomputedVelocity, evt.newValue))
                    return;

                target.addPrecomputedVelocity = evt.newValue;
                onChange();
            });

            context.AddProperty("Shadow Matte", 0, new Toggle() { value = enableShadowMatte }, (evt) =>
            {
                if (Equals(enableShadowMatte, evt.newValue))
                    return;

                enableShadowMatte = evt.newValue;
                onChange();
            });
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (m_EnableShadowMatte)
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
                target.surfaceType,
                HDSubShaderUtilities.ConvertAlphaModeToBlendMode(target.alphaMode),
                target.sortPriority,
                target.zWrite,
                target.transparentCullMode,
                target.zTest,
                false,
                target.transparencyFog
            );
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, target.alphaTest, false);
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

                // Port Mask
                pixelBlocks = UnlitPortMasks.FragmentDefault,

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

                // Port Mask
                vertexBlocks = UnlitPortMasks.Vertex,
                pixelBlocks = UnlitPortMasks.FragmentOnlyAlpha,

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

                // Port Mask
                vertexBlocks = UnlitPortMasks.Vertex,
                pixelBlocks = UnlitPortMasks.FragmentOnlyAlpha,

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

                // Port Mask
                vertexBlocks = UnlitPortMasks.Vertex,
                pixelBlocks = UnlitPortMasks.FragmentOnlyAlpha,

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

                // Port Mask
                vertexBlocks = UnlitPortMasks.Vertex,
                pixelBlocks = UnlitPortMasks.FragmentOnlyAlpha,

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

                // Port Mask
                vertexBlocks = UnlitPortMasks.Vertex,
                pixelBlocks = UnlitPortMasks.FragmentDistortion,

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

                // Port Mask
                vertexBlocks = UnlitPortMasks.Vertex,
                pixelBlocks = UnlitPortMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = UnlitRenderStates.Forward,
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

                // Port Mask
                vertexBlocks = UnlitPortMasks.Vertex,
                pixelBlocks = UnlitPortMasks.FragmentDefault,

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

                // Port Mask
                vertexBlocks = UnlitPortMasks.Vertex,
                pixelBlocks = UnlitPortMasks.FragmentDefault,

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

                // Port Mask
                vertexBlocks = UnlitPortMasks.Vertex,
                pixelBlocks = UnlitPortMasks.FragmentDefault,

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

                // Port Mask
                vertexBlocks = UnlitPortMasks.Vertex,
                pixelBlocks = UnlitPortMasks.FragmentDefault,

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

                // Port Mask
                vertexBlocks = UnlitPortMasks.Vertex,
                pixelBlocks = UnlitPortMasks.FragmentDefault,

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

#region PortMasks
        static class UnlitPortMasks
        {
            public static BlockFieldDescriptor[] Vertex = new BlockFieldDescriptor[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
            };

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
            // Mean unlit object need to not write in it (or write 0) - Disable color mask for this RT
            // This is not a problem in no MSAA mode as there is no buffer bind
            public static RenderStateCollection DepthForwardOnly = new RenderStateCollection
            {
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.ColorMask("ColorMask 0 0") },
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
                { RenderState.ColorMask("ColorMask 0 1") },
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
                { RenderState.BlendOp(UnityEditor.ShaderGraph.BlendOp.Add, UnityEditor.ShaderGraph.BlendOp.Add) },
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

            public static RenderStateCollection Forward = new RenderStateCollection
            {
                { RenderState.Blend(CoreRenderStates.Uniforms.srcBlend, CoreRenderStates.Uniforms.dstBlend, CoreRenderStates.Uniforms.alphaSrcBlend, CoreRenderStates.Uniforms.alphaDstBlend) },
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(CoreRenderStates.Uniforms.zWrite) },
                { RenderState.ZTest(CoreRenderStates.Uniforms.zTestTransparent) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = CoreRenderStates.Uniforms.stencilWriteMask,
                    Ref = CoreRenderStates.Uniforms.stencilRef,
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
            };

            public static KeywordCollection Forward = new KeywordCollection
            {
                { CoreKeywords.HDBase },
                { CoreKeywordDescriptors.DebugDisplay },
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
                { CoreIncludes.kCommonLighting, IncludeLocation.Pregraph, new FieldCondition(HDFields.EnableShadowMatte, true) },
                { CoreIncludes.kShadowContext, IncludeLocation.Pregraph, new FieldCondition(HDFields.EnableShadowMatte, true) },
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
