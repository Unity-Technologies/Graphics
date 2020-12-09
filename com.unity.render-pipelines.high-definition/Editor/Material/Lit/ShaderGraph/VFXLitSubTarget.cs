using System;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class VFXLitSubTarget : HDLitSubTarget, IVFXCompatibleTarget
    {
        private VFXAbstractParticleHDRPLitOutput m_Context;

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("a4015296799c4bfd99499b48602f9e32");  // VFXLitSubTarget.cs
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/ShaderPassVFX.template";

        protected override bool supportRaytracing => false;
        protected override bool supportPathtracing => false;

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            return new SubShaderDescriptor
            {
                generatesPreview = false,
                passes = GetPasses()
            };

            PassCollection GetPasses()
            {
                // TODO: Use the VFX context to configure the passes

                var passes = new PassCollection
                {
                    HDShaderPasses.GenerateSceneSelection(supportLighting),
                    HDShaderPasses.GenerateLitDepthOnly(),
                    HDShaderPasses.GenerateGBuffer(),
                    HDShaderPasses.GenerateLitForward()
                };

                if (m_Context.hasShadowCasting)
                {
                    passes.Add(HDShaderPasses.GenerateShadowCaster(supportLighting));
                }

                if (m_Context.hasMotionVector)
                {
                    passes.Add(HDShaderPasses.GenerateMotionVectors(supportLighting, supportForward));
                }

                return passes;
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            // Add the VFX properties
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                overrideReferenceName = "Test",
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.VFX
            });
        }

        public bool TryConfigureVFX(VFXContext context)
        {
            m_Context = context as VFXAbstractParticleHDRPLitOutput;

            return true;
        }
    }
}
