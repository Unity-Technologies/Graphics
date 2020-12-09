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
        }

        // See: VFXShaderWriter.TypeToUniformCode
        static readonly Dictionary<Type, Type> kVFXShaderPropertyMap = new Dictionary<Type, Type>
        {
            { typeof(float),     typeof(Vector1ShaderProperty) },
            { typeof(Vector2),   typeof(Vector2ShaderProperty) },
            { typeof(Vector3),   typeof(Vector3ShaderProperty) },
            { typeof(Vector4),   typeof(Vector4ShaderProperty) },
            { typeof(int),       typeof(Vector1ShaderProperty) },
            { typeof(uint),      typeof(Vector1ShaderProperty) },
            { typeof(Matrix4x4), typeof(Matrix4ShaderProperty) },
            { typeof(bool),      typeof(BooleanShaderProperty) },
        };

        static AbstractShaderProperty VFXExpressionToShaderProperty(VFXExpression expression, string name)
        {
            var type = VFXExpression.TypeToType(expression.valueType);

            if (!kVFXShaderPropertyMap.TryGetValue(type, out var shaderPropertyType))
                return null;

            // Must flag for non public here since all shader property constructors are internal.
            var property =  (AbstractShaderProperty)Activator.CreateInstance(shaderPropertyType, true);

            property.overrideReferenceName   = name;
            property.overrideHLSLDeclaration = true;
            property.hlslDeclarationOverride = HLSLDeclaration.VFX;

            return property;
        }

        static void CollectVFXShaderProperties(PropertyCollector collector, VFXContextCompiledData contextData)
        {
            // See: VFXShaderWriter.WriteCBuffer
            var mapper = contextData.uniformMapper;
            var uniformValues = mapper.uniforms
                .Where(e => !e.IsAny(VFXExpression.Flags.Constant | VFXExpression.Flags.InvalidOnCPU)) // Filter out constant expressions
                .OrderByDescending(e => VFXValue.TypeToSize(e.valueType));

            var uniformBlocks = new List<List<VFXExpression>>();
            foreach (var value in uniformValues)
            {
                var block = uniformBlocks.FirstOrDefault(b => b.Sum(e => VFXValue.TypeToSize(e.valueType)) + VFXValue.TypeToSize(value.valueType) <= 4);
                if (block != null)
                    block.Add(value);
                else
                    uniformBlocks.Add(new List<VFXExpression>() { value });
            }

            foreach (var block in uniformBlocks)
            {
                foreach (var value in block)
                {
                    string name = mapper.GetName(value);

                    //Reserved unity variable name (could be filled manually see : VFXCameraUpdate)
                    if (name.StartsWith("unity_"))
                        continue;

                    var property = VFXExpressionToShaderProperty(value, name);
                    collector.AddShaderProperty(property);
                }
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            CollectVFXShaderProperties(collector, m_ContextData);
        }

        public bool TryConfigureVFX(VFXContext context, VFXContextCompiledData contextData)
        {
            m_Context = context as VFXAbstractParticleHDRPLitOutput;
            m_ContextData = contextData;
            return true;
        }
    }
}
