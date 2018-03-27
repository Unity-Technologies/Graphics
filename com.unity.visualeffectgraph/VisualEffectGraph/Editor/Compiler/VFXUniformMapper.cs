using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

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
            if (!exp.IsAny(VFXExpression.Flags.NotCompilabeOnCPU))
            {
                string prefix;
                Dictionary<VFXExpression, string> expressions;

                if (VFXExpression.IsUniform(exp.valueType))
                {
                    if (!exp.Is(VFXExpression.Flags.Constant)) // Filter out constant uniform that should be patched directly in shader
                    {
                        prefix = "uniform_";
                        expressions = m_UniformToName;
                    }
                    else
                        return;
                }
                else if (VFXExpression.IsTexture(exp.valueType))
                {
                    prefix = "texture_";
                    expressions = m_TextureToName;
                }
                else
                {
                    if (VFXExpression.IsTypeValidOnGPU(exp.valueType))
                    {
                        throw new InvalidOperationException(string.Format("Missing handling for type: {0}", exp.valueType));
                    }
                    return;
                }

                if (expressions.ContainsKey(exp)) // Only need one name
                    return;

                name = name != null ? name : prefix + VFXCodeGeneratorHelper.GeneratePrefix((uint)expressions.Count());
                expressions[exp] = name;
            }
            else
                foreach (var parent in exp.parents)
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

        public string GetName(VFXExpression exp)    { return VFXExpression.IsTexture(exp.valueType) ? m_TextureToName[exp] : m_UniformToName[exp]; }

        public Dictionary<VFXExpression, string> expressionToName
        {
            get
            {
                return m_UniformToName.Union(m_TextureToName).ToDictionary(s => s.Key, s => s.Value);
            }
        }

        // This retrieves expression to name with additional type conversion where suitable
        public Dictionary<VFXExpression, string> expressionToCode
        {
            get
            {
                return m_UniformToName.Select(s => {
                        string code = null;
                        switch (s.Key.valueType)
                        {
                            case VFXValueType.Int32:
                                code = "asint(" + s.Value + ")";
                                break;
                            case VFXValueType.Uint32:
                                code = "asuint(" + s.Value + ")";
                                break;
                            case VFXValueType.Boolean:
                                code = "(bool)asuint(" + s.Value + ")";
                                break;
                            default:
                                code = s.Value;
                                break;
                        }

                        return new KeyValuePair<VFXExpression, string>(s.Key, code);
                    }).Union(m_TextureToName).ToDictionary(s => s.Key, s => s.Value);
            }
        }

        private Dictionary<VFXExpression, string> m_UniformToName;
        private Dictionary<VFXExpression, string> m_TextureToName;
    }
}
