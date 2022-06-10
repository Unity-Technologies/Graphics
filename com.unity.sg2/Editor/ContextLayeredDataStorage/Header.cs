using System;
using System.Collections.Generic;

namespace UnityEditor.ContextLayeredDataStorage
{
    public class DataHeader 
    {
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

        public virtual DataHeader MakeCopy()
        {
            return new DataHeader();
        }
    }
}
