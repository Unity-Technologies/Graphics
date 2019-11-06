using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Serialization
{
    static class JsonAsset
    {
        public static List<RawJsonObject> Parse(string str)
        {
            var result = new List<RawJsonObject>();
            var separatorStr = $"{Environment.NewLine}{Environment.NewLine}";
            var startIndex = 0;
            const string headerBeginStr = "--- ";
            if (!str.StartsWith(headerBeginStr))
            {
                throw new InvalidOperationException("Expected '--- '");
            }

            while (startIndex < str.Length)
            {
                var idEnd = str.IndexOf(Environment.NewLine, startIndex, StringComparison.Ordinal);
                if (idEnd == -1)
                {
                    throw new InvalidOperationException("Expected new line");
                }

                var jsonBegin = idEnd + 1;
                var jsonEnd = str.IndexOf(separatorStr, jsonBegin, StringComparison.Ordinal);
                if (jsonEnd == -1)
                {
                    jsonEnd = str.Length;
                }

                var typeBegin = str.IndexOf(' ', startIndex, idEnd - startIndex) + 1;
                var typeEnd = str.IndexOf(' ', typeBegin, idEnd - typeBegin);
                var idBegin = typeEnd + 1;

                if (str.IndexOf(headerBeginStr, typeBegin - 4, 4, StringComparison.Ordinal) == -1)
                {
                    throw new InvalidOperationException("Expected '--- '");
                }

                var o = new RawJsonObject
                {
                    typeFullName = str.Substring(typeBegin, typeEnd - typeBegin),
                    id = str.Substring(idBegin, idEnd - idBegin),
                    json = str.Substring(jsonBegin, jsonEnd - jsonBegin)
                };
                result.Add(o);

                startIndex = jsonEnd + separatorStr.Length;
            }

            return result;
        }
    }

}
