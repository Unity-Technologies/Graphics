namespace UnityEditor.VFX.Test
{

    public struct VFXCustomHLSLBufferCase
    {
        public string declaration;
        public string implementation;

        public override string ToString()
        {
            return declaration;
        }
    }

    internal static class VFXCustomHLSLBufferTestCases
    {
        internal static readonly VFXCustomHLSLBufferCase[] kSampleBufferCase =
        {
            new() { declaration = "AppendStructuredBuffer<uint>", implementation = "inputBuffer.Append(0u); localValue = 0.1f;" },
            new() { declaration = "ConsumeStructuredBuffer<float3>", implementation = "localValue = inputBuffer.Consume();" },
            new() { declaration = "RWByteAddressBuffer", implementation = "inputBuffer.Store3(0, (float3)1.0f); localValue = asfloat(inputBuffer.Load3(0));" },
            new() { declaration = "ByteAddressBuffer", implementation = "localValue = asfloat(inputBuffer.Load3(0));" },
            new() { declaration = $"StructuredBuffer<{nameof(CustomHLSLOperatorTestType)}>", implementation = "localValue = inputBuffer[0].position;" },
            new() { declaration = "StructuredBuffer<uint3>", implementation = "localValue = asfloat(inputBuffer[0]);" },
            new() { declaration = "StructuredBuffer<float3>", implementation = "localValue = inputBuffer[0];" },
            new() { declaration = "RWStructuredBuffer<float3>", implementation = "inputBuffer[0].x += 0.0f; localValue = inputBuffer[0];" },

            new() { declaration = "RWTexture2D<uint>", implementation = "InterlockedAdd(inputBuffer[(int2)0], 1u); localValue = 0.1f;" },
            new() { declaration = "RWTexture2D<float3>", implementation = "inputBuffer[(int2)0].xyz = (float3)1.0f; localValue = 0.1f;" },
            new() { declaration = "Texture2D<float3>", implementation = "localValue = inputBuffer[(int2)0].rgb;" },
            new() { declaration = "Texture2D", implementation = "localValue = inputBuffer[(int2)0].rgb;" },

            new() { declaration = "RWTexture1D<uint>", implementation = "inputBuffer[0] = 1u; localValue = 0.1f;" },
            new() { declaration = "RWTexture1DArray<float>", implementation = "inputBuffer[(uint2)0] = 0u; localValue = 0.1f;" },
            new() { declaration = "Texture1D", implementation = "localValue = inputBuffer[(uint)0];" }
        };
    }
}
