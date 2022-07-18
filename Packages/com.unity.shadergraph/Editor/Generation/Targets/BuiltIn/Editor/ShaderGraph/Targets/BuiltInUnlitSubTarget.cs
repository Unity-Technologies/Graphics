using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    sealed class BuiltInUnlitSubTarget : BuiltInSubTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("3af09b75886c549dbad6eaaaaf342387"); // BuiltInUnlitSubTarget.cs

        public BuiltInUnlitSubTarget()
        {
            displayName = "Unlit";
        }

        protected override ShaderID shaderID => ShaderID.SG_Unlit;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            // If there is a custom GUI, we need to leave out the subtarget GUI or it will override the custom one due to ordering
            // (Subtarget setup happens first, so it would always "win")
            var biTarget = target as BuiltInTarget;
            if (!context.HasCustomEditorForRenderPipeline(null) && string.IsNullOrEmpty(biTarget.customEditorGUI))
                context.customEditorForRenderPipelines.Add((typeof(BuiltInUnlitGUI).FullName, ""));

            // Process SubShaders
            context.AddSubShader(SubShaders.Unlit(target, target.renderType, target.renderQueue));
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.allowMaterialOverride)
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
            }

            // We always need these properties regardless of whether the material is allowed to override
            // Queue control & offset enable correct automatic render queue behavior
            // Control == 0 is automatic, 1 is user-specified render queue
            material.SetFloat(Property.QueueOffset(), 0.0f);
            material.SetFloat(Property.QueueControl(), (float)BuiltInBaseShaderGUI.QueueControl.Auto);

            // call the full unlit material setup function
            BuiltInUnlitGUI.UpdateMaterial(material);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Alpha, (target.surfaceType == SurfaceType.Transparent || target.alphaClip) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, target.alphaClip || target.allowMaterialOverride);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (target.allowMaterialOverride)
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
            }

            // We always need these properties regardless of whether the material is allowed to override other shader properties.
            // Queue control & offset enable correct automatic render queue behavior.  Control == 0 is automatic, 1 is user-specified.
            // We initialize queue control to -1 to indicate to UpdateMaterial that it needs to initialize it properly on the material.
            collector.AddFloatProperty(Property.QueueOffset(), 0.0f);
            collector.AddFloatProperty(Property.QueueControl(), -1.0f);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            // show the target default surface properties
            var builtInTarget = (target as BuiltInTarget);
            builtInTarget?.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);
            builtInTarget?.GetDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo);
        }

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor Unlit(BuiltInTarget target, string renderType, string renderQueue)
            {
                var result = new SubShaderDescriptor()
                {
                    //pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = BuiltInTarget.kUnlitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                result.passes.Add(UnlitPasses.Unlit(target));

                if (target.mayWriteDepth)
                    result.passes.Add(CorePasses.DepthOnly(target));

                result.passes.Add(CorePasses.ShadowCaster(target));
                result.passes.Add(CorePasses.SceneSelection(target));
                result.passes.Add(CorePasses.ScenePicking(target));

                return result;
            }
        }
        #endregion

        #region Pass
        static class UnlitPasses
        {
            public static PassDescriptor Unlit(BuiltInTarget target)
            {
                var result = new PassDescriptor
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
                    renderStates = CoreRenderStates.Default(target),
                    pragmas = CorePragmas.Forward,
                    defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
                    keywords = new KeywordCollection(),
                    includes = UnlitIncludes.Unlit,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                return result;
            }
        }
        #endregion

        #region Keywords
        static class UnlitKeywords
        {
            public static KeywordCollection Unlit(BuiltInTarget target)
            {
                var result = new KeywordCollection
                {
                    { CoreKeywordDescriptors.Lightmap },
                    { CoreKeywordDescriptors.DirectionalLightmapCombined },
                    { CoreKeywordDescriptors.SampleGI },
                };

                return result;
            }
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
