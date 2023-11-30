using System;
using NUnit.Framework;
using UnityEditor.TestTools.TestRunner;
using UnityEditorInternal;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    static class FurnaceTestIDs
    {
        public static readonly int _TestResult = Shader.PropertyToID("_TestResult");
        public static readonly int _OutgoingDirection = Shader.PropertyToID("_OutgoingDirection");
        public static readonly int _BetaM = Shader.PropertyToID("_BetaM");
        public static readonly int _BetaN = Shader.PropertyToID("_BetaN");
    }


    class FurnaceTests
    {
        private const int SAMPLE_COUNT = 10000000;
        private const int GROUP_SIZE = 512;
        private const int NUM_GROUPS = (SAMPLE_COUNT + GROUP_SIZE - 1) / GROUP_SIZE;

        float InvFourPI() => 1 / (4f * Mathf.PI);

        // [Test]
        public void FurnaceTestHairReference()
        {
            var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            var furnaceTestCS = hdrp.runtimeShaders.furnaceTestCS;

            var cmd = CommandBufferPool.Get();
            var buffer = new ComputeBuffer(NUM_GROUPS, sizeof(float));

            void Cleanup()
            {
                // Note: This won't be release on assertion failure.
                CommandBufferPool.Release(cmd);
                CoreUtils.SafeRelease(buffer);
            }

            // TODO: Investigate why this is so unstable for low variances (even at 10 million samples).
            for (float betaM = 0.4f; betaM < 1.0f; betaM += 0.2f)
            {
                for (float betaN = 0.4f; betaN < 1.0f; betaN += 0.2f)
                {
                    cmd.SetComputeVectorParam(furnaceTestCS, FurnaceTestIDs._OutgoingDirection, Random.onUnitSphere.normalized);
                    cmd.SetComputeFloatParam(furnaceTestCS, FurnaceTestIDs._BetaM, betaM);
                    cmd.SetComputeFloatParam(furnaceTestCS, FurnaceTestIDs._BetaN, betaN);
                    cmd.SetComputeBufferParam(furnaceTestCS, 0, FurnaceTestIDs._TestResult, buffer);

                    cmd.DispatchCompute(furnaceTestCS, 0, NUM_GROUPS, 1, 1);
                    Graphics.ExecuteCommandBuffer(cmd);

                    // Read back the test result.
                    float[] F = new float[NUM_GROUPS];
                    buffer.GetData(F);

                    float F_Avg = 0;

                    for (int i = 0; i < NUM_GROUPS; ++i)
                    {
                        F_Avg += F[i];
                    }

                    F_Avg /= SAMPLE_COUNT * InvFourPI();

                    // The reflected uniform radiance for a non-absorbing fiber should be within ~5% of energy conserving.
                    const float kErrorThreshold = 0.05f;

                    try
                    {
                        Assert.That(F_Avg >= 1f - kErrorThreshold &&
                                    F_Avg <= 1f + kErrorThreshold,
                            $"Expected result within { kErrorThreshold * 100f }% error, got { Math.Round(Mathf.Abs(1f - F_Avg) * 100f, 2) }%" +
                            $" for Beta M: { betaM} Beta N { betaN }.");
                    }
                    catch (AssertionException)
                    {
                        Cleanup();
                        throw;
                    }
                }
            }

            Cleanup();
        }
    }
}
