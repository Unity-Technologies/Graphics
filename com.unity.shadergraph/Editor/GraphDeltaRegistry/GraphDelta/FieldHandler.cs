using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class FieldHandler : GraphDataHandler
    {
        public IEnumerable<FieldHandler> GetSubFields()
        {
            throw new System.Exception();
        }
        public FieldHandler GetSubField(string localID)
        {
            throw new System.Exception();
        }
        public FieldHandler<T> GetSubField<T>(string localID)
        {
            throw new System.Exception();
        }
        public FieldHandler AddSubField(string localID)
        {
            throw new System.Exception();
        }
        public FieldHandler<T> AddSubField<T>(string localID, T value)
        {
            throw new System.Exception();
        }
        public FieldHandler<T> AddSubField<T>(string layer, string localID, T value)
        {
            throw new System.Exception();
        }
        public void RemoveSubField(string localID)
        {
            throw new System.Exception();
        }
    }

    public class FieldHandler<T> : FieldHandler
    {
        public T GetData()
        {
            throw new System.Exception();
        }
        public void SetData(T value)
        {
            throw new System.Exception();
        }
    }
}

