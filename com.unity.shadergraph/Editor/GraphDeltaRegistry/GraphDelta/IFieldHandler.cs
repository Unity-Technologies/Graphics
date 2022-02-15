using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface IFieldHandler : IGraphDataHandler
    {
        public IEnumerable<IFieldHandler> GetSubFields();
        public IFieldHandler GetSubField(string localID);
        public IFieldHandler<T> GetSubField<T>(string localID);
        public IFieldHandler AddSubField(string localID);
        public IFieldHandler<T> AddSubField<T>(string localID, T value);
        public IFieldHandler<T> AddSubField<T>(string layer, string localID, T value);
        public void RemoveSubField(string localID);
    }

    public interface IFieldHandler<T> : IFieldHandler
    {
        public T GetData();
        public void SetData(T value);
    }
}

