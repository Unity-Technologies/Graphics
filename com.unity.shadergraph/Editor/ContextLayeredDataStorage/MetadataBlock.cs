using System;
using System.Collections.Generic;

namespace UnityEditor.ContextLayeredDataStorage
{
    internal class MetadataBlock : Dictionary<string, ValueType>
    {
        private struct DataBox<T>
        {
            public T data;
        }

        public bool HasMetadata(string lookup)
        {
            return ContainsKey(lookup);
        }

        public T GetMetadata<T>(string lookup)
        {
            return (this[lookup] as DataBox<T>?).Value.data;
        }

        public void SetMetadata<T>(string lookup, T data)
        {
            this[lookup] = new DataBox<T> { data = data };
        }


    }

    internal class MetadataCollection : Dictionary<string, MetadataBlock>
    {
        
    }
}
