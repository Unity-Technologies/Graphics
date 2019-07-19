using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class PortMetadata
    {
        public string displayName { get; }
        public string referenceName { get; }
    }

    [Serializable]
    class GraphCompilationResult
    {
        public PortMetadata[] ports;

        public string[] codeSnippets;

        public int bodyCodeStartIndex;

        public int[][] portCodeIndices;

        public string[] globalCodes;

        public string[] bodyCodes;

        public int[][] portBodyCodeIndices;

        public int[][] portGlobalCodeIndices;

        public string GenerateCode(string name, int[] portIndices)
        {
            var codeIndexSet = new HashSet<int>();
            var globalCodeIndexSet = new HashSet<int>();
            var bodyCodeIndexSet = new HashSet<int>();

            foreach (var portIndex in portIndices)
            {
                foreach (var index in portGlobalCodeIndices[portIndex])
                {
                    globalCodeIndexSet.Add(index);
                }
                foreach (var index in portBodyCodeIndices[portIndex])
                {
                    bodyCodeIndexSet.Add(index);
                }
            }

            var globalCodeIndices = new int[globalCodeIndexSet.Count];
            globalCodeIndexSet.CopyTo(globalCodeIndices);
            Array.Sort(globalCodeIndices);

            var bodyCodeIndices = new int[bodyCodeIndexSet.Count];
            bodyCodeIndexSet.CopyTo(bodyCodeIndices);
            Array.Sort(bodyCodeIndices);

            var sb = new StringBuilder();

            foreach (var globalCodeIndex in globalCodeIndices)
            {
                sb.Append(globalCodes[globalCodeIndex]);
                sb.AppendLine();
            }

            sb.AppendLine($"struct {name}Input");
            sb.AppendLine("{");
            // TODO: Generate input struct
            sb.AppendLine("};");

            sb.AppendLine();

            sb.AppendLine($"struct {name}Output");
            sb.AppendLine("{");
            // TODO: Generate output struct
            sb.AppendLine("};");

            sb.AppendLine();

            sb.AppendLine("ShaderGraphOutput EvaluateShaderGraph(ShaderGraphInput IN)");
            sb.AppendLine("{");

            var isFirst = true;
            foreach (var bodyCodeIndex in bodyCodeIndices)
            {
                if (!isFirst)
                {
                    sb.AppendLine();
                }

                isFirst = false;
                var code = bodyCodes[bodyCodeIndex];
                sb.Append(code);
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
