using System;

namespace UnityEditor.ShaderFoundry
{
    internal class ShaderFormatter
    {
        internal string Format(string data)
        {
            var builder = new ShaderBuilder();
            string[] lines = data.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
                ProcessLine(builder, line);

            return builder.ToString();
        }

        void ProcessLine(ShaderBuilder builder, string line)
        {
            // This attempts to emit lines at the correct indentation level while
            // doing minimal formatting, No newlines will be removed or inserted.

            var trimmedLine = line.Trim();

            // If the starts with an end brace then we need to emit the entire line de-indented and then skip the first character
            bool skipFirst = false;
            if(trimmedLine.StartsWith('}'))
            {
                skipFirst = true;
                builder.Deindent();
            }

            builder.AddLine(trimmedLine);

            foreach(var value in trimmedLine)
            {
                if (skipFirst)
                {
                    skipFirst = false;
                    continue;
                }
                if (value == '{')
                    builder.Indent();
                else if (value == '}')
                    builder.Deindent();
            }
        }
    }
}
