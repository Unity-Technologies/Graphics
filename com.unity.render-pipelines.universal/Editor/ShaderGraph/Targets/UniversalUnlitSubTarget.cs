using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalUnlitSubTarget : SubTarget<UniversalTarget>, ILegacyTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("97c3f7dcb477ec842aa878573640313a"); // UniversalUnlitSubTarget.cs

        public UniversalUnlitSubTarget()
        {
            displayName = "Unlit";
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);

            // Process SubShaders
            SubShaderDescriptor[] subShaders = { SubShaders.Unlit, SubShaders.UnlitDOTS };
            for(int i = 0; i < subShaders.Length; i++)
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
            context.AddField(UniversalFields.SurfaceOpaque,       target.surfaceType == SurfaceType.Opaque);
            context.AddField(UniversalFields.SurfaceTransparent,  target.surfaceType != SurfaceType.Opaque);
            context.AddField(UniversalFields.BlendAdd,            target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Additive);
            context.AddField(Fields.BlendAlpha,                   target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Alpha);
            context.AddField(UniversalFields.BlendMultiply,       target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Multiply);
            context.AddField(UniversalFields.BlendPremultiply,    target.surfaceType != SurfaceType.Opaque && target.alphaMode == AlphaMode.Premultiply);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Alpha,              target.surfaceType == SurfaceType.Transparent || target.alphaClip);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, target.alphaClip);
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

            context.AddProperty("Alpha Clip", new Toggle() { value = target.alphaClip }, (evt) =>
            {
                if (Equals(target.alphaClip, evt.newValue))
                    return;

                registerUndo("Change Alpha Clip");
                target.alphaClip = evt.newValue;
                onChange();
            });

            context.AddProperty("Two Sided", new Toggle() { value = target.twoSided }, (evt) =>
            {
                if (Equals(target.twoSided, evt.newValue))
                    return;

                registerUndo("Change Two Sided");
                target.twoSided = evt.newValue;
                onChange();
            });
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if(!(masterNode is UnlitMasterNode1 unlitMasterNode))
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
                pipelineTag = UniversalTarget.kPipelineTag,
                customTags = UniversalTarget.kUnlitMaterialTypeTag,
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

                    unlit.pragmas = CorePragmas.DOTSForward;
                    shadowCaster.pragmas = CorePragmas.DOTSInstanced;
                    depthOnly.pragmas = CorePragmas.DOTSInstanced;

                    return new SubShaderDescriptor()
                    {
                        pipelineTag = UniversalTarget.kPipelineTag,
                        customTags = UniversalTarget.kUnlitMaterialTypeTag,
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
                useInPreview = true,

                // Template
                passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.Default,
                pragmas = CorePragmas.Forward,
                keywords = UnlitKeywords.Unlit,
                includes = UnlitIncludes.Unlit,
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
            };
        }
#endregion

#region Includes
        static class UnlitIncludes
        {
            const string kUnlitPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl";

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
