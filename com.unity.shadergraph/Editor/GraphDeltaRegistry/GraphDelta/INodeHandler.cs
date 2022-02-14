using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface INodeHandler : IGraphDataHandler
    {
        public IEnumerable<IPortHandler> GetPorts();
        public IPortHandler GetPort(string localID);
        public IEnumerable<IFieldHandler> GetFields();
        public IFieldHandler GetField(string localID);
        public IFieldHandler<T> GetField<T>(string localID);
        public IPortHandler AddPort(string localID, bool isInput, bool isHorizontal);
        public void RemovePort(string localID);
        public IFieldHandler AddField(string localID);
        public IFieldHandler<T> AddField<T>(string localID, T value);
        public void RemoveField(string localID);
    }
}
