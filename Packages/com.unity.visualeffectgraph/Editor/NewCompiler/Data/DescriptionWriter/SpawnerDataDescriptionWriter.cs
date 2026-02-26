using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    class SpawnerDataDescriptionWriter : IDataDescriptionWriter<SpawnData>
    {
        public void WriteDescription(ShaderWriter shaderWriter, DataView dataView, SpawnData structuredData, string name, CompilationContext context)
        {
            shaderWriter.IncludeFile("Packages/com.unity.vfxgraph/Shaders/Data/SpawnerData.hlsl");
            shaderWriter.NewLine();

            shaderWriter.WriteLine($"struct {name}");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine("SpawnerData spawner;");
            shaderWriter.NewLine();
            shaderWriter.WriteLine("void Init()");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine("VFXStructuredBuffer_uint instancingPrefixSum;");
            shaderWriter.WriteLine($"instancingPrefixSum.Init(_{name}_instancingPrefixSum, {0}u,  {2}u) ;"); //TODO size = 2??
            shaderWriter.WriteLine($"spawner.Init(instancingPrefixSum);");
            shaderWriter.CloseBlock();
            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
        }

        public IEnumerable<(string, string)> GetUsedResources(string name, DataView usedDataView)
        {
            if (usedDataView.Id.IsValid)
            {
                yield return ("StructuredBuffer<uint>", $"_{name}_instancingPrefixSum");
            }
        }

    }
}
