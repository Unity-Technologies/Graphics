using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    [Serializable]
    [ScriptedProvider]
    internal class ExpressionProvider : IProvider<IShaderFunction>
    {
        internal const string kExpressionProviderKey = "unity:shadergraph:scripted:Expression";
        public string ProviderKey => kExpressionProviderKey;
        public GUID AssetID => default;
        public bool RequiresGeneration => true;
        private static readonly string[] kNamespace = { "unity", "shadergraph", "expression" };

        internal string Expression => m_expression;
        internal string ShaderType => m_type;

        public IShaderFunction Definition
        {
            get
            {
                if (m_definition == null || !m_definition.IsValid)
                {
                    UpdateExpression(m_name, m_expression, m_type);
                }
                return m_definition;
            }
        }

        [SerializeField]
        string m_name;

        [SerializeField]
        string m_expression;

        [SerializeField]
        string m_type;

        [NonSerialized]
        IShaderFunction m_definition;

        internal ExpressionProvider() : this("Expression", "", "float") { }

        internal ExpressionProvider(string name, string expression, string type)
        {
            UpdateExpression(name, expression, type);
        }

        public void Reload()
        {
            m_definition = ExpressionToShaderFunction(m_name, m_expression, m_type, out _);
        }

        static IReadOnlyDictionary<string, string> StandardHints = new Dictionary<string, string>()
        {
            { Hints.Common.kDisplayName, "Expression" },
            { Hints.Func.kProviderKey,  kExpressionProviderKey },
            { Hints.Func.kSearchName, "Expression" },
            { Hints.Func.kCategory, "Utility" },
            { Hints.Func.kSearchTerms, "equation, calculation, inline, code" }
        };

        internal void UpdateExpression(string name, string expression, string type)
        {
            m_name = name;
            m_type = type;
            m_expression = expression;
            Reload();
        }

        public IProvider Clone()
            => new ExpressionProvider(m_name, m_expression, m_type);

        internal static IShaderFunction ExpressionToShaderFunction(string name, string expression, string type, out string finalExpression)
        {
            // don't create fields from comments
            const string kCommentRegex = @"(\/\*.*?\*\/)|(\/\/.*)";

            // pattern for getting valid identifiers that aren't functions or members,
            // included expanded european character set.
            const string kIdentifierRegex = @"(?<!\.)\b[a-zA-ZŽžÀ-ÿ_][a-zA-ZŽžÀ-ÿ0-9_]*\b(?!\s*[\(])";

            List<string> orderedNames = new();
            HashSet<string> usedNames = new();

            // gather the list of identifiers, deduplicate them, and ignore if they are reserved keywords.
            string HandleName(string name)
            {
                if (!usedNames.Contains(name) && !IsReserved(name))
                {
                    usedNames.Add(name);
                    orderedNames.Add(name);
                }
                return name;
            }

            // clean out comments.
            expression = Regex.Replace(expression, kCommentRegex, "");

            // process identifiers.
            expression = Regex.Replace(expression, kIdentifierRegex, e => HandleName(e.Value));

            finalExpression = expression;

            // default initialize if there is no expression left.
            if (string.IsNullOrWhiteSpace(expression))
                expression = $"({type})0";

            IShaderType shaderType = new ShaderType(type);

            List<IShaderField> parameters = new();
            foreach (var paramName in orderedNames)
                parameters.Add(new ShaderField(paramName, true, false, shaderType, null));

            // trust that the name and type is valid.
            return new ShaderFunction(name, kNamespace, parameters, shaderType, $"return {expression};", StandardHints);
        }

        private static bool IsReserved(string name)
        {
            return NodeUtils.IsShaderLabKeyWord(name) || NodeUtils.IsShaderGraphKeyWord(name) || NodeUtils.IsHLSLKeyword(name);
        }
    }
}
