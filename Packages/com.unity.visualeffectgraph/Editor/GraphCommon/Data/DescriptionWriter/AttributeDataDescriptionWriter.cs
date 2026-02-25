
namespace Unity.GraphCommon.LowLevel.Editor
{
    class AttributeDataDescriptionWriter : IDataDescriptionWriter<AttributeData>
    {
        public void WriteDescription(ShaderWriter shaderWriter, DataView dataView, AttributeData attributeData, string name, CompilationContext context)
        {
            shaderWriter.IncludeFile("Packages/com.unity.vfxgraph/Shaders/Data/AttributeBuffer.hlsl");

            shaderWriter.NewLine();
            shaderWriter.WriteLine($"struct {name}");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine("VFXAttributeBuffer attributeBuffer;");
            shaderWriter.NewLine();

            var layoutCompilationData = context.data.Get<AttributeSetLayoutCompilationData>();
            var layout = layoutCompilationData[attributeData];
            foreach (var attributeLocation in layout)
            {
                var attribute = attributeLocation.Item1;
                var offset = attributeLocation.Item2;
                var stride = attributeLocation.Item3;
                shaderWriter.WriteLine($"VFX_ATTRIBUTE_DECLARE({HlslCodeHelper.GetTypeName(attribute.Type)}, {attribute.Name}, {HlslCodeHelper.GetValueString(attribute.DefaultValue)}, {offset}u, {stride}u);");
            }
            shaderWriter.NewLine();

            foreach (var attributeView in dataView.Children)
            {
                var attributeKey = attributeView.SubDataKey as AttributeKey;
                var attribute = attributeKey.Attribute;
                if (!layout.ContainsAttribute(attribute))
                {
                    shaderWriter.WriteLine($"VFX_ATTRIBUTE_IGNORE({HlslCodeHelper.GetTypeName(attribute.Type)}, {attribute.Name}, {HlslCodeHelper.GetValueString(attribute.DefaultValue)});");
                }
            }
            shaderWriter.NewLine();

            shaderWriter.WriteLine("void Init(VFXByteAddressBuffer buffer)");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine("attributeBuffer.Init(buffer);");
            shaderWriter.CloseBlock();
            shaderWriter.WriteLine("void StoreDefault(uint index)");
            shaderWriter.OpenBlock();
            foreach (var attributeLocation in layout)
            {
                var attribute = attributeLocation.Item1;
                shaderWriter.WriteLine($"Store_{attribute.Name}({HlslCodeHelper.GetValueString(attribute.DefaultValue)}, index);");
            }
            shaderWriter.CloseBlock();
            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
        }

        public bool WriteView(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView, DataView writtenDataView, string name, string sourceName, CompilationContext context)
        {
            var variableName = "data";

            shaderWriter.WriteLine($"struct {name}");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine($"{sourceName} buffer;");

            shaderWriter.NewLine();
            shaderWriter.WriteLine($"void Init({sourceName} buffer)");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine("this.buffer = buffer;");
            shaderWriter.CloseBlock();

            var attributeSourceManager = context.data.Get<AttributeSourceManager>();
            if (attributeSourceManager != null && attributeSourceManager.TryGetAttributeSource(usedDataView.Id, out var attributeSource))
            {
                // TODO: Detach attribute sources from attribute buffers?
                // That would allow multiple attribute sources for each attribute buffer, overloading the methods
                // If detached, the attribute set to be used would be included on each attributeSource (.AttributeSet)
                var attributeSourceTypename = attributeSource.ToString();
                if (readDataView.Children.Count > 0)
                {
                    shaderWriter.NewLine();
                    shaderWriter.WriteLine($"void LoadData(out {attributeSourceTypename} {variableName}, uint index)");
                    shaderWriter.OpenBlock();
                    shaderWriter.WriteLine($"{variableName}.Init();");
                    foreach (var attributeDataView in readDataView.Children)
                    {
                        var attribute = (attributeDataView.SubDataKey as AttributeKey).Attribute;
                        shaderWriter.WriteLine($"{variableName}.{attribute.Name} = buffer.Load_{attribute.Name}(index);");
                    }
                    shaderWriter.CloseBlock();
                }
                if (writtenDataView.Children.Count > 0)
                {
                    shaderWriter.NewLine();
                    shaderWriter.WriteLine($"void StoreData({attributeSourceTypename} {variableName}, uint index)");
                    shaderWriter.OpenBlock();
                    foreach (var attributeDataView in writtenDataView.Children)
                    {
                        var attribute = (attributeDataView.SubDataKey as AttributeKey).Attribute;
                        shaderWriter.WriteLine($"buffer.Store_{attribute.Name}({variableName}.{attribute.Name}, index);");
                    }
                    shaderWriter.CloseBlock();
                }
            }

            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
            return true;
        }

        public void DefineResourceUsage(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView,
            DataView writtenDataView)
        {
            throw new System.NotImplementedException();
        }

        public void UndefineResourceUsage(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView,
            DataView writtenDataView)
        {
            throw new System.NotImplementedException();
        }
    }
}
