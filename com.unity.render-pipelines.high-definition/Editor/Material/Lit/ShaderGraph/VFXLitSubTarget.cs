using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class VFXLitSubTarget : HDLitSubTarget, IVFXCompatibleTarget
    {
        private VFXContext m_Context;
        private VFXContextCompiledData m_ContextData;

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("a4015296799c4bfd99499b48602f9e32");  // VFXLitSubTarget.cs
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;

        protected override bool supportRaytracing => false;
        protected override bool supportPathtracing => false;

        protected override string customInspector => "Rendering.HighDefinition.VFXShaderGraphGUI";

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            var baseSubShaderDescriptor = base.GetSubShaderDescriptor();

            /*
            var baseSubShaderDescriptor = new SubShaderDescriptor
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

                if (m_Context is VFXAbstractParticleOutput m_ContextHDRP)
                {
                    if (m_ContextHDRP.hasShadowCasting)
                    {
                        passes.Add(HDShaderPasses.GenerateShadowCaster(supportLighting));
                    }

                    if (m_ContextHDRP.hasMotionVector)
                    {
                        passes.Add(HDShaderPasses.GenerateMotionVectors(supportLighting, supportForward));
                    }
                }

                return passes;
            }*/

            // Use the current VFX context to configure the subshader.
            return VFXTargetUtility.PostProcessSubShaderVFX(baseSubShaderDescriptor, m_Context, m_ContextData);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            context.AddField(Fields.GraphVFX);
        }

        public bool TryConfigureVFX(VFXContext context, VFXContextCompiledData contextData)
        {
            m_Context = context as VFXAbstractParticleHDRPLitOutput;
            m_ContextData = contextData;
            return true;
        }
    }
}
