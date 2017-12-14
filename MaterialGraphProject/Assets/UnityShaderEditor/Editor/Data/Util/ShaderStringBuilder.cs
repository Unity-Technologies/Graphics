using System;
using System.Collections.Generic;
using System.Text;
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
            m_StringBuilder.Append(Environment.NewLine);
        }

        public void AppendLine(string value)
        {
            AppendNewLine();
            AppendIndentation();
            m_StringBuilder.Append(value);
        }

        [StringFormatMethod("formatString")]
        public void AppendLine(string formatString, params object[] args)
        {
            AppendNewLine();
            AppendIndentation();
            m_StringBuilder.AppendFormat(formatString, args);
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
            m_IndentationLevel++;
            return this;
        }

        public IDisposable BlockScope()
        {
            AppendLine("{");
            m_IndentationLevel++;
            m_ScopeStack.Push(ScopeType.Block);
            return this;
        }

        public void Dispose()
        {
            switch (m_ScopeStack.Pop())
            {
                case ScopeType.Indent:
                    m_IndentationLevel--;
                    break;
                case ScopeType.Block:
                    m_IndentationLevel--;
                    AppendLine("}");
                    break;
            }
        }

        public override string ToString()
        {
            return m_StringBuilder.ToString();
        }

        public string ToString(int startIndex, int length)
        {
            return m_StringBuilder.ToString(startIndex, length);
        }
    }
}
