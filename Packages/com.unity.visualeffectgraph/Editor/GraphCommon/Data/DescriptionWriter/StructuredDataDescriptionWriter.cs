using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    class StructuredDataDescriptionWriter : IDataDescriptionWriter<StructuredData>
    {
        private static readonly string StructuredBufferTypeDefine = "STRUCTURED_BUFFER_TYPE";

        public void WriteDescription(ShaderWriter shaderWriter, DataView dataView, StructuredData structuredData, string name, CompilationContext context)
        {
            shaderWriter.WriteLine($"struct {name}");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine("VFXByteAddressBuffer buffer;");
            shaderWriter.NewLine();

            var layoutCompilationData = context.data.Get<StructuredDataLayoutContainer>();
            layoutCompilationData.TryGetLayout(structuredData, out var layout);

            shaderWriter.WriteLine("void Init()");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine($"buffer.Init(_{name}_buffer, 0u, {layout.GetBufferSize()});");
            shaderWriter.CloseBlock();
            shaderWriter.NewLine();

            foreach (var subDataView in dataView.Children)
            {
                if (subDataView.DataDescription is not ValueData valueData)
                    continue;
                var offset = layout.GetValueOffset(valueData);
                var typeString = HlslCodeHelper.GetTypeName(valueData.Type);
                var subDataName  = subDataView.SubDataKey.ToString();
                DeclareMember(shaderWriter, typeString, subDataName, (uint)offset);
            }

            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
        }

        void DeclareMember(ShaderWriter shaderWriter, string type, string name, uint offset)
        {
            shaderWriter.WriteLine($"{type} Load_{name}()");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine($"{type} value;");
            shaderWriter.WriteLine($"buffer.LoadData(value, {offset}u);");
            shaderWriter.WriteLine("return value;");
            shaderWriter.CloseBlock();
            shaderWriter.NewLine();
            shaderWriter.WriteLine($"void Store_{name}({type} value)");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine($"buffer.StoreData(value, {offset}u);");
            shaderWriter.CloseBlock();
        }

        public bool WriteView(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView, DataView writtenDataView,
            string name, string sourceName, CompilationContext context)
        {
            shaderWriter.WriteLine($"struct {name}View");
            shaderWriter.OpenBlock();
            foreach (var subDataView in readDataView.Children)
            {
                var subData = subDataView.DataDescription;
                if (subData is ValueData valueData)
                {
                    var typeString = HlslCodeHelper.GetTypeName(valueData.Type);
                    shaderWriter.WriteLine($"{typeString} {subDataView.SubDataKey};");
                }
            }

            shaderWriter.WriteLine($"void Init({sourceName} buffer)");
            shaderWriter.OpenBlock();

            foreach (var subDataView in readDataView.Children)
            {
                var subData = subDataView.DataDescription;
                if (subData is ValueData)
                {
                    var memberName = subDataView.SubDataKey;
                    shaderWriter.WriteLine($"{memberName} = buffer.Load_{memberName}();");
                }
            }
            shaderWriter.CloseBlock();
            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
            shaderWriter.WriteLine($"{name}View {name};"); // TODO: probably externally only for the actual binding, not inner types
            return true;
        }

        public string GetSubdataName(IDataKey subDataKey)
        {
            return $".Load_{subDataKey}()";
        }

        public void DefineResourceUsage(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView,
            DataView writtenDataView)
        {
            if (writtenDataView.Id.IsValid)
            {
                shaderWriter.Define(StructuredBufferTypeDefine, "RWByteAddressBuffer");
                return;
            }

            shaderWriter.Define(StructuredBufferTypeDefine, "ByteAddressBuffer");
        }

        public void UndefineResourceUsage(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView,
            DataView writtenDataView)
        {
            if (usedDataView.Id.IsValid)
            {
                shaderWriter.Undefine(StructuredBufferTypeDefine);
            }
        }
        public IEnumerable<(string, string)> GetUsedResources(string name, DataView usedDataView)
        {
            yield return (StructuredBufferTypeDefine, $"_{name}_buffer");
        }
    }
}
