using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class PortHandler : GraphDataHandler
    {
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
        public bool IsInput { get; }
        public bool IsHorizontal { get; }

    }
}

