using System;

namespace UnityEditor.ContextLayeredDataStorage
{
    public class DataHeader 
    {
        public virtual T GetMetadata<T>(string lookup)
        {
            throw new NotImplementedException();
        }

        public virtual void SetMetadata<T>(string lookup, T data)
        {
            throw new NotImplementedException();
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
