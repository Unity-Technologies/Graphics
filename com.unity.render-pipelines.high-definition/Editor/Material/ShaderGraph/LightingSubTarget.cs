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
    abstract class LightingSubTarget : SurfaceSubTarget, IRequiresData<LightingData>
    {
        LightingData m_LightingData;

        LightingData IRequiresData<LightingData>.data
        {
            get => m_LightingData;
            set => m_LightingData = value;
        }

        public LightingData lightingData
        {
            get => m_LightingData;
            set => m_LightingData = value;
        }

        protected override string renderType => HDRenderTypeTags.HDLitShader.ToString();

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("aea3df556ea7e9b44855d1fff79fed53")); // LightingSubTarget.cs
            base.Setup(ref context);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var gui = new SubTargetPropertiesGUI(context, onChange, registerUndo, systemData, builtinData, lightingData);
            AddInspectorPropertyBlocks(gui);
            context.Add(gui);
        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            int hash = base.ComputeMaterialNeedsUpdateHash();

            unchecked
            {
                hash = hash * 23 + builtinData.alphaTestShadow.GetHashCode();
                hash = hash * 23 + lightingData.receiveSSR.GetHashCode();
                hash = hash * 23 + lightingData.receiveSSRTransparent.GetHashCode();
            }

            return hash;
        }

        protected override bool supportLighting => true;
        // All lit sub targets are forward only except Lit so we set it as default here
        protected override bool supportForward => true;

        protected abstract bool requireSplitLighting { get; }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Common properties to all Lit master nodes
            var descs = context.blocks.Select(x => x.descriptor);

            // Normal dropoff space
            context.AddField(Fields.NormalDropOffOS,                        lightingData.normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(Fields.NormalDropOffTS,                        lightingData.normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(Fields.NormalDropOffWS,                        lightingData.normalDropOffSpace == NormalDropOffSpace.World);

            // Misc
            context.AddField(HDFields.BlendPreserveSpecular,                systemData.surfaceType != SurfaceType.Opaque && lightingData.blendPreserveSpecular);
            context.AddField(HDFields.DisableDecals,                        !lightingData.receiveDecals);
            context.AddField(HDFields.DisableSSR,                           !lightingData.receiveSSR);
            context.AddField(HDFields.DisableSSRTransparent,                !lightingData.receiveSSRTransparent);
            context.AddField(HDFields.SpecularAA,                           lightingData.specularAA &&
                                                                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) &&
                                                                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
            context.AddField(HDFields.LightingGI,                           descs.Contains(HDBlockFields.SurfaceDescription.BakedGI) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedGI));
            context.AddField(HDFields.BackLightingGI,                       descs.Contains(HDBlockFields.SurfaceDescription.BakedBackGI) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI));
            context.AddField(HDFields.BentNormal,                           descs.Contains(HDBlockFields.SurfaceDescription.BentNormal) && context.connectedBlocks.Contains(HDBlockFields.SurfaceDescription.BentNormal) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BentNormal));
            context.AddField(HDFields.AmbientOcclusion,                     context.blocks.Contains((BlockFields.SurfaceDescription.Occlusion, false)) && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.Occlusion));

            // Specular Occlusion Fields
            context.AddField(HDFields.SpecularOcclusionFromAO,              lightingData.specularOcclusionMode == SpecularOcclusionMode.FromAO);
            context.AddField(HDFields.SpecularOcclusionFromAOBentNormal,    lightingData.specularOcclusionMode == SpecularOcclusionMode.FromAOAndBentNormal);
            context.AddField(HDFields.SpecularOcclusionCustom,              lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom);

            // Double Sided
            context.AddField(HDFields.DoubleSidedFlip,                      systemData.doubleSidedMode == DoubleSidedMode.FlippedNormals && context.pass.referenceName != "SHADERPASS_MOTION_VECTORS");
            context.AddField(HDFields.DoubleSidedMirror,                    systemData.doubleSidedMode == DoubleSidedMode.MirroredNormals && context.pass.referenceName != "SHADERPASS_MOTION_VECTORS");
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            context.AddBlock(HDBlockFields.SurfaceDescription.BentNormal);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);

            // Specular AA
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance, lightingData.specularAA);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularAAThreshold,  lightingData.specularAA);

            // Baked GI
            context.AddBlock(HDBlockFields.SurfaceDescription.BakedGI,              lightingData.overrideBakedGI);
            context.AddBlock(HDBlockFields.SurfaceDescription.BakedBackGI,          lightingData.overrideBakedGI);

            // Misc
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularOcclusion,    lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom);

            // Normal dropoff space
            context.AddBlock(BlockFields.SurfaceDescription.NormalOS,               lightingData.normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS,               lightingData.normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS,               lightingData.normalDropOffSpace == NormalDropOffSpace.World);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            // Add all shader properties required by the inspector
            HDSubShaderUtilities.AddStencilShaderProperties(collector, systemData, lightingData, requireSplitLighting);
        }
    }
}
