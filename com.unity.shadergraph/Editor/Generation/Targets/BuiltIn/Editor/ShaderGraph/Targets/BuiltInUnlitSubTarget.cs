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
    sealed class BuiltInUnlitSubTarget : SubTarget<BuiltInTarget>, ILegacyTarget
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
            context.AddBlock(BlockFields.SurfaceDescription.Alpha,              target.surfaceType == SurfaceType.Transparent || target.alphaClip);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, target.alphaClip);
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
            context.AddProperty("Surface", new EnumField(SurfaceType.Opaque) { value = target.surfaceType }, (evt) =>
            {
                if (Equals(target.surfaceType, evt.newValue))
                    return;

                registerUndo("Change Surface");
                target.surfaceType = (SurfaceType)evt.newValue;
                onChange();
            });

            context.AddProperty("Blend", new EnumField(AlphaMode.Alpha) { value = target.alphaMode }, target.surfaceType == SurfaceType.Transparent, (evt) =>
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

            context.AddProperty("Depth Test", new EnumField(ZTestMode.LEqual) { value = target.zTestMode }, (evt) =>
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

            context.AddProperty("Render Face", new EnumField(RenderFace.Front) { value = target.renderFace }, (evt) =>
            {
                if (Equals(target.renderFace, evt.newValue))
                    return;

                registerUndo("Change Render Face");
                target.renderFace = (RenderFace)evt.newValue;
                onChange();
            });
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if (!(masterNode is UnlitMasterNode1 unlitMasterNode))
                return false;

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

            return true;
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
                },
            };

            public static SubShaderDescriptor UnlitDOTS
            {
                get
                {
                    var unlit = UnlitPasses.Unlit;
                    var shadowCaster = CorePasses.ShadowCaster;
                    var depthOnly = CorePasses.DepthOnly;

                    unlit.pragmas = CorePragmas.Forward;
                    shadowCaster.pragmas = CorePragmas.Instanced;
                    depthOnly.pragmas = CorePragmas.Instanced;

                    return new SubShaderDescriptor()
                    {
                        //pipelineTag = BuiltInTarget.kPipelineTag,
                        customTags = BuiltInTarget.kUnlitMaterialTypeTag,
                        generatesPreview = true,
                        passes = new PassCollection
                        {
                            { unlit },
                            { shadowCaster },
                            { depthOnly },
                        },
                    };
                }
            }
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
