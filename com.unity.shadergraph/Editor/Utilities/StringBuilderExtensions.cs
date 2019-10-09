using System;
using System.Text;

namespace UnityEditor.ShaderGraph
{
    static class StringBuilderExtensions
    {
        public static void AppendIndentedLines(this StringBuilder sb, string lines, string indentation)
        {
            sb.EnsureCapacity(sb.Length + lines.Length);
            var charIndex = 0;
            while (charIndex < lines.Length)
            {
                var nextNewLineIndex = lines.IndexOf(Environment.NewLine, charIndex, StringComparison.Ordinal);
                if (nextNewLineIndex == -1)
                {
                    nextNewLineIndex = lines.Length;
                }

                sb.Append(indentation);

                for (var i = charIndex; i < nextNewLineIndex; i++)
                {
                    sb.Append(lines[i]);
                }

                sb.AppendLine();

                charIndex = nextNewLineIndex + Environment.NewLine.Length;
            }
        }
    }
}
