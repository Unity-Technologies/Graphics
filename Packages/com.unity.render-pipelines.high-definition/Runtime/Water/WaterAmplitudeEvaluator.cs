using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Rendering.HighDefinition;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace UnityEditor.Rendering.HighDefinition
{
    class WaterAmplitudeEvaluator
    {
        // Defines the number of subdivisions
        const int k_NumIterations = 32;
        const int k_NumTimeSteps = 512;
        const WaterSimulationResolution resolutionEnum = WaterSimulationResolution.Low64;
        const int resolution = (int)resolutionEnum;
        const int numPixels = resolution * resolution;

        [BurstCompile]
        internal struct ReductionStep : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<float4> InputBuffer;

            // Output spectrum buffer
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float4> OutputBuffer;

            public void Execute(int index)
            {
                float4 maxDisplacement = 0.0f;
                for (int i = 0; i < resolution; ++i)
                    maxDisplacement = max(maxDisplacement, InputBuffer[i + index * resolution]);
                OutputBuffer[index] = maxDisplacement;
            }
        }

        static void EvaluateMaxAmplitude(NativeArray<float4> startBuffer, NativeArray<float4> intBuffer, NativeArray<float4> finBuffer)
        {
            ReductionStep reductionStep = new ReductionStep();
            reductionStep.InputBuffer = startBuffer;
            reductionStep.OutputBuffer = intBuffer;
            JobHandle handle = reductionStep.Schedule(resolution, 1);
            handle.Complete();

            reductionStep = new ReductionStep();
            reductionStep.InputBuffer = intBuffer;
            reductionStep.OutputBuffer = finBuffer;
            handle = reductionStep.Schedule(1, 1);
            handle.Complete();
        }

#if UNITY_EDITOR
        // [MenuItem("Generation/Water Amplitude Table")]
        static public void GenerateAmplitudeTable()
        {
            // Number of pixels per band
            int waterSampleOffset = HDRenderPipeline.EvaluateWaterNoiseSampleOffset(resolutionEnum);
            float waterSpectrumOffset = HDRenderPipeline.EvaluateFrequencyOffset(resolutionEnum);

            // Allocate all the native buffers
            NativeArray<float2> h0BufferCPU = new NativeArray<float2>(numPixels, Allocator.Persistent);
            NativeArray<float4> htR0 = new NativeArray<float4>(numPixels, Allocator.Persistent);
            NativeArray<float4> htI0 = new NativeArray<float4>(numPixels, Allocator.Persistent);
            NativeArray<float4> htR1 = new NativeArray<float4>(numPixels, Allocator.Persistent);
            NativeArray<float4> htI1 = new NativeArray<float4>(numPixels, Allocator.Persistent);
            NativeArray<float3> pingPong = new NativeArray<float3>(numPixels * 4, Allocator.Persistent);
            NativeArray<uint4> indices = new NativeArray<uint4>(numPixels, Allocator.Persistent);
            NativeArray<float4> displacementBufferCPU = new NativeArray<float4>(numPixels, Allocator.Persistent);
            NativeArray<float4> intermediateBuffer = new NativeArray<float4>(resolution, Allocator.Persistent);
            NativeArray<float4> maxDisplacement = new NativeArray<float4>(1, Allocator.Persistent);

            // output table for the string
            //string outputTable = new string("PatchSize/WindSpeed,");
            string outputTable = new string("");

            // Evaluate the wind speed for this patch size
            float maxWindSpeed = WaterConsts.k_SwellMaximumWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;

            // Iterate over the patch size
            for (int patchSizeIndex = 0; patchSizeIndex < k_NumIterations; ++patchSizeIndex)
            {
                // Evaluate the current patch size
                float patchRatio = patchSizeIndex / (float)(k_NumIterations - 1);
                float currentPatchSize = 25.0f + patchRatio * (5000.0f - 25.0f);

                for (int windSpeedIndex = 0; windSpeedIndex < k_NumIterations; ++windSpeedIndex)
                {
                    // Evaluate the wind speed
                    float windRatio = windSpeedIndex / (float)(k_NumIterations - 1);
                    float windSpeed = windRatio * maxWindSpeed;

                    // Prepare the first band
                    WaterCPUSimulation.PhillipsSpectrumInitialization spectrumInit = new WaterCPUSimulation.PhillipsSpectrumInitialization();
                    spectrumInit.simulationResolution = resolution;
                    spectrumInit.waterSampleOffset = waterSampleOffset;
                    spectrumInit.sliceIndex = 0;
                    spectrumInit.windOrientation = 0;
                    spectrumInit.windSpeed = windSpeed;
                    spectrumInit.patchSize = currentPatchSize;
                    spectrumInit.directionDampner = 1.0f;
                    spectrumInit.bufferOffset = 0;
                    spectrumInit.H0Buffer = h0BufferCPU;

                    // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
                    JobHandle handle = spectrumInit.Schedule((int)numPixels, 1);
                    handle.Complete();

                    float maxAmplitude = 0.0f;
                    for (int i = 0; i < k_NumTimeSteps; ++i)
                    {
                        // Evaluate the simulation time
                        float simulationTime = (float)i * 10.0f;

                        // Prepare the first band
                        WaterCPUSimulation.EvaluateDispersion dispersion = new WaterCPUSimulation.EvaluateDispersion();
                        dispersion.simulationResolution = resolution;
                        dispersion.patchSize = currentPatchSize;
                        dispersion.bufferOffset = 0;
                        dispersion.simulationTime = simulationTime;

                        // Input buffers
                        dispersion.H0Buffer = h0BufferCPU;

                        // Output buffers
                        dispersion.HtRealBuffer = htR0;
                        dispersion.HtImaginaryBuffer = htI0;

                        // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
                        JobHandle dispersionHandle = dispersion.Schedule((int)numPixels, 1);
                        dispersionHandle.Complete();

                        // Prepare the first band
                        WaterCPUSimulation.InverseFFT inverseFFT0 = new WaterCPUSimulation.InverseFFT();
                        inverseFFT0.simulationResolution = resolution;
                        inverseFFT0.butterflyCount = HDRenderPipeline.ButterFlyCount(resolutionEnum);
                        inverseFFT0.bufferOffset = 0;
                        inverseFFT0.columnPass = false;

                        // Input buffers
                        inverseFFT0.HtRealBufferInput = htR0;
                        inverseFFT0.HtImaginaryBufferInput = htI0;

                        // Temp buffers
                        inverseFFT0.pingPongArray = pingPong;
                        inverseFFT0.textureIndicesArray = indices;

                        // Output buffers buffers
                        inverseFFT0.HtRealBufferOutput = htR1;
                        inverseFFT0.HtImaginaryBufferOutput = htI1;

                        // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
                        JobHandle ifft0Handle = inverseFFT0.Schedule(resolution, 1, dispersionHandle);
                        ifft0Handle.Complete();

                        // Second inverse FFT
                        WaterCPUSimulation.InverseFFT inverseFFT1 = new WaterCPUSimulation.InverseFFT();
                        inverseFFT1.simulationResolution = resolution;
                        inverseFFT1.butterflyCount = HDRenderPipeline.ButterFlyCount(resolutionEnum);
                        inverseFFT1.bufferOffset = 0;
                        inverseFFT1.columnPass = true;

                        // Input buffers
                        inverseFFT1.HtRealBufferInput = htR1;
                        inverseFFT1.HtImaginaryBufferInput = htI1;

                        // Temp buffers
                        inverseFFT1.pingPongArray = pingPong;
                        inverseFFT1.textureIndicesArray = indices;

                        // Output buffers buffers
                        inverseFFT1.HtRealBufferOutput = displacementBufferCPU;
                        inverseFFT1.HtImaginaryBufferOutput = htR0;

                        // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
                        JobHandle ifft1Handle = inverseFFT1.Schedule(resolution, 1, ifft0Handle);
                        ifft1Handle.Complete();

                        // Evaluate the max amplitude of the displacement
                        EvaluateMaxAmplitude(displacementBufferCPU, intermediateBuffer, maxDisplacement);

                        // Add this contribution
                        maxAmplitude += maxDisplacement[0].x;
                    }

                    // Output the results
                    outputTable += (maxAmplitude / (float)k_NumTimeSteps).ToString() + "f, ";
                }
                outputTable += "\n";
            }

            // Release the buffers
            maxDisplacement.Dispose();
            intermediateBuffer.Dispose();
            displacementBufferCPU.Dispose();
            indices.Dispose();
            pingPong.Dispose();
            htI1.Dispose();
            htR1.Dispose();
            htI0.Dispose();
            htR0.Dispose();
            h0BufferCPU.Dispose();

            // Output our table
            Debug.Log(outputTable);
        }
#endif
    }
}
