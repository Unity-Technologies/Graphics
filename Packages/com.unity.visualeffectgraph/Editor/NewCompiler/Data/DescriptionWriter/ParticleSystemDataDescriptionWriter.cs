using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;

namespace UnityEditor.VFX
{
    class ParticleSystemDataDescriptionWriter : IDataDescriptionWriter<ParticleData>
    {
        IDataDescriptionWriter<AttributeData> m_AttributeDataWriter;
        private static readonly string DeadlistBufferTypeDefine = "DEADLIST_BUFFER_TYPE";
        private static readonly string AttributeBufferTypeDefine = "ATTRIBUTE_BUFFER_TYPE";

        public ParticleSystemDataDescriptionWriter(IDataDescriptionWriter<AttributeData> attributeDataWriter)
        {
            Debug.Assert(attributeDataWriter != null);
            m_AttributeDataWriter = attributeDataWriter;
        }

        public void WriteDescription(ShaderWriter shaderWriter, DataView dataView, ParticleData particleSystemData, string name, CompilationContext context)
        {
            var layoutCompilationData = context.data.Get<AttributeSetLayoutCompilationData>();

            shaderWriter.IncludeFile("Packages/com.unity.vfxgraph/Shaders/Data/ParticleSystemData.hlsl");

            var attributeData = dataView.FindSubData(ParticleData.AttributeDataKey, out var attributeDataView) ? attributeDataView.DataDescription as AttributeData : null;
            var deadlist = dataView.FindSubData(ParticleData.DeadlistKey, out var deadlistDataView) ? deadlistDataView.DataDescription : null;

            if (deadlist != null)
            {
                shaderWriter.IncludeFile("Packages/com.unity.vfxgraph/Shaders/Data/DeadListData.hlsl");
            }

            if (attributeData != null)
            {
                shaderWriter.NewLine();
                m_AttributeDataWriter.WriteDescription(shaderWriter, attributeDataView, name + "_ParticleAttributeBuffer", context);
            }
            shaderWriter.NewLine();
            shaderWriter.WriteLine($"struct {name}");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine("VFXParticleSystemData particleSystem;");
            shaderWriter.WriteLine($"{name}_ParticleAttributeBuffer particleAttributeBuffer;");
            if (deadlist != null)
            {
                shaderWriter.WriteLine("VFXDeadListData deadList;");
            }
            shaderWriter.NewLine();
            shaderWriter.WriteLine("void Init()");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine($"uint capacity = {particleSystemData.Capacity}u;");
            shaderWriter.WriteLine("particleSystem.Init(capacity);");
            if (attributeData != null)
            {
                shaderWriter.NewLine();
                shaderWriter.WriteLine("VFXByteAddressBuffer particleBuffer;");
                shaderWriter.WriteLine($"particleBuffer.Init(_{name}_attributeBuffer, {0}u, {layoutCompilationData[attributeData].GetBufferSize()}u);");
                shaderWriter.WriteLine("particleAttributeBuffer.Init(particleBuffer);");
            }
            if (deadlist != null)
            {
                shaderWriter.NewLine();
                shaderWriter.WriteLine("VFXStructuredBuffer_uint deadListCounter;");
                shaderWriter.WriteLine("VFXStructuredBuffer_uint deadListCounterCopy;");
                shaderWriter.WriteLine("VFXStructuredBuffer_uint deadListBuffer;");
                shaderWriter.WriteLine($"deadListCounter.Init(_{name}_deadListBuffer, {0}u, {1}u);");
                shaderWriter.WriteLine($"deadListCounterCopy.Init(_{name}_deadListBuffer, {1}u, {1}u);");
                shaderWriter.WriteLine($"deadListBuffer.Init(_{name}_deadListBuffer, {2}u, {particleSystemData.Capacity}u);");
                shaderWriter.WriteLine("deadList.Init(deadListCounter, deadListCounterCopy, deadListBuffer);");
            }
            shaderWriter.CloseBlock();

            shaderWriter.NewLine();
            shaderWriter.WriteLine("bool NewParticle(uint threadIndex, out uint particleIndex)");
            shaderWriter.OpenBlock();
            if (deadlist != null)
            {
                shaderWriter.WriteLine("bool success = deadList.NewIndex(threadIndex, particleIndex);");
                shaderWriter.WriteLine("if(success)");
                shaderWriter.OpenBlock();
                shaderWriter.WriteLine("particleAttributeBuffer.StoreDefault(particleIndex);");
                shaderWriter.CloseBlock();
                shaderWriter.WriteLine("return success;");
            }
            else
            {
                shaderWriter.WriteLine("index = 0;");
                shaderWriter.WriteLine("return true;");
            }
            shaderWriter.CloseBlock();

            shaderWriter.NewLine();
            shaderWriter.WriteLine("bool DeleteParticle(uint index)");
            shaderWriter.OpenBlock();
            if (deadlist != null)
            {
                shaderWriter.WriteLine("particleAttributeBuffer.Store_alive(false, index);");
                shaderWriter.WriteLine("return deadList.DeleteIndex(index);");
            }
            else
            {
                shaderWriter.WriteLine("return true;");
            }
            shaderWriter.CloseBlock();

            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
        }

