using UnityEditor.ShaderGraph;
using UnityEditor.VFX;

using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class VFXLitSubTarget : HDLitSubTarget, IVFXCompatibleTarget
    {
        private VFXAbstractParticleHDRPLitOutput m_Context;

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/ShaderPassVFX.template";

        protected override bool supportRaytracing => false;
        protected override bool supportPathtracing => false;

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            return new SubShaderDescriptor
            {
                generatesPreview = true,
                passes = GetPasses()
            };

            PassCollection GetPasses()
            {
                var passes = new PassCollection
                {
                    // TODO: Generate motion vectors, shadow pass if the context asks for it.
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

        public bool TryConfigureVFX(VFXContext context)
        {
            m_Context = context as VFXAbstractParticleHDRPLitOutput;

            return true;
        }
    }
}
