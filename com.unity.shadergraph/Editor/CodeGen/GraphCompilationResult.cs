using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    // Required for Unity to handle nested array serialization
    [Serializable]
    struct IntArray
    {
        public int[] array;

        public int this[int i]
        {
            get => array[i];
            set => array[i] = value;
        }

        public static implicit operator IntArray(int[] array)
        {
            return new IntArray { array = array };
        }

        public static implicit operator int[](IntArray array)
        {
            return array.array;
        }
    }

    [Serializable]
    class GraphCompilationResult
    {
        public string[] codeSnippets;

        public int[] sharedCodeIndices;

        public IntArray[] outputCodeIndices;

        public string GenerateCode(int[] outputIndices)
        {
            var codeIndexSet = new HashSet<int>();

            foreach (var codeIndex in sharedCodeIndices)
            {
                codeIndexSet.Add(codeIndex);
            }

            foreach (var outputIndex in outputIndices)
            {
                foreach (var codeIndex in outputCodeIndices[outputIndex].array)
                {
                    codeIndexSet.Add(codeIndex);
                }
            }

            var codeIndices = new int[codeIndexSet.Count];
            codeIndexSet.CopyTo(codeIndices);
            Array.Sort(codeIndices);

            var charCount = 0;
            foreach (var codeIndex in codeIndices)
            {
                charCount += codeSnippets[codeIndex].Length;
            }

            var sb = new StringBuilder();
            sb.EnsureCapacity(charCount);

            foreach (var codeIndex in codeIndices)
            {
                sb.Append(codeSnippets[codeIndex]);
            }

            return sb.ToString();
        }
    }
}
