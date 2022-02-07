using System;

namespace UnityEditor.ContextLayeredDataStorage
{
    public interface IDataHeader
    {
        public IDataReader GetReader(Element element);
        public IDataWriter GetWriter(Element element);
        public void SetMetadata<T>(string lookup, T data);
        public T GetMetadata<T>(string lookup);
        public string ToJson();
        public void FromJson(string json);
    }


    internal class DefaultHeader : IDataHeader
    {
        public T GetMetadata<T>(string lookup)
        {
            throw new NotImplementedException();
        }

        public void SetMetadata<T>(string lookup, T data)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetReader(Element element)
        {
            return new ElementReader(element);
        }

        public IDataWriter GetWriter(Element element)
        {
            return new ElementWriter(element);
        }

        public string ToJson()
        {
            return "";
        }

        public void FromJson(string json)
        {

        }
    }



}
