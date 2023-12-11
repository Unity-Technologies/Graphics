using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXSGInputs
    {
        public static readonly string kKeywordPrefix = "[ShaderKeyword]";

        private Dictionary<VFXExpression, string> m_Interpolators = new();
        private Dictionary<string, VFXExpression> m_FragInputs = new();
        private Dictionary<string, VFXExpression> m_VertInputs = new();
        private Dictionary<string, string> m_KeywordToDefine = new();

        public IEnumerable<KeyValuePair<VFXExpression, string>> interpolators => m_Interpolators;
        public IEnumerable<KeyValuePair<string, VFXExpression>> fragInputs => m_FragInputs;
        public IEnumerable<KeyValuePair<string, VFXExpression>> vertInputs => m_VertInputs;

        public IEnumerable<KeyValuePair<string, string>> keywordsToDefine => m_KeywordToDefine;

        public bool IsPredefinedKeyword(string keywordReference)
        {
            return m_KeywordToDefine.ContainsKey(keywordReference);
        }


        public bool IsInterpolant(VFXExpression exp)
        {
            return m_Interpolators.ContainsKey(exp);
        }

        public string GetInterpolantName(VFXExpression exp)
        {
            return m_Interpolators[exp];
        }

        public bool IsEmpty()
        {
            return m_Interpolators.Count == 0 && m_FragInputs.Count == 0 && m_VertInputs.Count == 0 && m_KeywordToDefine.Count == 0;
        }

        static int FindKeywordIndex(List<ShaderInput> shaderInputs, string keywordValue)
        {
            for (int index = 0; index < shaderInputs.Count; ++index)
            {
                var input = shaderInputs[index];
                if (input is ShaderKeyword shaderKeyword)
                {
                    if (shaderKeyword.keywordType == KeywordType.Boolean)
                    {
                        if (shaderKeyword.referenceName == keywordValue)
                            return index;
                    }
                    else if (shaderKeyword.keywordType == KeywordType.Enum)
                    {
                        foreach (var enumKeyword in shaderKeyword.entries)
                        {
                            if ((shaderKeyword.referenceName + "_" + enumKeyword.referenceName) == keywordValue)
                                return index;
                        }
                    }
                }
            }

            return -1;
        }


        public VFXSGInputs(VFXExpressionMapper cpuMapper, VFXExpressionMapper gpuMapper, VFXUniformMapper uniforms, ShaderGraphVfxAsset shaderGraph)
        {
            m_Interpolators.Clear();
            m_FragInputs.Clear();
            m_VertInputs.Clear();
            m_KeywordToDefine.Clear();

            VFXShaderGraphHelpers.GetShaderGraphParameter(shaderGraph, out var fragInputNames, out var vertInputNames);
            foreach (var inputName in vertInputNames)
            {
                var exp = gpuMapper.FromNameAndId(inputName, -1); // Postulate that inputs are only generated from context slots.
                if (exp == null)
                    throw new ArgumentException("Cannot find an expression matching the vertInput: " + inputName);

                m_VertInputs.Add(inputName, exp);
            }

            foreach(var inputName in fragInputNames)
            {
                var exp = gpuMapper.FromNameAndId(inputName, -1); // Postulate that inputs are only generated from context slots.
                if (exp == null)
                    throw new ArgumentException("Cannot find an expression matching the fragInput: " + inputName);

                m_FragInputs.Add(inputName, exp);
                if (!(exp.Is(VFXExpression.Flags.Constant) || uniforms.Contains(exp) || m_Interpolators.ContainsKey(exp))) // No interpolator needed for constants or uniforms
                    m_Interpolators.Add(exp, inputName);
            }

            foreach (var namedExpression in cpuMapper.CollectExpression(-1))
            {
                if (namedExpression.name.StartsWith(kKeywordPrefix, StringComparison.InvariantCulture))
                {
                    var keywordValue = namedExpression.name.Substring(kKeywordPrefix.Length);
                    if (namedExpression.exp.Is(VFXExpression.Flags.Constant))
                    {
                        var shaderInputIndex = FindKeywordIndex(shaderGraph.properties, keywordValue);
                        if (shaderInputIndex == -1)
                        {
                            Debug.LogErrorFormat("Unable to find matching keyword input for {0}", keywordValue);
                            continue;
                        }

                        var shaderInput = (ShaderKeyword)shaderGraph.properties[shaderInputIndex];
                        if (shaderInput == null)
                        {
                            Debug.LogErrorFormat("Unexpected shader input type at index: ", shaderInputIndex);
                            continue;
                        }

                        m_KeywordToDefine.TryAdd(shaderInput.referenceName, string.Empty);
                        var enabledKeyword = namedExpression.exp.Get<bool>();
                        if (enabledKeyword)
                        {
                            if (m_KeywordToDefine[shaderInput.referenceName] != String.Empty)
                                throw new InvalidOperationException($"Unexpected conflicting keyword values between {m_KeywordToDefine[shaderInput.referenceName]} vs. {keywordValue}");

                            m_KeywordToDefine[shaderInput.referenceName] = keywordValue;
                        }
                    }
                }
            }
        }
    }
}
