using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface INodeWriter : IDisposable
    {
        public bool TryAddPort(string portKey, bool isInput, bool isHorizontal, out IPortWriter portWriter);

        public bool TryAddTransientNode<T>(string transientNodeKey, IFieldWriter<T> fieldWriter) ;

        public bool TryAddField<T>(string fieldKey, out IFieldWriter<T> fieldWriter);

        public bool TryAddField(string fieldKey, out IFieldWriter fieldWriter);

        public bool TryGetPort(string portKey, out IPortWriter portWriter);

        public bool TryGetField<T>(string fieldKey, out IFieldWriter<T> fieldWriter);

        public bool TryGetField(string fieldKey, out IFieldWriter fieldWriter);

        public IPortWriter GetPort(string portKey);

        public bool TryRemove();
    }

    public interface IPortWriter : IDisposable
    {
        public bool TryAddField<T>(string fieldKey, out IFieldWriter<T> fieldWriter);

        public bool TryAddField(string fieldKey, out IFieldWriter fieldWriter);

        public bool TryGetField<T>(string fieldKey, out IFieldWriter<T> fieldWriter);

        public bool TryGetField(string fieldKey, out IFieldWriter fieldWriter);

        public bool TryAddConnection(IPortWriter other);

        public bool TryRemove();
    }

    public interface IFieldWriter : IDisposable
    {
        public bool TryAddSubField(string subFieldKey, out IFieldWriter subFieldWriter);

        public bool TryAddSubField<T>(string subFieldKey, out IFieldWriter<T> subFieldWriter) ;

        public bool TryGetSubField(string subFieldKey, out IFieldWriter subFieldWriter);

        public bool TryGetSubField<T>(string subFieldKey, out IFieldWriter<T> subFieldWriter) ;

        public bool TryGetTypedFieldWriter<T>(out IFieldWriter<T> typedFieldWriter) ;

        public bool TryRemove();
    }

    public interface IFieldWriter<T> : IFieldWriter 
    {
        public bool TryWriteData(T data);
    }
}
