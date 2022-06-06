using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public struct ContextEntry
    {
        public string fieldName;
        internal GraphType.Precision precision;
        internal GraphType.Primitive primitive;
        public GraphType.Length length;
        public GraphType.Height height;
        public Matrix4x4 initialValue;
    }

    public interface IContextDescriptor : IRegistryEntry
    {
        IEnumerable<ContextEntry> GetEntries();
    }
}
