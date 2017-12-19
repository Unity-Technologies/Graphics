using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace UnityEditor.ShaderGraph
{
    public class ShaderStringBuilder : IDisposable
    {
        enum ScopeType
        {
            Indent,
            Block
        }

        StringBuilder m_StringBuilder;
        Stack<ScopeType> m_ScopeStack;
        int m_IndentationLevel;
        const string k_IndentationString = "    ";

        public ShaderStringBuilder()
        {
            m_StringBuilder = new StringBuilder();
            m_ScopeStack = new Stack<ScopeType>();
        }

        public void AppendNewLine()
        {
            m_StringBuilder.AppendLine();
        }

        public void AppendLine(string value)
        {
            AppendIndentation();
            m_StringBuilder.Append(value);
            AppendNewLine();
        }

        [StringFormatMethod("formatString")]
        public void AppendLine(string formatString, params object[] args)
        {
            AppendIndentation();
            m_StringBuilder.AppendFormat(formatString, args);
            AppendNewLine();
        }

        public void AppendLines(string lines)
        {
            foreach (var line in Regex.Split(lines, Environment.NewLine))
                AppendLine(line);
        }

        public void Append(string value)
        {
            m_StringBuilder.Append(value);
        }

        [StringFormatMethod("formatString")]
        public void Append(string formatString, params object[] args)
        {
            m_StringBuilder.AppendFormat(formatString, args);
        }

        public void AppendIndentation()
        {
            for (var i = 0; i < m_IndentationLevel; i++)
                m_StringBuilder.Append(k_IndentationString);
        }

        public IDisposable IndentScope()
        {
            m_ScopeStack.Push(ScopeType.Indent);
            IncreaseIndent();
            return this;
        }

        public IDisposable BlockScope()
        {
            AppendLine("{");
            IncreaseIndent();
            m_ScopeStack.Push(ScopeType.Block);
            return this;
        }

        public void IncreaseIndent()
        {
            m_IndentationLevel++;
        }

        public void DecreaseIndent()
        {
            m_IndentationLevel--;
        }

        public void Dispose()
        {
            switch (m_ScopeStack.Pop())
            {
                case ScopeType.Indent:
                    DecreaseIndent();
                    break;
                case ScopeType.Block:
                    DecreaseIndent();
                    AppendLine("}");
                    break;
            }
        }

        public override string ToString()
        {
            return m_StringBuilder.ToString();
        }
    }
}
