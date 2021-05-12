using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    sealed class BuiltInUnlitSubTarget : SubTarget<BuiltInTarget>
    {
        static readonly GUID kSourceCodeGuid = new GUID("3af09b75886c549dbad6eaaaaf342387"); // BuiltInUnlitSubTarget.cs

        public BuiltInUnlitSubTarget()
        {
            displayName = "Unlit";
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            if (!context.HasCustomEditorForRenderPipeline(null))
                context.customEditorForRenderPipelines.Add((typeof(BuiltInUnlitGUI).FullName, ""));

            // Process SubShaders
            SubShaderDescriptor[] subShaders = { SubShaders.Unlit };
            for (int i = 0; i < subShaders.Length; i++)
            {
                // Update Render State
                subShaders[i].renderType = target.renderType;
                subShaders[i].renderQueue = target.renderQueue;

                // Add
                context.AddSubShader(subShaders[i]);
            }
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            // copy our target's default settings into the material
            // (technically not necessary since we are always recreating the material from the shader each time,
            // which will pull over the defaults from the shader definition)
            // but if that ever changes, this will ensure the defaults are set
            material.SetFloat(Property.Surface(), (float)target.surfaceType);
            material.SetFloat(Property.Blend(), (float)target.alphaMode);
            material.SetFloat(Property.AlphaClip(), target.alphaClip ? 1.0f : 0.0f);
            material.SetFloat(Property.Cull(), (int)target.renderFace);
            material.SetFloat(Property.ZWriteControl(), (float)target.zWriteControl);
            material.SetFloat(Property.ZTest(), (float)target.zTestMode);

            // call the full unlit material setup function
            BuiltInUnlitGUI.UpdateMaterial(material);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            // Surface Type & Blend Mode
            // These must be set per SubTarget as Sprite SubTargets override them
            context.AddField(BuiltInFields.SurfaceOpaque,       target.surfaceType == SurfaceType.Opaque);
            context.AddField(BuiltInFields.SurfaceTransparent,  target.surfaceType != SurfaceType.Opaque);
            context.AddField(BuiltInFields.BlendAdd,            target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Additive);
            context.AddField(Fields.BlendAlpha,                 target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Alpha);
            context.AddField(BuiltInFields.BlendMultiply,       target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Multiply);
            context.AddField(BuiltInFields.BlendPremultiply,    target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Premultiply);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Always add the alpha and alpha clip blocks. These may or may not be active depending on the material controls so we have to always add them.
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            // setup properties using the defaults
            collector.AddFloatProperty(Property.Surface(), (float)target.surfaceType);
            collector.AddFloatProperty(Property.Blend(), (float)target.alphaMode);
            collector.AddFloatProperty(Property.AlphaClip(), target.alphaClip ? 1.0f : 0.0f);
            collector.AddFloatProperty(Property.SrcBlend(), 1.0f);    // always set by material inspector (TODO : get src/dst blend and set here?)
            collector.AddFloatProperty(Property.DstBlend(), 0.0f);    // always set by material inspector
            collector.AddFloatProperty(Property.ZWrite(), (target.surfaceType == SurfaceType.Opaque) ? 1.0f : 0.0f);
            collector.AddFloatProperty(Property.ZWriteControl(), (float)target.zWriteControl);
            collector.AddFloatProperty(Property.ZTest(), (float)target.zTestMode);    // ztest mode is designed to directly pass as ztest
            collector.AddFloatProperty(Property.Cull(), (float)target.renderFace);    // render face enum is designed to directly pass as a cull mode
            collector.AddFloatProperty(Property.QueueOffset(), 0.0f);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            // show the target default surface properties
            var builtInTarget = (target as BuiltInTarget);
            builtInTarget?.GetDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo);
        }

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor Unlit = new SubShaderDescriptor()
            {
                //pipelineTag = BuiltInTarget.kPipelineTag,
                customTags = BuiltInTarget.kUnlitMaterialTypeTag,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { UnlitPasses.Unlit },
                    { CorePasses.ShadowCaster },
                    { CorePasses.DepthOnly },
                    { CorePasses.SceneSelection },
                    { CorePasses.ScenePicking },
                },
            };
        }
        #endregion

        #region Pass
        static class UnlitPasses
        {
            public static PassDescriptor Unlit = new PassDescriptor
            {
                // Definition
                displayName = "Pass",
                referenceName = "SHADERPASS_UNLIT",
                lightMode = "ForwardBase",
                useInPreview = true,

                // Template
                passTemplatePath = BuiltInTarget.kTemplatePath,
                sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.Default,
                pragmas = CorePragmas.Forward,
                defines = CoreDefines.BuiltInTargetAPI,
                keywords = UnlitKeywords.Unlit,
                includes = UnlitIncludes.Unlit,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };
        }
        #endregion

        #region Keywords
        static class UnlitKeywords
        {
            public static KeywordCollection Unlit = new KeywordCollection
            {
                { CoreKeywordDescriptors.Lightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.SampleGI },
                CoreKeywordDescriptors.AlphaClip,
                CoreKeywordDescriptors.AlphaTestOn,
                CoreKeywordDescriptors.SurfaceTypeTransparent,
                CoreKeywordDescriptors.AlphaPremultiplyOn,
            };
        }
        #endregion

        #region Includes
        static class UnlitIncludes
        {
            const string kUnlitPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/UnlitPass.hlsl";

            public static IncludeCollection Unlit = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kUnlitPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
