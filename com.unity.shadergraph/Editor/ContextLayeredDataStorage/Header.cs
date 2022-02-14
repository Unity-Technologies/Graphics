using System;
using System.Collections.Generic;

namespace UnityEditor.ContextLayeredDataStorage
{
    public class DataHeader 
    {
        private struct DataBox<T>
        {
            public T data;
        }

        private Dictionary<string, ValueType> m_data;

        public bool TryGetMetaData<T>(string lookup, out T data)
        {
            try
            {
                data = GetMetadata<T>(lookup);
                return true;
            }
            catch
            {
                data = default;
                return false;
            }

        }

        public virtual bool HasMetadata(string lookup)
        {
            return m_data.ContainsKey(lookup);
        }

        public virtual T GetMetadata<T>(string lookup)
        {
            return (m_data[lookup] as DataBox<T>?).Value.data;
        }

        public virtual void SetMetadata<T>(string lookup, T data)
        {
            m_data[lookup] = new DataBox<T> { data = data };
        }

        public virtual DataReader GetReader(Element element)
        {
            return new DataReader(element);
        }

        public virtual DataWriter GetWriter(Element element)
        {
            return new DataWriter(element);
        }

        public virtual string ToJson()
        {
            return "";
        }

        public virtual void FromJson(string json)
        {

        }
    }
}
