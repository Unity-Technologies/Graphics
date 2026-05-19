using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;

namespace UnityEditor.VFX
{
    class EventListDataDescriptionWriter : IDataDescriptionWriter<EventListData>
    {
        IDataDescriptionWriter<AttributeData> m_AttributeDataWriter;

        public EventListDataDescriptionWriter(IDataDescriptionWriter<AttributeData> attributeDataWriter)
        {
            Debug.Assert(attributeDataWriter != null);
            m_AttributeDataWriter = attributeDataWriter;
        }

        public void WriteDescription(ShaderWriter shaderWriter, DataView dataView, EventListData eventListData, string name, CompilationContext context)
        {
            shaderWriter.IncludeFile("Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/EventListData.hlsl");

            var attributeData = dataView.FindSubData(EventData.AttributeDataKey, out var attributeDataView) ? attributeDataView.DataDescription as AttributeData : null;
            if (attributeData != null)
            {
                shaderWriter.NewLine();
                m_AttributeDataWriter.WriteDescription(shaderWriter, attributeDataView, name + "_Attributes", context);
            }

            shaderWriter.NewLine();
            shaderWriter.WriteLine($"struct {name}");
            shaderWriter.OpenBlock();
            if (eventListData.IsCpu)
            {
                shaderWriter.WriteLine("CPUEventListData eventListData;");
            }
            else
            {
                shaderWriter.WriteLine("GPUEventListData eventListData;");
            }
            if (attributeData != null)
            {
                shaderWriter.WriteLine($"{name}_Attributes attributes;");
            }
            shaderWriter.NewLine();
            shaderWriter.WriteLine("void Init()");
            shaderWriter.OpenBlock();

            shaderWriter.WriteLine("VFXStructuredBuffer_uint eventListBuffer;");
            shaderWriter.WriteLine($"eventListBuffer.Init(_{name}_eventIndexList, 0, {eventListData.BufferSize});");
            shaderWriter.WriteLine("eventListData.Init(eventListBuffer);");

            if (attributeData != null)
            {
                shaderWriter.NewLine();
                shaderWriter.WriteLine("VFXByteAddressBuffer buffer;");
                var layoutCompilationData = context.data.Get<AttributeSetLayoutCompilationData>();
                shaderWriter.WriteLine($"buffer.Init(_{name}_attributeBuffer, {0}u, {layoutCompilationData[attributeData].GetBufferSize()}u);");
                shaderWriter.WriteLine("attributes.Init(buffer);");
            }

            shaderWriter.CloseBlock();
            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
        }

        public bool WriteView(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView, DataView writtenDataView, string name, string sourceName, CompilationContext context)
        {
            usedDataView.FindSubData(EventData.AttributeDataKey, out var attributeUsedDataView);
            bool needsAttributeData = attributeUsedDataView.Id.IsValid;
            if (needsAttributeData)
            {
                readDataView.FindSubData(EventData.AttributeDataKey, out var attributeReadDataView);
                writtenDataView.FindSubData(EventData.AttributeDataKey, out var attributeWrittenDataView);
                m_AttributeDataWriter.WriteView(shaderWriter, attributeUsedDataView, attributeReadDataView, attributeWrittenDataView, name + "_Attributes", sourceName + "_Attributes", context);
                shaderWriter.NewLine();
            }
            shaderWriter.WriteLine($"struct {name}View");
            shaderWriter.OpenBlock();
            var eventListData = usedDataView.DataDescription as EventListData;
            if (eventListData.IsCpu)
            {
                shaderWriter.WriteLine("CPUEventListData eventListData;");
            }
            else
            {
                shaderWriter.WriteLine("GPUEventListData eventListData;");
            }
            if (needsAttributeData)
            {
                shaderWriter.WriteLine($"{name}_AttributesView attributes;");
            }
            shaderWriter.NewLine();
            shaderWriter.WriteLine($"void Init({sourceName} eventData)");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine("eventListData = eventData.eventListData;");
            if (needsAttributeData)
            {
                shaderWriter.WriteLine("attributes.Init(eventData.attributes);");
            }
            shaderWriter.CloseBlock();

            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
            return true;
        }

        public IEnumerable<(string, string)> GetUsedResources(string name, DataView usedDataView)
        {
            if (usedDataView.Id.IsValid)
            {
                bool isCpu = (usedDataView.DataDescription as EventListData).IsCpu;
                yield return (isCpu ? "StructuredBuffer<uint>" : "RWStructuredBuffer<uint>", $"_{name}_eventIndexList");
            }

            if (usedDataView.FindSubData(EventData.AttributeDataKey, out var _))
            {
                string attributeBufferType = "ByteAddressBuffer";
                string attributeBufferName = $"_{name}_attributeBuffer";
                yield return (attributeBufferType, attributeBufferName);
            }
        }

        public string GetSubdataName(DataView dataView, IDataKey subDataKey)
        {
            if (subDataKey == EventData.AttributeDataKey)
            {
                return ".attributes";
            }
            else
            {
                throw new System.NotImplementedException();
            }
        }

        public string GetSubdataTypeName(IDataKey subDataKey)
        {
            if (subDataKey == EventData.AttributeDataKey)
            {
                return "_Attributes";
            }
            else
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