        public bool WriteView(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView, DataView writtenDataView, string name, string sourceName, CompilationContext context)
        {
            usedDataView.FindSubData(ParticleData.AttributeDataKey, out var attributeUsedDataView);
            bool needsParticleAttributeData = attributeUsedDataView.Id.IsValid;
            if (needsParticleAttributeData)
            {
                readDataView.FindSubData(ParticleData.AttributeDataKey, out var attributeReadDataView);
                writtenDataView.FindSubData(ParticleData.AttributeDataKey, out var attributeWrittenDataView);
                m_AttributeDataWriter.WriteView(shaderWriter, attributeUsedDataView, attributeReadDataView, attributeWrittenDataView, name + "_ParticleAttributeBuffer", sourceName + "_ParticleAttributeBuffer", context);
                shaderWriter.NewLine();
            }
            shaderWriter.WriteLine($"struct {name}View");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine($"{sourceName} container;");
            if (needsParticleAttributeData)
            {
                shaderWriter.WriteLine($"{name}_ParticleAttributeBuffer particleAttributeBuffer;");
            }
            shaderWriter.NewLine();
            shaderWriter.WriteLine($"void Init({sourceName} particleData)");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine("container = particleData;");
            if (needsParticleAttributeData)
            {
                shaderWriter.WriteLine("particleAttributeBuffer.Init(particleData.particleAttributeBuffer);");
            }
            shaderWriter.CloseBlock();

            shaderWriter.NewLine();
            shaderWriter.WriteLine("bool NewParticle(uint threadIndex, out uint particleIndex) { return container.NewParticle(threadIndex, particleIndex); }");

            shaderWriter.NewLine();
            shaderWriter.WriteLine("bool DeleteParticle(uint index) { return container.DeleteParticle(index); }");

            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
            shaderWriter.WriteLine($"{name}View {name};"); // TODO: probably externally only for the actual binding, not inner types
            return true;
        }

        public string GetSubdataName(IDataKey subDataKey)
        {
            if (subDataKey == ParticleData.AttributeDataKey)
            {
                return ".particleAttributeBuffer";
            }
            else
            {
                throw new System.NotImplementedException();
            }
        }

        public string GetSubdataTypeName(IDataKey subDataKey)
        {
            if (subDataKey == ParticleData.AttributeDataKey)
            {
                return "_ParticleAttributeBuffer";
            }
            else
            {
                throw new System.NotImplementedException();
            }
        }

        public void DefineResourceUsage(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView,
            DataView writtenDataView)
        {
            if(writtenDataView.FindSubData(ParticleData.AttributeDataKey, out var attributeWrittenDataView))
            {
                shaderWriter.Define(AttributeBufferTypeDefine, "RWByteAddressBuffer");
            }
            else
            {
                shaderWriter.Define(AttributeBufferTypeDefine, "ByteAddressBuffer");
            }

            if (writtenDataView.FindSubData(ParticleData.DeadlistKey, out var _))
            {
                shaderWriter.Define(DeadlistBufferTypeDefine, "RWStructuredBuffer<uint>");
            }
            else
            {
                shaderWriter.Define(DeadlistBufferTypeDefine, "StructuredBuffer<uint>");
            }
        }

        public void UndefineResourceUsage(ShaderWriter shaderWriter, DataView usedDataView, DataView readDataView,
            DataView writtenDataView)
        {
            if (usedDataView.FindSubData(ParticleData.AttributeDataKey, out var _))
            {
                shaderWriter.Undefine(AttributeBufferTypeDefine);
            }

            if (usedDataView.FindSubData(ParticleData.DeadlistKey, out var _))
            {
                shaderWriter.Undefine(DeadlistBufferTypeDefine);
            }
        }

        public IEnumerable<(string, string)> GetUsedResources(string name, DataView usedDataView)
        {
            if (usedDataView.FindSubData(ParticleData.AttributeDataKey, out var _))
            {
                string attributeBufferType = AttributeBufferTypeDefine; //TODO: Define actual resource types
                string attributeBufferName = $"_{name}_attributeBuffer";
                yield return (attributeBufferType, attributeBufferName);
            }

            if (usedDataView.FindSubData(ParticleData.DeadlistKey, out var _))
            {
                string deadlistBufferType = DeadlistBufferTypeDefine; //TODO: Define actual resource types
                string deadlistBufferName = $"_{name}_deadListBuffer";
                yield return (deadlistBufferType, deadlistBufferName);
            }
        }
    }
}

