using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class NodeHandler : GraphDataHandler
    {
        internal NodeHandler(ElementID elementID, GraphStorage owner)
            : base(elementID, owner)
        {
        }

        public IEnumerable<PortHandler> GetPorts()
        {
            throw new System.Exception();
        }
        public PortHandler GetPort(string localID)
        {
            throw new System.Exception();
        }
        public IEnumerable<FieldHandler> GetFields()
        {
            throw new System.Exception();
        }
        public FieldHandler GetField(string localID)
        {
            throw new System.Exception();
        }
        public FieldHandler<T> GetField<T>(string localID)
        {
            throw new System.Exception();
        }
        public PortHandler AddPort(string localID, bool isInput, bool isHorizontal)
        {
            throw new System.Exception();
        }
        public void RemovePort(string localID)
        {
            throw new System.Exception();
        }
        public FieldHandler AddField(string localID)
        {
            throw new System.Exception();
        }
        public FieldHandler<T> AddField<T>(string localID, T value)
        {
            throw new System.Exception();
        }
        public void RemoveField(string localID)
        {
            throw new System.Exception();
        }
    }
}
