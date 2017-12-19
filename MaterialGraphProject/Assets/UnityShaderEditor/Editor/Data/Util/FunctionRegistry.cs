using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public class FunctionRegistry
    {
        Dictionary<string, string> m_Functions = new Dictionary<string, string>();
        ShaderStringBuilder m_StringBuilder = new ShaderStringBuilder();
        bool m_ValidationEnabled = false;

        public FunctionRegistry(int indentLevel = 0)
        {
            for (var i = 0; i < indentLevel; i++)
                m_StringBuilder.IncreaseIndent();
        }

        public bool ProvideFunction(string name, Action<ShaderStringBuilder> generator)
        {
            string functionSource = string.Empty;
            if (m_ValidationEnabled)
            {
                var ssb = new ShaderStringBuilder();
                generator(ssb);
                functionSource = ssb.ToString();
            }

            string existingFunctionSource;
            if (m_Functions.TryGetValue(name, out existingFunctionSource))
            {
                if (m_ValidationEnabled && functionSource != existingFunctionSource)
                    Debug.LogErrorFormat(@"Function `{0}` has varying implementations:{1}{1}{2}{1}{1}{3}", name, Environment.NewLine, functionSource, existingFunctionSource);
                return false;
            }
            generator(m_StringBuilder);
            m_Functions.Add(name, functionSource);
            m_StringBuilder.AppendNewLine();
            return true;
        }

        public override string ToString()
        {
            return m_StringBuilder.ToString();
        }
    }
}
