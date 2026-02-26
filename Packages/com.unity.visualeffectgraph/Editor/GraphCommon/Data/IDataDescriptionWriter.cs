using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    interface IDataDescriptionWriter
    {
        System.Type DataDescriptionType { get; }

        void WriteDescription(ShaderWriter writer, DataView dataView, string name, CompilationContext context);

        bool WriteView(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView, DataView writtenDataView, string name, string sourceName, CompilationContext context) => false;

        string GetSubdataName(IDataKey subDataKey) => throw new System.NotImplementedException();

        string GetSubdataTypeName(IDataKey subDataKey) => throw new System.NotImplementedException();

        public void DefineResourceUsage(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView, DataView writtenDataView) { }

        public void UndefineResourceUsage(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView, DataView writtenDataView) { }

        IEnumerable<(string, string)> GetUsedResources(string name, DataView usedDataView) => System.Array.Empty<(string, string)>();
    }

    interface IDataDescriptionWriter<T> : IDataDescriptionWriter where T : class, IDataDescription
    {
        System.Type IDataDescriptionWriter.DataDescriptionType => typeof(T);

        void IDataDescriptionWriter.WriteDescription(ShaderWriter writer, DataView dataView, string name, CompilationContext context) => WriteDescription(writer, dataView, dataView.DataDescription as T, name, context);
        void WriteDescription(ShaderWriter writer, DataView dataView, T dataDescription, string name, CompilationContext context);
    }
}
