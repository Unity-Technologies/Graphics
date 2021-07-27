using System;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface IPortRef : IDisposable
    {
        public bool IsInput { get; }
        public void Remove();
    }
}

