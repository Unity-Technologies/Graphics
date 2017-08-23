using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXUniformMapper
    {
        public VFXUniformMapper(VFXExpressionMapper mapper)
        {
            Init(mapper);
        }

        private void CollectAndAddUniforms(VFXExpression exp, string name)
        {
            if (!exp.Is(VFXExpression.Flags.PerElement))
            {
                if (exp.Is(VFXExpression.Flags.InvalidOnCPU))
                    throw new InvalidOperationException(string.Format("Collected uniform expression is invalid on CPU: {0}", exp));

                string prefix;
                Dictionary<VFXExpression, string> expressions;

                if (VFXExpression.IsUniform(exp.ValueType))
                {
                    if (!exp.Is(VFXExpression.Flags.Constant)) // Filter out constant uniform that should be patched directly in shader
                    {
                        prefix = "uniform_";
                        expressions = m_UniformToName;
                    }
                    else
                        return;
                }
                else if (VFXExpression.IsTexture(exp.ValueType))
                {
                    prefix = "texture_";
                    expressions = m_TextureToName;
                }
                else
                {
                    if (VFXExpression.IsTypeValidOnGPU(exp.ValueType))
                    {
                        throw new InvalidOperationException(string.Format("Missing handling for type: {0}", exp.ValueType));
                    }
                    return;
                }

                if (expressions.ContainsKey(exp)) // Only need one name
                    return;

                name = prefix + (name != null ? name : VFXCodeGeneratorHelper.GeneratePrefix((uint)expressions.Count()));
                expressions[exp] = name;
            }
            else
                foreach (var parent in exp.Parents)
                    CollectAndAddUniforms(parent, null);
        }

        private void Init(VFXExpressionMapper mapper)
        {
            m_UniformToName = new Dictionary<VFXExpression, string>();
            m_TextureToName = new Dictionary<VFXExpression, string>();

            foreach (var exp in mapper.expressions)
                CollectAndAddUniforms(exp, mapper.GetData(exp).First().fullName);
        }

        public IEnumerable<VFXExpression> uniforms { get { return m_UniformToName.Keys; } }
        public IEnumerable<VFXExpression> textures { get { return m_TextureToName.Keys; } }

        public string GetName(VFXExpression exp)    { return VFXExpression.IsTexture(exp.ValueType) ? m_TextureToName[exp] : m_UniformToName[exp]; }

        public Dictionary<VFXExpression, string> expressionToName
        {
            get
            {
                return m_UniformToName.Union(m_TextureToName).ToDictionary(s => s.Key, s => s.Value);
            }
        }

        private Dictionary<VFXExpression, string> m_UniformToName;
        private Dictionary<VFXExpression, string> m_TextureToName;
    }
}
