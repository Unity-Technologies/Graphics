using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface IPortHandler : IGraphDataHandler
    {
        public IEnumerable<IFieldHandler> GetFields();
        public IFieldHandler GetField(string localID);
        public IFieldHandler<T> GetField<T>(string localID);
        public IFieldHandler AddField(string localID);
        public IFieldHandler<T> AddField<T>(string localID, T value);
        public void RemoveField(string localID);
        public bool IsInput { get; }
        public bool IsHorizontal { get; }

    }
}

