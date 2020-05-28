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
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    abstract class SurfaceSubTarget : HDSubTarget, IRequiresData<BuiltinData>
    {
        BuiltinData m_BuiltinData;

        // Interface Properties
        BuiltinData IRequiresData<BuiltinData>.data
        {
            get => m_BuiltinData;
            set => m_BuiltinData = value;
        }

        public BuiltinData builtinData
        {
            get => m_BuiltinData;
            set => m_BuiltinData = value;
        }

        protected override string renderQueue
        {
            get => HDRenderQueue.GetShaderTagValue(HDRenderQueue.ChangeType(systemData.renderingPass, systemData.sortPriority, systemData.alphaTest));
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Common properties between all "surface" master nodes (everything except decal right now)

            // Blend Mode
            context.AddField(Fields.BlendAdd,                       systemData.surfaceType != SurfaceType.Opaque && systemData.blendMode == BlendMode.Additive);
            context.AddField(Fields.BlendAlpha,                     systemData.surfaceType != SurfaceType.Opaque && systemData.blendMode == BlendMode.Alpha);
            context.AddField(Fields.BlendPremultiply,               systemData.surfaceType != SurfaceType.Opaque && systemData.blendMode == BlendMode.Premultiply);

            // We always generate the keyword ALPHATEST_ON
            context.AddField(Fields.AlphaTest,                      systemData.alphaTest
                && (context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold)
                    || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow)
                || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass)
                || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass)));

            // Double Sided
            context.AddField(HDFields.DoubleSided,                  systemData.doubleSidedMode != DoubleSidedMode.Disabled);

            context.AddField(HDFields.DoAlphaTestPrepass,           systemData.alphaTest && systemData.alphaTestDepthPrepass && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass));
            context.AddField(HDFields.DoAlphaTestPostpass,          systemData.alphaTest && systemData.alphaTestDepthPostpass && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass));
            context.AddField(HDFields.DoAlphaTestShadow,            systemData.alphaTest && builtinData.alphaTestShadow && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow));

            context.AddField(HDFields.TransparentDepthPrePass,      systemData.surfaceType != SurfaceType.Opaque && systemData.alphaTestDepthPrepass);
            context.AddField(HDFields.TransparentDepthPostPass,     systemData.surfaceType != SurfaceType.Opaque && systemData.alphaTestDepthPostpass);

            // Features & Misc
            context.AddField(Fields.LodCrossFade,                   systemData.supportLodCrossFade);
            context.AddField(Fields.VelocityPrecomputed,            builtinData.addPrecomputedVelocity);
            context.AddField(HDFields.TransparentWritesMotionVec,   systemData.surfaceType != SurfaceType.Opaque && builtinData.transparentWritesMotionVec);
            context.AddField(Fields.AlphaToMask,                    systemData.alphaTest && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) && builtinData.alphaToMask);
            context.AddField(HDFields.DepthOffset,                  builtinData.depthOffset && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.DepthOffset));
            context.AddField(HDFields.AlphaFog,                     systemData.surfaceType != SurfaceType.Opaque && builtinData.transparencyFog);
            context.AddField(HDFields.TransparentBackFace,          systemData.surfaceType != SurfaceType.Opaque && builtinData.backThenFrontRendering);
        }

        protected void AddDistortionFields(ref TargetFieldContext context)
        {
            // Distortion
            context.AddField(HDFields.DistortionDepthTest,                  builtinData.distortionDepthTest);
            context.AddField(HDFields.DistortionAdd,                        builtinData.distortionMode == DistortionMode.Add);
            context.AddField(HDFields.DistortionMultiply,                   builtinData.distortionMode == DistortionMode.Multiply);
            context.AddField(HDFields.DistortionReplace,                    builtinData.distortionMode == DistortionMode.Replace);
            context.AddField(HDFields.TransparentDistortion,                systemData.surfaceType != SurfaceType.Opaque && builtinData.distortion);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Common block between all "surface" master nodes
            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);

            // Surface
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, systemData.alphaTest);

            // Alpha Test
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass, systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && systemData.alphaTestDepthPrepass);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass, systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && systemData.alphaTestDepthPostpass);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow, systemData.alphaTest && builtinData.alphaTestShadow);

            // Misc
            context.AddBlock(HDBlockFields.SurfaceDescription.DepthOffset,          builtinData.depthOffset);
        }

        protected void AddDistortionBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(HDBlockFields.SurfaceDescription.Distortion,       systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);
            context.AddBlock(HDBlockFields.SurfaceDescription.DistortionBlur,   systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var gui = new SubTargetPropertiesGUI(context, onChange, registerUndo, systemData, builtinData, null);
            AddInspectorPropertyBlocks(gui);
            context.Add(gui);
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
                    value = true,
                    hidden = true,
                    overrideReferenceName = kAddPrecomputedVelocity,
                });
            }

            // Common properties for all "surface" master nodes
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, systemData.alphaTest, builtinData.alphaTestShadow);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, systemData.doubleSidedMode);

            // Add all shader properties required by the inspector
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                systemData.surfaceType,
                systemData.blendMode,
                systemData.sortPriority,
                builtinData.alphaToMask,
                systemData.zWrite,
                systemData.transparentCullMode,
                systemData.zTest,
                builtinData.backThenFrontRendering,
                builtinData.transparencyFog
            );
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            // Fixup the material settings:
            material.SetFloat(kSurfaceType, (int)systemData.surfaceType);
            material.SetFloat(kDoubleSidedNormalMode, (int)systemData.doubleSidedMode);
            material.SetFloat(kAlphaCutoffEnabled, systemData.alphaTest ? 1 : 0);
            material.SetFloat(kBlendMode, (int)systemData.blendMode);
            material.SetFloat(kEnableFogOnTransparent, builtinData.transparencyFog ? 1.0f : 0.0f);
            material.SetFloat(kZTestTransparent, (int)systemData.zTest);
            material.SetFloat(kTransparentCullMode, (int)systemData.transparentCullMode);
            material.SetFloat(kZWrite, systemData.zWrite ? 1.0f : 0.0f);

            // No sorting priority for shader graph preview
            material.renderQueue = (int)HDRenderQueue.ChangeType(systemData.renderingPass, offset: 0, alphaTest: systemData.alphaTest);

            HDLitGUI.SetupMaterialKeywordsAndPass(material);
        }
    }
}
