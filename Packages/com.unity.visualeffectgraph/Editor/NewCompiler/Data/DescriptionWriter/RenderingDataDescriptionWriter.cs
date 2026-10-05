using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    class RenderingDataDescriptionWriter : IDataDescriptionWriter<RenderingData>
    {
        // Defines rather than types: the tasks that fill these and the output that reads them share one
        // container source, so the access is per task while the text declaring them is not.
        static readonly string SortingBufferTypeDefine = "SORTING_BUFFER_TYPE";
        static readonly string IndirectionBufferTypeDefine = "INDIRECTION_BUFFER_TYPE";

        /// <summary>
        /// Suffix of the indirection binding name, so the output pass can recognise the buffer holding
        /// the draw count without repeating the name shape.
        /// </summary>
        public const string IndirectionBufferSuffix = "_indirectionBuffer";

        const string SortingBufferSuffix = "_sortingBuffer";

        static string SortingBufferResourceName(string containerName) => $"_{containerName}{SortingBufferSuffix}";
        static string IndirectionBufferResourceName(string containerName) => $"_{containerName}{IndirectionBufferSuffix}";

        public void WriteDescription(ShaderWriter shaderWriter, DataView dataView, RenderingData renderingData, string name, CompilationContext context)
        {
            shaderWriter.IncludeFile("Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/RenderingData.hlsl");

            bool hasSortingData = dataView.FindSubData(RenderingData.SortingData, out _);
            bool hasIndirectionData = dataView.FindSubData(RenderingData.IndirectBuffer, out _);

            if (hasSortingData)
            {
                shaderWriter.WriteVariableDeclaration(SortingBufferTypeDefine, SortingBufferResourceName(name));
                shaderWriter.NewLine();
            }
            if (hasIndirectionData)
            {
                shaderWriter.WriteVariableDeclaration(IndirectionBufferTypeDefine, IndirectionBufferResourceName(name));
                shaderWriter.NewLine();
            }

            shaderWriter.WriteLine($"struct {name}");
            shaderWriter.OpenBlock();

            if(hasSortingData)
                shaderWriter.WriteLine("VFXSortingData sortingData;");

            if(hasIndirectionData)
                shaderWriter.WriteLine("VFXDrawIndirectionData drawIndirectionData;");

            shaderWriter.WriteLine("void Init(ThreadData threadData)");
            shaderWriter.OpenBlock();
            if (hasSortingData)
            {
                shaderWriter.WriteLine($"VFXStructuredBuffer_SortingKVP counter;");
                shaderWriter.WriteLine($"VFXStructuredBuffer_SortingKVP keysBuffer;");
                shaderWriter.WriteLine($"counter.Init({SortingBufferResourceName(name)}, threadData.instancingData.activeIndex, {1}u);");
                shaderWriter.WriteLine(
                    $"keysBuffer.Init({SortingBufferResourceName(name)}, threadData.instancingData.batchSize + threadData.instancingData.activeIndex * {renderingData.Capacity}, {renderingData.Capacity}u);");
                shaderWriter.WriteLine($"sortingData.Init(counter, keysBuffer);");
            }

            if (hasIndirectionData)
            {
                shaderWriter.WriteLine("VFXStructuredBuffer_uint indirectionCounter;");
                shaderWriter.WriteLine("VFXStructuredBuffer_uint indirectionBuffer;");
                shaderWriter.WriteLine(
                    $"indirectionCounter.Init({IndirectionBufferResourceName(name)}, threadData.instancingData.activeIndex, {1}u);");
                shaderWriter.WriteLine(
                    $"indirectionBuffer.Init({IndirectionBufferResourceName(name)}, threadData.instancingData.batchSize + threadData.instancingData.activeIndex * {renderingData.Capacity}, {renderingData.Capacity}u);");
                shaderWriter.WriteLine("drawIndirectionData.Init(indirectionCounter, indirectionBuffer);");
            }
            shaderWriter.CloseBlock();

            if (hasSortingData)
            {
                shaderWriter.NewLine();
                shaderWriter.WriteLine("void RegisterSortingKVP(SortingKVP kvp)");
                shaderWriter.OpenBlock();
                shaderWriter.WriteLine("sortingData.RegisterSortingKVP(kvp);");
                shaderWriter.CloseBlock();
            }

            if (hasIndirectionData)
            {
                shaderWriter.NewLine();
                shaderWriter.WriteLine("void AppendRenderingIndex(uint index)");
                shaderWriter.OpenBlock();
                shaderWriter.WriteLine("drawIndirectionData.AppendRenderingIndex(index);");
                shaderWriter.CloseBlock();
            }

            shaderWriter.NewLine();
            shaderWriter.WriteLine("bool GetRenderingIndex(uint index, out uint renderingIndex)");
            shaderWriter.OpenBlock();
            if (hasIndirectionData)
                shaderWriter.WriteLine("return drawIndirectionData.GetRenderingIndex(index, renderingIndex);");
            else
            {
                shaderWriter.WriteLine("renderingIndex = index;");
                shaderWriter.WriteLine("return true;");
            }
            shaderWriter.CloseBlock();

            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
        }

        public bool WriteView(ShaderWriter shaderWriter, DataView dataView, DataNode dataNode, string name, string sourceName, CompilationContext context)
        {
            bool hasSortingData = dataView.FindSubData(RenderingData.SortingData, out _);
            bool hasIndirectionData = dataView.FindSubData(RenderingData.IndirectBuffer, out _);

            shaderWriter.WriteLine($"struct {name}View");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine($"{sourceName} container;");
            shaderWriter.NewLine();
            shaderWriter.WriteLine($"void Init({sourceName} renderingData)");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine("container = renderingData;");
            shaderWriter.CloseBlock();

            if (hasSortingData)
            {
                shaderWriter.NewLine();
                shaderWriter.WriteLine("void RegisterSortingKVP(SortingKVP kvp) { container.RegisterSortingKVP(kvp); }");
            }

            if (hasIndirectionData)
            {
                shaderWriter.NewLine();
                shaderWriter.WriteLine("void AppendRenderingIndex(uint index) { container.AppendRenderingIndex(index); }");
            }

            shaderWriter.NewLine();
            shaderWriter.WriteLine("bool GetRenderingIndex(uint index, out uint renderingIndex) { return container.GetRenderingIndex(index, renderingIndex); }");

            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
            return true;
        }

        public void DefineResourceUsage(ShaderWriter shaderWriter, DataView dataView, DataNode dataNode)
        {
            if (dataView.FindSubData(RenderingData.SortingData, out var sortingView))
            {
                shaderWriter.Define(SortingBufferTypeDefine, dataNode.Writes(sortingView.Id)
                    ? "RWStructuredBuffer<SortingKVP>"
                    : "StructuredBuffer<SortingKVP>");
            }

            if (dataView.FindSubData(RenderingData.IndirectBuffer, out var indirectionView))
            {
                shaderWriter.Define(IndirectionBufferTypeDefine, dataNode.Writes(indirectionView.Id)
                    ? "RWStructuredBuffer<uint>"
                    : "StructuredBuffer<uint>");
            }
        }

        public void UndefineResourceUsage(ShaderWriter shaderWriter, DataView dataView, DataNode dataNode)
        {
            if (dataView.FindSubData(RenderingData.SortingData, out _))
                shaderWriter.Undefine(SortingBufferTypeDefine);

            if (dataView.FindSubData(RenderingData.IndirectBuffer, out _))
                shaderWriter.Undefine(IndirectionBufferTypeDefine);
        }

        public string GetResourceName(DataView dataView)
        {
            string containerName = dataView.DataContainer.IdentifierName;
            if (dataView.SubDataKey == RenderingData.SortingData)
                return SortingBufferResourceName(containerName);
            if (dataView.SubDataKey == RenderingData.IndirectBuffer)
                return IndirectionBufferResourceName(containerName);
            return null;
        }
    }
}
