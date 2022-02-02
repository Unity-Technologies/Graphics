using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface IDataReader : IDisposable
    {
        public string GetName();

        public string GetFullPath();
    }

    public interface INodeReader : IDisposable, IDataReader
    {
        public IEnumerable<IPortReader> GetPorts();

        public IEnumerable<IPortReader> GetInputPorts();

        public IEnumerable<IPortReader> GetOutputPorts();

        public IEnumerable<IFieldReader> GetFields();

        public bool TryGetPort(string portKey, out IPortReader portReader);

        public bool TryGetField(string fieldKey, out IFieldReader fieldReader);
    }

    public interface IPortReader : IDisposable, IDataReader
    {
        public bool IsInput();

        public bool IsHorizontal();

        public INodeReader GetNode();

        public IEnumerable<IFieldReader> GetFields();

        public IEnumerable<IPortReader> GetConnectedPorts();

        public bool TryGetField(string fieldKey, out IFieldReader fieldReader, bool throughConnection = true);

    }

    public interface IFieldReader : IDisposable, IDataReader
    {
        public IEnumerable<IFieldReader> GetSubFields();
        public bool TryGetValue<T>(out T value);

        public bool TryGetSubField(string fieldKey, out IFieldReader fieldReader);
    }

    public interface IFieldReader<T> : IFieldReader where T : ISerializable
    {
        public ref readonly T GetValue();
    }

}
