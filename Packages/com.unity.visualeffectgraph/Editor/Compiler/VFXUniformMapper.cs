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
        public VFXUniformMapper(VFXExpressionMapper mapper, bool filterOutConstants, bool needsNameSuffixes)
        {
            m_FilterOutConstants = filterOutConstants;
            m_NeedsNameSuffixes = needsNameSuffixes;
            Init(mapper);
        }

        private void CollectAndAddUniforms(VFXExpression exp, IEnumerable<VFXExpressionMapper.Data> datas, HashSet<VFXExpression> processedExp)
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
                else if (VFXExpression.IsBufferOnGPU(exp.valueType))
                {
                    prefix = "buffer_";
                    expressions = m_BufferToName;
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

                if (datas == null)
                {
                    if (previousNames.Count == 0) // No need to generate a name if one was already generated
                        previousNames.Add(prefix + VFXCodeGeneratorHelper.GeneratePrefix(m_CurrentUniformIndex++));
                }
                else
                {
                    foreach (var data in datas)
                    {
                        m_NameCounts.TryGetValue(data.name, out uint count);
                        m_NameCounts[data.name] = count + 1u;
                        string name = data.id == -1 && (!VFXExpression.IsUniform(exp.valueType) || !m_NeedsNameSuffixes) ? data.name : $"{data.name}_{VFXCodeGeneratorHelper.GeneratePrefix(count)}";
                        if (!previousNames.Contains(name))
                            previousNames.Add(name);
                    }
                }
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
            m_BufferToName = new Dictionary<VFXExpression, List<string>>();
            m_NameCounts = new Dictionary<string, uint>();

            m_CurrentUniformIndex = 0;

            AppendMapper(mapper);
        }

        public void AppendMapper(VFXExpressionMapper mapper)
        {
            var processedExp = new HashSet<VFXExpression>();
            foreach (var exp in mapper.expressions)
            {
                processedExp.Clear();
                var initialData = mapper.GetData(exp);
                CollectAndAddUniforms(exp, initialData, processedExp);
            }
        }

        public IEnumerable<VFXExpression> uniforms { get { return m_UniformToName.Keys; } }
        public IEnumerable<VFXExpression> textures { get { return m_TextureToName.Keys; } }
        public IEnumerable<VFXExpression> buffers { get { return m_BufferToName.Keys; } }



        private Dictionary<VFXExpression, List<string>> GetDictionaryFromType(VFXExpression exp)
        {
            if (VFXExpression.IsTexture(exp.valueType))
                return m_TextureToName;

            if (VFXExpression.IsBufferOnGPU(exp.valueType))
                return m_BufferToName;

            return m_UniformToName;
        }

        public bool Contains(VFXExpression exp)
        {
            return GetDictionaryFromType(exp).ContainsKey(exp);
        }

        public List<string> GetNames(VFXExpression exp)
        {
            return GetDictionaryFromType(exp)[exp];
        }

        // Get only the first name of a uniform (For generated code, we collapse all uniforms using the same expression into a single one)
        public string GetName(VFXExpression exp)
        {
            return GetNames(exp).FirstOrDefault();
        }

        // This retrieves expression to name with additional type conversion where suitable
        public Dictionary<VFXExpression, string> expressionToCode
        {
            get
            {
                return m_UniformToName.Select(s =>
                {
                    string firstName = $"graphValues.{s.Value.First()}";
                    return new KeyValuePair<VFXExpression, string>(s.Key, firstName);
                })
                    .Union(m_TextureToName.Select(s => new KeyValuePair<VFXExpression, string>(s.Key, s.Value.First())))
                    .Union(m_BufferToName.Select(s => new KeyValuePair<VFXExpression, string>(s.Key, s.Value.First())))
                    .ToDictionary(s => s.Key, s => s.Value);
            }
        }

        public void OverrideUniformsNamesWithOther(VFXUniformMapper otherMapper)
        {
            var prevUniforms = uniforms.ToArray();
            foreach (var exp in prevUniforms)
            {
                m_UniformToName[exp] = otherMapper.GetNames(exp);
            }
        }

        private Dictionary<VFXExpression, List<string>> m_UniformToName;
        private Dictionary<VFXExpression, List<string>> m_TextureToName;
        private Dictionary<VFXExpression, List<string>> m_BufferToName;
        private Dictionary<string, uint> m_NameCounts;
        private uint m_CurrentUniformIndex;
        private bool m_FilterOutConstants;
        private bool m_NeedsNameSuffixes;
    }
}
