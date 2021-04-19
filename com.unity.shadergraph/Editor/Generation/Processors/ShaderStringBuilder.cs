using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEditor.Graphing;
using System.Globalization;

namespace UnityEditor.ShaderGraph
{
    struct ShaderStringMapping
    {
        public AbstractMaterialNode node { get; set; }
//        public List<AbstractMaterialNode> nodes { get; set; }
        public int startIndex { get; set; }
        public int count { get; set; }
    }

    class ShaderStringBuilder : IDisposable
    {
        enum ScopeType
        {
            Indent,
            Block,
            BlockSemicolon
        }

        StringBuilder m_StringBuilder;
        Stack<ScopeType> m_ScopeStack;
        int m_IndentationLevel;
        ShaderStringMapping m_CurrentMapping;
        List<ShaderStringMapping> m_Mappings;

        const string k_IndentationString = "    ";
        const string k_NewLineString = "\n";

        internal AbstractMaterialNode currentNode
        {
            get { return m_CurrentMapping.node; }
            set
            {
                m_CurrentMapping.count = m_StringBuilder.Length - m_CurrentMapping.startIndex;
                if (m_CurrentMapping.count > 0)
                    m_Mappings.Add(m_CurrentMapping);
                m_CurrentMapping.node = value;
                m_CurrentMapping.startIndex = m_StringBuilder.Length;
                m_CurrentMapping.count = 0;
            }
        }

        internal List<ShaderStringMapping> mappings
        {
            get { return m_Mappings; }
        }

        public ShaderStringBuilder()
        {
            m_StringBuilder = new StringBuilder();
            m_ScopeStack = new Stack<ScopeType>();
            m_Mappings = new List<ShaderStringMapping>();
            m_CurrentMapping = new ShaderStringMapping();
        }

        public ShaderStringBuilder(int indentationLevel)
            : this()
        {
            IncreaseIndent(indentationLevel);
        }

        public void AppendNewLine()
        {
            m_StringBuilder.Append(k_NewLineString);
        }

        public void AppendLine(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                AppendIndentation();
                m_StringBuilder.Append(value);
            }
            AppendNewLine();
        }

        [StringFormatMethod("formatString")]
        public void AppendLine(string formatString, params object[] args)
        {
            AppendIndentation();
            m_StringBuilder.AppendFormat(CultureInfo.InvariantCulture,formatString, args);
            AppendNewLine();
        }

        public void AppendLines(string lines)
        {
            if (string.IsNullOrEmpty(lines))
                return;
            var splitLines = lines.Split('\n');
            var lineCount = splitLines.Length;
            var lastLine = splitLines[lineCount - 1];
            if (string.IsNullOrEmpty(lastLine) || lastLine == "\r")
                lineCount--;
            for (var i = 0; i < lineCount; i++)
                AppendLine(splitLines[i].Trim('\r'));
        }

        public void Append(string value)
        {
            m_StringBuilder.Append(value);
        }

        public void Append(string value, int start, int count)
        {
            m_StringBuilder.Append(value, start, count);
        }

        [StringFormatMethod("formatString")]
        public void Append(string formatString, params object[] args)
        {
            m_StringBuilder.AppendFormat(formatString, args);
        }

        public void AppendSpaces(int count)
        {
            m_StringBuilder.Append(' ', count);
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

        public IDisposable BlockSemicolonScope()
        {
            AppendLine("{");
            IncreaseIndent();
            m_ScopeStack.Push(ScopeType.BlockSemicolon);
            return this;
        }

        public void IncreaseIndent()
        {
            m_IndentationLevel++;
        }

        public void IncreaseIndent(int level)
        {
            for (var i = 0; i < level; i++)
                IncreaseIndent();
        }

        public void DecreaseIndent()
        {
            m_IndentationLevel--;
        }

        public void DecreaseIndent(int level)
        {
            for (var i = 0; i < level; i++)
                DecreaseIndent();
        }

        public void Dispose()
        {
            if(m_ScopeStack.Count == 0)
                return;

            switch (m_ScopeStack.Pop())
            {
                case ScopeType.Indent:
                    DecreaseIndent();
                    break;
                case ScopeType.Block:
                    DecreaseIndent();
                    AppendLine("}");
                    break;
                case ScopeType.BlockSemicolon:
                    DecreaseIndent();
                    AppendLine("};");
                    break;
            }
        }

        public void Concat(ShaderStringBuilder other)
        {
            // First re-add all the mappings from `other`, such that their mappings are transformed.
            foreach (var mapping in other.m_Mappings)
            {
                currentNode = mapping.node;

                // Use `AppendLines` to indent according to the current indentation.
                AppendLines(other.ToString(mapping.startIndex, mapping.count));
            }
            currentNode = other.currentNode;
            AppendLines(other.ToString(other.m_CurrentMapping.startIndex, other.length - other.m_CurrentMapping.startIndex));
        }

        public void ReplaceInCurrentMapping(string oldValue, string newValue)
        {
            int start = m_CurrentMapping.startIndex;
            int end = m_StringBuilder.Length - start;
            m_StringBuilder.Replace(oldValue, newValue, start, end );
        }

        public string ToCodeBlock()
        {
            // Remove new line
            if(m_StringBuilder.Length > 0)
                m_StringBuilder.Length = m_StringBuilder.Length - 1;

            // Set indentations
            m_StringBuilder.Replace(Environment.NewLine, Environment.NewLine + k_IndentationString);

            return m_StringBuilder.ToString();
        }

        public override string ToString()
        {
            return m_StringBuilder.ToString();
        }

        public string ToString(int startIndex, int length)
        {
            return m_StringBuilder.ToString(startIndex, length);
        }

        internal void Clear()
        {
            m_StringBuilder.Length = 0;
        }

        internal int length
        {
            get { return m_StringBuilder.Length; }
            set { m_StringBuilder.Length = value; }
        }
    }
}
