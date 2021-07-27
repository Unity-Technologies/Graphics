using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface INodeRef : IDisposable
    {
        public IPortRef AddInputPort(string portID);

        public IPortRef AddOutputPort(string portID);

        public IPortRef GetInputPort(string portID);

        public IPortRef GetOutputPort(string portID);

        public IEnumerable<IPortRef> GetInputPorts();

        public IEnumerable<IPortRef> GetOutputPorts();

        public void Remove();
    }
}
