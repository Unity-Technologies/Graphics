using System;
using NUnit.Framework;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class FurnaceTests
    {
        private const int SAMPLE_COUNT = 300000;
        private const int GROUP_SIZE = 128;
        private const int NUM_GROUPS = (SAMPLE_COUNT + GROUP_SIZE - 1) / GROUP_SIZE;

        float InvFourPI() => 1 / (4f * Mathf.PI);

        // Need to take at least hundreds of thousands of BSDF evaluations to produce a stable result,
        // so evaluate a sample per thread and sum up.

        [Test]
        public void FurnaceTestHairReference()
        {
            var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;

            var furnaceTestCS = hdrp.defaultResources.shaders.furnaceTestCS;
            var furnaceTestResultBuffer = new ComputeBuffer(NUM_GROUPS, sizeof(float));
            var cmd = CommandBufferPool.Get();

            cmd.SetComputeVectorParam(furnaceTestCS, "_OutgoingDirection", Random.onUnitSphere);
            cmd.SetComputeBufferParam(furnaceTestCS, 0, "_FurnaceTestResult", furnaceTestResultBuffer);
            cmd.DispatchCompute(furnaceTestCS, 0, NUM_GROUPS, 1, 1);
            Graphics.ExecuteCommandBuffer(cmd);

            // Read back the test result.
            float[] F = new float[NUM_GROUPS];
            furnaceTestResultBuffer.GetData(F);

            float F_Avg = 0;

            for (int i = 1; i < NUM_GROUPS - 1; ++i)
            {
                F_Avg += F[i];
            }

            F_Avg /= SAMPLE_COUNT * InvFourPI();

            Debug.Log(F_Avg);

            // The reflected uniform radiance for a non-absorbing fiber should be within ~5% of energy conserving.
            Assert.Greater(F_Avg, 0.95f);
            Assert.Less(F_Avg, 1.05f);

            CoreUtils.SafeRelease(furnaceTestResultBuffer);
            CommandBufferPool.Release(cmd);
        }
    }
}
