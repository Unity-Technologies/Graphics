using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;

namespace UnityEditor.VFX
{
    class EventListDataDescriptionWriter : IDataDescriptionWriter<EventListData>
    {
        IDataDescriptionWriter<AttributeData> m_AttributeDataWriter;

        /// <summary>
        /// Suffix of the event index list binding name, so the output pass can recognise a lowered GPU event
        /// buffer without repeating the name shape.
        /// </summary>
        public const string EventIndexListSuffix = "_eventIndexList";

        static string EventIndexListResourceName(string containerName) => $"_{containerName}{EventIndexListSuffix}";
        static string AttributeBufferResourceName(string containerName) => $"_{containerName}_attributeBuffer";

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
            shaderWriter.WriteLine("void Init(ThreadData threadData)");
            shaderWriter.OpenBlock();

            if (eventListData.IsCpu)
            {
                shaderWriter.WriteLine("VFXStructuredBuffer_uint eventsPrefixSumBuffer;");
                shaderWriter.WriteLine("VFXStructuredBuffer_uint spawnCountPrefixSumBuffer;");
                shaderWriter.WriteLine($"eventsPrefixSumBuffer.Init({EventIndexListResourceName(name)}, 0, threadData.instancingData.activeCount);");
                shaderWriter.WriteLine($"spawnCountPrefixSumBuffer.Init({EventIndexListResourceName(name)}, threadData.instancingData.activeCount, {eventListData.BufferSize}u * threadData.instancingData.activeCount);");
                shaderWriter.WriteLine("eventListData.Init(eventsPrefixSumBuffer, spawnCountPrefixSumBuffer, threadData.instancingData.activeIndex);");
            }
            else
            {
                shaderWriter.WriteLine("VFXStructuredBuffer_uint elementCountBuffer;");
                shaderWriter.WriteLine("VFXStructuredBuffer_uint totalCountBuffer;");
                shaderWriter.WriteLine("VFXStructuredBuffer_uint prefixSumBuffer;");
                shaderWriter.WriteLine("VFXStructuredBuffer_uint sourceIndexBuffer;");
                shaderWriter.WriteLine($"elementCountBuffer.Init({EventIndexListResourceName(name)}, threadData.instancingData.instanceIndex, 1u);");
                shaderWriter.WriteLine($"totalCountBuffer.Init({EventIndexListResourceName(name)}, threadData.instancingData.batchSize + threadData.instancingData.instanceIndex, 1u);");
                shaderWriter.WriteLine($"prefixSumBuffer.Init({EventIndexListResourceName(name)}, 2u * threadData.instancingData.batchSize, threadData.instancingData.activeCount);");
                shaderWriter.WriteLine($"sourceIndexBuffer.Init({EventIndexListResourceName(name)}, 3u * threadData.instancingData.batchSize + threadData.instancingData.instanceIndex * {eventListData.Capacity}u, {eventListData.Capacity}u);");
                shaderWriter.WriteLine("eventListData.Init(elementCountBuffer, totalCountBuffer, prefixSumBuffer, sourceIndexBuffer, threadData.instancingData.activeIndex);");
            }

            if (attributeData != null)
            {
                shaderWriter.NewLine();
                shaderWriter.WriteLine("VFXByteAddressBuffer buffer;");
                var layoutCompilationData = context.data.Get<AttributeSetLayoutCompilationData>();
                shaderWriter.WriteLine($"buffer.Init({AttributeBufferResourceName(name)}, threadData.instancingData.activeIndex * {layoutCompilationData[attributeData].GetBufferSize()}u, {layoutCompilationData[attributeData].GetBufferSize()}u);");
                shaderWriter.WriteLine("attributes.Init(buffer);");
            }

            shaderWriter.CloseBlock();
            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
        }

        public bool WriteView(ShaderWriter shaderWriter, DataView usedDataView, DataNode dataNode, string name, string sourceName, CompilationContext context)
        {
            usedDataView.FindSubData(EventData.AttributeDataKey, out var attributeUsedDataView);
            bool needsAttributeData = attributeUsedDataView.Valid;
            if (needsAttributeData)
            {
                m_AttributeDataWriter.WriteView(shaderWriter, attributeUsedDataView, dataNode, name + "_Attributes", sourceName + "_Attributes", context);
                shaderWriter.NewLine();
            }
            shaderWriter.WriteLine($"struct {name}View");
            shaderWriter.OpenBlock();
            var eventListData = usedDataView.DataDescription as EventListData;
            if (eventListData.IsCpu)
            {
                shaderWriter.WriteLine("CPUEventListData eventListData;");
                shaderWriter.WriteLine("uint baseEventCount;");
            }
            else
            {
                shaderWriter.WriteLine("GPUEventListData eventListData;");
                shaderWriter.WriteLine("uint baseEventCount;");
            }
            if (needsAttributeData)
            {
                shaderWriter.WriteLine($"{name}_AttributesView attributes;");
            }
            shaderWriter.NewLine();
            shaderWriter.WriteLine($"void Init({sourceName} eventData, ContextDataView contextData)");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine("eventListData = eventData.eventListData;");
            if (needsAttributeData)
            {
                shaderWriter.WriteLine("attributes.Init(eventData.attributes);");
            }
            if (eventListData.IsCpu)
            {
                shaderWriter.WriteLine("baseEventCount = contextData.initSpawnIndex;");
            }
            else
            {
                shaderWriter.WriteLine("baseEventCount = eventData.eventListData.baseEventCount;");
            }
            shaderWriter.CloseBlock();

            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
            return true;
        }

        public IEnumerable<(string, string)> GetUsedResources(string name, DataView usedDataView)
        {
            if (usedDataView.Valid)
            {
                bool isCpu = (usedDataView.DataDescription as EventListData).IsCpu;
                yield return (isCpu ? "StructuredBuffer<uint>" : "RWStructuredBuffer<uint>", EventIndexListResourceName(name));
            }

            if (usedDataView.FindSubData(EventData.AttributeDataKey, out var _))
            {
                string attributeBufferType = "ByteAddressBuffer";
                string attributeBufferName = AttributeBufferResourceName(name);
                yield return (attributeBufferType, attributeBufferName);
            }
        }

        public string GetResourceName(DataView dataView)
        {
            string containerName = dataView.DataContainer.IdentifierName;
            if (dataView.IsRoot)
                return EventIndexListResourceName(containerName);
            if (dataView.SubDataKey == EventData.AttributeDataKey)
                return AttributeBufferResourceName(containerName);
            return null;
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
