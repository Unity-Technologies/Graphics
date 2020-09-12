using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderAnalysis
{
    public struct KeywordSet
    {
        HashSet<string> m_Values;

        public int Count => m_Values?.Count ?? 0;

        public KeywordSet(IEnumerable<string> values)
        {
            m_Values = new HashSet<string>();
            foreach (var value in values)
                m_Values.Add(value);
        }
        public bool Contains(string value) => m_Values?.Contains(value) ?? false;
        public void Add(string value) => m_Values?.Add(value);
        public bool IsSubsetOf(HashSet<string> entry) => m_Values?.IsSubsetOf(entry) ?? true;

        public static explicit operator HashSet<string>(in KeywordSet v) => v.m_Values;
    }

    public struct PassNameSet
    {
        HashSet<string> m_Values;

        public int Count => m_Values?.Count ?? 0;

        public PassNameSet(IEnumerable<string> values)
        {
            m_Values = new HashSet<string>();
            foreach (var value in values)
                m_Values.Add(value);
        }

        public bool Contains(string value) => m_Values?.Contains(value) ?? false;
        public void Add(string value) => m_Values?.Add(value);

        public static explicit operator HashSet<string>(in PassNameSet v) => v.m_Values;
    }

    public class ShaderProgramFilter
    {
        public PassNameSet includedPassNames;
        public PassNameSet excludedPassNames;
        /// <summary>If not empty, a variant is selected if it contains all keywords of one keyword set.</summary>
        public List<KeywordSet> includedKeywords = new List<KeywordSet>();

        public static ShaderProgramFilter Parse(string shaderPassFilter, string keywordFilter)
        {
            var result = new ShaderProgramFilter();

            bool include = false, exclude = false;
            var passNames = Array.Empty<string>();
            if (!string.IsNullOrEmpty(shaderPassFilter) && shaderPassFilter.Length > 0)
            {
                include = shaderPassFilter[0] == '+';
                exclude = shaderPassFilter[0] == '-';
                passNames = shaderPassFilter.Substring(1).Split(',');
            }
            result.includedPassNames = new PassNameSet(include ? passNames : Enumerable.Empty<string>());
            result.excludedPassNames = new PassNameSet(exclude ? passNames : Enumerable.Empty<string>());

            if (!string.IsNullOrEmpty(keywordFilter))
            {
                var ands = keywordFilter.Split('|');
                foreach (var and in ands)
                    result.includedKeywords.Add(new KeywordSet(and.Split('&')));
            }

            return result;
        }
    }
}
