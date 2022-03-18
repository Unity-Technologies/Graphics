using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface IContextDescriptor : IRegistryEntry
    {
        public struct ContextEntry
        {
            public string fieldName;
            internal GraphType.Precision precision;
            internal GraphType.Primitive primitive;
            public GraphType.Length length;
            public GraphType.Height height;
            public Matrix4x4 initialValue;
            public string interpolationSemantic;
            public bool isFlat;
        }
        IReadOnlyCollection<ContextEntry> GetEntries();
    }
}
