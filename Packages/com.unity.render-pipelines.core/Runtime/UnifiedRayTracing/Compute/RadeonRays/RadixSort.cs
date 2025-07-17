
namespace UnityEngine.Rendering.RadeonRays
{
    internal class RadixSort
    {
        readonly ComputeShader shaderBitHistogram;
        readonly int kernelBitHistogram;

        readonly ComputeShader shaderScatter;
        readonly int kernelScatter;

        readonly Scan scan;

        const uint kKeysPerThread  = 4u;
        const uint kGroupSize      = 256u;
        const uint kKeysPerGroup   = kKeysPerThread * kGroupSize;
        const int kNumBitsPerPass = 4;

        public RadixSort(RadeonRaysShaders shaders)
        {
            shaderBitHistogram = shaders.bitHistogram;
            kernelBitHistogram = shaderBitHistogram.FindKernel("BitHistogram");

            shaderScatter = shaders.scatter;
            kernelScatter = shaderScatter.FindKernel("Scatter");

            scan = new Scan(shaders);
        }
        public void Execute(
            CommandBuffer cmd,
            GraphicsBuffer buffer,
            uint inputKeysOffset, uint outputKeysOffset,
            uint inputValuesOffset, uint outputValuesOffset,
            uint scratchDataOffset, uint size)
        {
            uint num_histogram_values = (1 << kNumBitsPerPass) * Common.CeilDivide(size, kKeysPerGroup);
            uint num_groups = Common.CeilDivide(size, kKeysPerGroup);

            uint tempsKeys = scratchDataOffset;
            uint tempValues = tempsKeys + size ;
            uint groupHistograms = tempValues + size;
            uint scan_scratch = groupHistograms + num_histogram_values;

            uint i = outputKeysOffset;
            uint iv = outputValuesOffset;

            uint o = tempsKeys;
            uint ov = tempValues;

            for (uint bitshift = 0u; bitshift < 32; bitshift += kNumBitsPerPass)
            {
                // Calculate histograms
                {
                    cmd.SetComputeIntParam(shaderBitHistogram, SID.g_constants_num_keys, (int)size);
                    cmd.SetComputeIntParam(shaderBitHistogram, SID.g_constants_num_blocks, (int)Common.CeilDivide(size, kKeysPerGroup));
                    cmd.SetComputeIntParam(shaderBitHistogram, SID.g_constants_bit_shift, (int)bitshift);

                    cmd.SetComputeBufferParam(shaderBitHistogram, kernelBitHistogram, SID.g_buffer, buffer);
                    cmd.SetComputeIntParam(shaderBitHistogram, SID.g_input_keys_offset, (int)(bitshift == 0 ? inputKeysOffset : i));
                    cmd.SetComputeIntParam(shaderBitHistogram, SID.g_group_histograms_offset, (int)groupHistograms);

                    cmd.DispatchCompute(shaderBitHistogram, kernelBitHistogram, (int)num_groups, 1, 1);
                }

                // Scan histograms
                scan.Execute(cmd, buffer, groupHistograms, groupHistograms, scan_scratch, num_histogram_values);

                // Scatter key values
                {
                    cmd.SetComputeIntParam(shaderScatter, SID.g_constants_num_keys, (int)size);
                    cmd.SetComputeIntParam(shaderScatter, SID.g_constants_num_blocks, (int)Common.CeilDivide(size, kKeysPerGroup));
                    cmd.SetComputeIntParam(shaderScatter, SID.g_constants_bit_shift, (int)bitshift);

                    cmd.SetComputeBufferParam(shaderScatter, kernelScatter, SID.g_buffer, buffer);
                    cmd.SetComputeIntParam(shaderScatter, SID.g_input_keys_offset, (int)(bitshift == 0 ? inputKeysOffset : i));
                    cmd.SetComputeIntParam(shaderScatter, SID.g_group_histograms_offset, (int)groupHistograms);
                    cmd.SetComputeIntParam(shaderScatter, SID.g_output_keys_offset, (int)o);
                    cmd.SetComputeIntParam(shaderScatter, SID.g_input_values_offset, (int)(bitshift == 0 ? inputValuesOffset : iv));
                    cmd.SetComputeIntParam(shaderScatter, SID.g_output_values_offset, (int)ov);

                    cmd.DispatchCompute(shaderScatter, kernelScatter, (int)num_groups, 1, 1);
                }

                // swap buffers
                (o, i) = (i, o);
                (ov, iv) = (iv, ov);
            }
        }
        static public ulong GetScratchDataSizeInDwords(uint size)
        {
            uint num_histogram_values = (1 << kNumBitsPerPass) * Common.CeilDivide(size, kKeysPerGroup);

            ulong scratch_size = 0;
            // Histogram buffer: num bins * num groups
            scratch_size += num_histogram_values;
            // Temporary buffers for ping-pong
            // additional 1024 ints are a workaround for when DX can generate out of bounds error
            // even if no actual out of bounds access happening
            scratch_size += 2 * size + 1024;
            // Scan scratch size
            scratch_size += Scan.GetScratchDataSizeInDwords(num_histogram_values);

            return scratch_size;
        }
    }
}
