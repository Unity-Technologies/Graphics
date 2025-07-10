
namespace UnityEngine.Rendering.RadeonRays
{
    internal class Scan
    {
        readonly ComputeShader shaderScan;
        readonly int kernelScan;

        readonly ComputeShader shaderReduce;
        readonly int kernelReduce;

        const uint kKeysPerThread = 4u;
        const uint kGroupSize     = 256u;
        const uint kKeysPerGroup  = kKeysPerThread * kGroupSize;

        public Scan(RadeonRaysShaders shaders)
        {
            shaderScan = shaders.blockScan;
            kernelScan = shaderScan.FindKernel("BlockScanAdd");

            shaderReduce = shaders.blockReducePart;
            kernelReduce = shaderReduce.FindKernel("BlockReducePart");
        }
        public void Execute(CommandBuffer cmd, GraphicsBuffer buffer, uint inputKeysOffset, uint outputKeysOffset, uint scratchDataOffset, uint size)
        {
            if (size > kKeysPerGroup)
            {
                var num_groups_level_1 = Common.CeilDivide(size, kKeysPerGroup);

                // Do first round of part sum reduction.
                SetState(cmd, shaderReduce, kernelReduce, size, buffer,
                    inputKeysOffset,
                    scratchDataOffset,
                    outputKeysOffset);
                cmd.DispatchCompute(shaderReduce, kernelReduce, (int)num_groups_level_1, 1, 1);

                if (num_groups_level_1 > kKeysPerGroup)
                {
                    var num_groups_level_2 = Common.CeilDivide(num_groups_level_1, kKeysPerGroup);

                    // Do second round of part sum reduction.
                    SetState(cmd, shaderReduce, kernelReduce, num_groups_level_1, buffer,
                        scratchDataOffset,
                        scratchDataOffset + num_groups_level_1,
                        scratchDataOffset);
                    cmd.DispatchCompute(shaderReduce, kernelReduce, (int)num_groups_level_2, 1, 1);

                    // Scan level 2 inplace.
                    Common.EnableKeyword(cmd, shaderScan, "ADD_PART_SUM", false);
                    SetState(cmd, shaderScan, kernelScan, num_groups_level_2, buffer,
                        scratchDataOffset + num_groups_level_1,
                        scratchDataOffset,
                        scratchDataOffset + num_groups_level_1);
                    cmd.DispatchCompute(shaderScan, kernelScan, 1, 1, 1);
                }

                // Scan and add level 2 back to level 1.
                {
                    Common.EnableKeyword(cmd, shaderScan, "ADD_PART_SUM", num_groups_level_1 > kKeysPerGroup);
                    SetState(cmd, shaderScan, kernelScan, num_groups_level_1, buffer,
                        scratchDataOffset,
                        scratchDataOffset + num_groups_level_1,
                        scratchDataOffset);
                    var num_groups_scan_level_1 = Common.CeilDivide(num_groups_level_1, kKeysPerGroup);
                    cmd.DispatchCompute(shaderScan, kernelScan, (int)num_groups_scan_level_1, 1, 1);
                }
            }

            // Scan and add level 1 back.
            {
                Common.EnableKeyword(cmd, shaderScan, "ADD_PART_SUM", size > kKeysPerGroup);
                SetState(cmd, shaderScan, kernelScan, size, buffer,
                    inputKeysOffset,
                    scratchDataOffset,
                    outputKeysOffset);
                var num_groups_scan_level_0 = Common.CeilDivide(size, kKeysPerGroup);
                cmd.DispatchCompute(shaderScan, kernelScan, (int)num_groups_scan_level_0, 1, 1);
            }
        }

        void SetState(
            CommandBuffer cmd,
            ComputeShader shader, int kernelIndex, uint size, GraphicsBuffer buffer,
            uint inputKeysOffset, uint scratchDataOffset, uint outputKeysOffset)
        {
            cmd.SetComputeIntParam(shader, SID.g_constants_num_keys, (int)size);

            cmd.SetComputeIntParam(shader, SID.g_constants_input_keys_offset, (int)inputKeysOffset);
            cmd.SetComputeIntParam(shader, SID.g_constants_part_sums_offset, (int)scratchDataOffset);
            cmd.SetComputeIntParam(shader, SID.g_constants_output_keys_offset, (int)outputKeysOffset);

            cmd.SetComputeBufferParam(shader, kernelIndex, SID.g_buffer, buffer);
        }

        static public ulong GetScratchDataSizeInDwords(uint size)
        {
            if (size <= kKeysPerGroup)
            {
                return 0;
            }
            else
            {
                var sizeLevel1 = Common.CeilDivide(size, kKeysPerGroup);
                if (sizeLevel1 <= kKeysPerGroup)
                {
                    return sizeLevel1;
                }
                else
                {
                    var sizeLevel2 = Common.CeilDivide(sizeLevel1, kKeysPerGroup);
                    return (sizeLevel1 + sizeLevel2);
                }
            }
        }
    }
}
