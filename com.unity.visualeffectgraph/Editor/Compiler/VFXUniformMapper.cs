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
        public VFXUniformMapper(VFXExpressionMapper mapper, bool filterOutConstants)
        {
            m_FilterOutConstants = filterOutConstants;
            Init(mapper);
        }

        private void CollectAndAddUniforms(VFXExpression exp, IEnumerable<string> names, HashSet<VFXExpression> processedExp)
        {
            if (!exp.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
            {
                string prefix;
                Dictionary<VFXExpression, List<string>> expressions;

                if (VFXExpression.IsUniform(exp.valueType))
                {
                    if (m_FilterOutConstants && exp.Is(VFXExpression.Flags.Constant)) // Filter out constant uniform that should be patched directly in shader
                        return;

                    prefix = "uniform_";
                    expressions = m_UniformToName;
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

                List<string> previousNames;
                expressions.TryGetValue(exp, out previousNames);

                if (previousNames == null)
                {
                    previousNames = new List<string>();
                    expressions[exp] = previousNames;
                }

                if (names == null) 
                {
                    if (previousNames.Count == 0) // No need to generate a name if one was already generated
                        previousNames.Add(prefix + VFXCodeGeneratorHelper.GeneratePrefix(m_CurrentUniformIndex++));
                }
                else
                    previousNames.AddRange(names);
            }
            else
            {
                foreach (var parent in exp.parents)
                {
                    if (processedExp.Contains(parent))
                        continue;

                    processedExp.Add(parent);
                    CollectAndAddUniforms(parent, null, processedExp);
                }
            }
        }

        private void Init(VFXExpressionMapper mapper)
        {
            m_UniformToName = new Dictionary<VFXExpression, List<string>>();
            m_TextureToName = new Dictionary<VFXExpression, List<string>>();

            m_CurrentUniformIndex = 0;

            var processedExp = new HashSet<VFXExpression>();
            foreach (var exp in mapper.expressions)
            {
                processedExp.Clear();
                var initialNames = mapper.GetData(exp).Select(d => d.fullName);
                CollectAndAddUniforms(exp, initialNames, processedExp);
            }
        }

        public IEnumerable<VFXExpression> uniforms { get { return m_UniformToName.Keys; } }
        public IEnumerable<VFXExpression> textures { get { return m_TextureToName.Keys; } }

        // Get only the first name of a uniform (For generated code, we collapse all uniforms using the same expression into a single one)
        public string GetName(VFXExpression exp)        { return VFXExpression.IsTexture(exp.valueType) ? m_TextureToName[exp].First() : m_UniformToName[exp].First(); }

        public List<string> GetNames(VFXExpression exp) { return VFXExpression.IsTexture(exp.valueType) ? m_TextureToName[exp] : m_UniformToName[exp]; }

        // This retrieves expression to name with additional type conversion where suitable
        public Dictionary<VFXExpression, string> expressionToCode
        {
            get
            {
                return m_UniformToName.Select(s => {
                    string code = null;
                    string firstName = s.Value.First();
                    switch (s.Key.valueType)
                    {
                        case VFXValueType.Int32:
                            code = "asint(" + firstName + ")";
                            break;
                        case VFXValueType.Uint32:
                            code = "asuint(" + firstName + ")";
                            break;
                        case VFXValueType.Boolean:
                            code = "(bool)asuint(" + firstName + ")";
                            break;
                        default:
                            code = firstName;
                            break;
                    }

                    return new KeyValuePair<VFXExpression, string>(s.Key, code);
                }).Union(m_TextureToName.Select(s => new KeyValuePair<VFXExpression, string>(s.Key, s.Value.First()))).ToDictionary(s => s.Key, s => s.Value);
            }
        }

        private Dictionary<VFXExpression, List<string>> m_UniformToName;
        private Dictionary<VFXExpression, List<string>> m_TextureToName;

        private uint m_CurrentUniformIndex;
        private bool m_FilterOutConstants;
    }
}
