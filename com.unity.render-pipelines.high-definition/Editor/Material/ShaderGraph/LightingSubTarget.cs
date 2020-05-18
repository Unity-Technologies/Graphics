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

        protected override string renderQueue
        {
            get
            {
                var renderingPass = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
                int queue = HDRenderQueue.ChangeType(renderingPass, systemData.sortPriority, systemData.alphaTest);
                return HDRenderQueue.GetShaderTagValue(queue);
            }
        }

        protected override string renderType => HDRenderTypeTags.HDLitShader.ToString();

        public LightingData lightingData
        {
            get => m_LightingData;
            set => m_LightingData = value;
        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            int hash = base.ComputeMaterialNeedsUpdateHash();
            // Be careful to not use a shift index used by the base function!
            hash |= (lightingData.alphaTestShadow ? 0 : 1) << 1;
            hash |= (lightingData.receiveSSR ? 0 : 1) << 2;
            hash |= (lightingData.subsurfaceScattering ? 0 : 1) << 3;
            return hash;
        }

        protected void AddLitMiscFields(ref TargetFieldContext context)
        {
            context.AddField(HDFields.BlendPreserveSpecular,                systemData.surfaceType != SurfaceType.Opaque && lightingData.blendPreserveSpecular);
            context.AddField(HDFields.DisableDecals,                        !lightingData.receiveDecals);
            context.AddField(HDFields.DisableSSR,                           !lightingData.receiveSSR);
            context.AddField(HDFields.SpecularAA,                           lightingData.specularAA &&
                                                                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) &&
                                                                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
            context.AddField(HDFields.BentNormal,                           context.blocks.Contains(HDBlockFields.SurfaceDescription.BentNormal) && context.connectedBlocks.Contains(HDBlockFields.SurfaceDescription.BentNormal) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BentNormal));
            context.AddField(HDFields.AmbientOcclusion,                     context.blocks.Contains(BlockFields.SurfaceDescription.Occlusion) && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.Occlusion));
            context.AddField(HDFields.LightingGI,                           context.blocks.Contains(HDBlockFields.SurfaceDescription.BakedGI) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedGI));
            context.AddField(HDFields.BackLightingGI,                       context.blocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.BakedBackGI));

            context.AddField(HDFields.TransparentBackFace,                  systemData.surfaceType != SurfaceType.Opaque && lightingData.backThenFrontRendering);
            context.AddField(HDFields.DoAlphaTestShadow,                    systemData.alphaTest && lightingData.alphaTestShadow && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow));
        }

        protected void AddNormalDropOffFields(ref TargetFieldContext context)
        {
            context.AddField(Fields.NormalDropOffOS,                        lightingData.normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(Fields.NormalDropOffTS,                        lightingData.normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(Fields.NormalDropOffWS,                        lightingData.normalDropOffSpace == NormalDropOffSpace.World);
        }

        protected void AddSpecularOcclusionFields(ref TargetFieldContext context)
        {
            context.AddField(HDFields.SpecularOcclusionFromAO,              lightingData.specularOcclusionMode == SpecularOcclusionMode.FromAO);
            context.AddField(HDFields.SpecularOcclusionFromAOBentNormal,    lightingData.specularOcclusionMode == SpecularOcclusionMode.FromAOAndBentNormal);
            context.AddField(HDFields.SpecularOcclusionCustom,              lightingData.specularOcclusionMode == SpecularOcclusionMode.Custom);
        }
    }
}