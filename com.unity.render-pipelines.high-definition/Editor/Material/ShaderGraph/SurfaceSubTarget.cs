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
        
        protected void AddDistortionFields(ref TargetFieldContext context)
        {
            // Distortion
            context.AddField(HDFields.DistortionDepthTest,                  builtinData.distortionDepthTest);
            context.AddField(HDFields.DistortionAdd,                        builtinData.distortionMode == DistortionMode.Add);
            context.AddField(HDFields.DistortionMultiply,                   builtinData.distortionMode == DistortionMode.Multiply);
            context.AddField(HDFields.DistortionReplace,                    builtinData.distortionMode == DistortionMode.Replace);
            context.AddField(HDFields.TransparentDistortion,                systemData.surfaceType != SurfaceType.Opaque && builtinData.distortion);
        }

        protected void AddSurfaceMiscFields(ref TargetFieldContext context)
        {
            context.AddField(Fields.AlphaToMask,                            systemData.alphaTest && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) && builtinData.alphaToMask);
            context.AddField(HDFields.AlphaFog,                             systemData.surfaceType != SurfaceType.Opaque && builtinData.transparencyFog);
            context.AddField(Fields.VelocityPrecomputed,                    builtinData.addPrecomputedVelocity);
            context.AddField(HDFields.TransparentWritesMotionVec,           systemData.surfaceType != SurfaceType.Opaque && builtinData.transparentWritesMotionVec);
            context.AddField(HDFields.DepthOffset,                          builtinData.depthOffset && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.DepthOffset));
        }

        /// <summary>Add vertex inputs for standard surface shader</summary>
        protected void AddVertexBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);
        }

        protected void AddDistortionBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(HDBlockFields.SurfaceDescription.Distortion,       systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);
            context.AddBlock(HDBlockFields.SurfaceDescription.DistortionBlur,   systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);
        }

        protected void AddSurfaceBlocks(ref TargetActiveBlockContext context)
        {
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
    }
}