using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

using Util = UnityEditor.VFX.VFXShaderGraphGeneration;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class VFXLitSubTarget : HDLitSubTarget, IVFXCompatibleTarget
    {
        private VFXContext m_Context;
        private VFXContextCompiledData m_ContextData;

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("a4015296799c4bfd99499b48602f9e32");  // VFXLitSubTarget.cs
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/ShaderPassVFX.template";

        protected override bool supportRaytracing => false;
        protected override bool supportPathtracing => false;

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
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
            }

            // Use the current VFX context to configure the subshader.
            return PostProcessSubShaderVFX(baseSubShaderDescriptor, m_Context);
        }

        static SubShaderDescriptor PostProcessSubShaderVFX(SubShaderDescriptor subShaderDescriptor, VFXContext context)
        {
            var attributesStruct = Util.GenerateVFXAttributesStruct(context, Util.VFXAttributeType.Current);
            var sourceAttributesStruct = Util.GenerateVFXAttributesStruct(context, Util.VFXAttributeType.Source);

            var passes = subShaderDescriptor.passes.ToArray();
            PassCollection vfxPasses = new PassCollection();
            for (int i = 0; i < passes.Length; i++)
            {
                var passDescriptor = passes[i].descriptor;

                // Warning: Touching the structs field may require to manually append the default structs here.
                passDescriptor.structs = new StructCollection
                {
                    HDStructs.AttributesMesh, // TODO: Could probably re-use the original HD Attributes Mesh and just ensure Instancing enabled.
                    HDStructs.VaryingsMeshToPS,
                    Structs.SurfaceDescriptionInputs,
                    Structs.VertexDescriptionInputs,
                    attributesStruct,
                    sourceAttributesStruct
                };

                passDescriptor.pragmas = new PragmaCollection
                {
                    passDescriptor.pragmas,
                    Pragma.DebugSymbolsD3D
                };

                passDescriptor.defines = new DefineCollection
                {
                    passDescriptor.defines,
                };

                vfxPasses.Add(passDescriptor, passes[i].fieldConditions);
            }

            subShaderDescriptor.passes = vfxPasses;

            return subShaderDescriptor;
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            VFXShaderGraphGeneration.CollectVFXShaderProperties(collector, m_ContextData);
        }

        public bool TryConfigureVFX(VFXContext context, VFXContextCompiledData contextData)
        {
            m_Context = context as VFXAbstractParticleHDRPLitOutput;
            m_ContextData = contextData;
            return true;
        }
    }
}
