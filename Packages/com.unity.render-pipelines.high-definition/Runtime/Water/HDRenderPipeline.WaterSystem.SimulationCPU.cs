using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Structure that holds the water surface data used for height requests.
    /// </summary>
    public struct WaterSimSearchData
    {
        /// <summary>
        /// Native array that holds the displacement data for all bands.
        /// </summary>
        [ReadOnly]
        public NativeArray<float4> displacementData;

        /// <summary>
        /// Elevation of the surface (Y axis coordinate).
        /// </summary>
        public float waterSurfaceElevation;

        /// <summary>
        /// Resolution that the CPU simulation runs at.
        /// </summary>
        public int simulationRes;

        /// <summary>
        /// Spectrum data
        /// </summary>
        public WaterSpectrumParameters spectrum;

        /// <summary>
        /// Rendering data
        /// </summary>
        public WaterRenderingParameters rendering;

        /// <summary>
        /// Number of bands that the CPU simulation needs to evaluates.
        /// </summary>
        public int activeBandCount;
    }

    /// <summary>
    /// Structure that holds the input parameters of the search.
    /// </summary>
    public struct WaterSearchParameters
    {
        /// <summary>
        /// Target position that the search needs to evaluate the height at.
        /// </summary>
        public float3 targetPosition;

        /// <summary>
        /// Position that the search starts from. Can be used as a hint for the search algorithm.
        /// </summary>
        public float3 startPosition;

        /// <summary>
        /// Target error value at which the algorithm should stop.
        /// </summary>
        public float error;

        /// <summary>
        /// Number of iterations of the search algorithm.
        /// </summary>
        public int maxIterations;
    }

    /// <summary>
    /// Structure that holds the output parameters of the search.
    /// </summary>
    public struct WaterSearchResult
    {
        /// <summary>
        /// World space elevation of the input target elevation
        /// </summary>
        public float height;

        /// <summary>
        /// Horizontal error value of the search algorithm.
        /// </summary>
        public float error;

        /// <summary>
        /// Location of the 3D point that has been displaced to the target positions.
        /// </summary>
        public float3 candidateLocation;

        /// <summary>
        /// Number of iterations of the search algorithm to find the height value.
        /// </summary>
        public int numIterations;
    }

    internal class WaterCPUSimulation
    {
        // Simulation constants
        internal const float k_EarthGravity = 9.81f;
        internal const float k_OneOverSqrt2 = 0.70710678118f;
        internal const float k_PhillipsAmplitudeScalar = 0.2f;
        internal const int k_NoiseFunctionOffset = 64;

        // Function that generates a uint4 noise from a uint3
        // Extracted from this Shader Toy: https://www.shadertoy.com/view/4tfyW4
        internal static uint4 WaterHashFunctionUInt4(uint3 coord)
        {
            uint4 x = coord.xyzz;
            x = ((x >> 16) ^ x.yzxy) * 0x45d9f3bu;
            x = ((x >> 16) ^ x.yzxz) * 0x45d9f3bu;
            x = ((x >> 16) ^ x.yzxx) * 0x45d9f3bu;
            return x;
        }

        // Function that generates a float4 normalized noise from a uint3
        internal static float4 WaterHashFunctionFloat4(uint3 p)
        {
            uint4 hashed = WaterHashFunctionUInt4(p);
            return new float4(hashed.x, hashed.y, hashed.z, hashed.w) / (float)0xffffffffU;
        }

        // Gaussian distribution
        // http://www.dspguide.com/ch2/6.htm
        internal static float GaussianDis(float u, float v)
        {
            return Mathf.Sqrt(-2.0f * Mathf.Log(Mathf.Max(u, 1e-6f))) * Mathf.Cos(Mathf.PI * v);
        }

        internal static float2 ComplexExp(float arg)
        {
            return new float2(Mathf.Cos(arg), Mathf.Sin(arg));
        }

        internal static float2 ComplexMult(float2 a, float2 b)
        {
            return new float2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
        }

        [BurstCompile]
        internal struct PhillipsSpectrumInitialization : IJobParallelFor
        {
            // Resolution at which the simulation will be evaluate
            public int simulationResolution;
            // Offset applied to the noise pattern to guarantee the coherence independently from the resolution
            public int waterSampleOffset;
            // Index of the current slice
            public int sliceIndex;
            // Offset in the output buffer
            public int bufferOffset;

            // Wind speed scalar
            public float windSpeed;
            // Wind orientation
            public float windOrientation;
            // Attenuation of the current based on the wind direction
            public float directionDampner;
            // Patch size of the current patch
            public float patchSize;

            // Output spectrum buffer
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float2> H0Buffer;

            float Phillips(float2 k, float2 w, float V, float directionDampener, float patchSize)
            {
                float kk = k.x * k.x + k.y * k.y;
                float result = 0.0f;
                if (kk != 0.0)
                {
                    float L = (V * V) / k_EarthGravity;
                    // To avoid _any_ directional bias when there is no wind we lerp towards 0.5f
                    float2 k_n = k / Mathf.Sqrt(kk);
                    float wk = Mathf.Lerp(Vector2.Dot(k_n, w), 0.5f, directionDampner);
                    float phillips = (Mathf.Exp(-1.0f / (kk * L * L)) / (kk * kk)) * (wk * wk);
                    result = phillips * (wk < 0.0f ? directionDampner : 1.0f);
                }
                return k_PhillipsAmplitudeScalar * result / (patchSize * patchSize);
            }

            public void Execute(int index)
            {
                // Compute the coordinates of the current pixel to process
                int x = index % simulationResolution;
                int y = index / simulationResolution;

                // Generate the random numbers to use
                uint3 coords = new uint3((uint)x, (uint)y, (uint)sliceIndex);
                uint3 sampleCoord = new uint3((uint)(x + waterSampleOffset), (uint)(y + waterSampleOffset), (uint)sliceIndex);
                float4 rn = WaterHashFunctionFloat4(sampleCoord + k_NoiseFunctionOffset);

                // First part of the Phillips spectrum term
                float2 E = k_OneOverSqrt2 * new float2(GaussianDis(rn.x, rn.y), GaussianDis(rn.z, rn.w));

                // Second part of the Phillips spectrum term
                float2 k = Mathf.PI * 2.0f * ((float2)coords.xy - simulationResolution * 0.5f)  / patchSize;
                float2 windDirection = -HDRenderPipeline.OrientationToDirection(windOrientation);
                float P = Phillips(k, windDirection, windSpeed, directionDampner, patchSize);

                // Combine and output
                H0Buffer[index + bufferOffset] = E * Mathf.Sqrt(P);
            }
        }

        [BurstCompile]
        internal struct EvaluateDispersion : IJobParallelFor
        {
            // Resolution at which the simulation will be evaluate
            public int simulationResolution;
            // Offset in the output buffer
            public int bufferOffset;
            // Size of the current patch
            public float patchSize;
            // Passed simulation time since the beginning of the simulation
            public float simulationTime;

            // Input spectrum buffer
            [ReadOnly]
            public NativeArray<float2> H0Buffer;

            // Output frequency spectrum (real and imaginary buffers)
            [WriteOnly]
            public NativeArray<float4> HtRealBuffer;
            [WriteOnly]
            public NativeArray<float4> HtImaginaryBuffer;

            public void Execute(int index)
            {
                // Compute the coordinates of the current pixel to process
                int x = index % simulationResolution;
                int y = index / simulationResolution;

                float2 k = Mathf.PI * 2.0f * (new float2(x, y) - simulationResolution * 0.5f) / patchSize;
                float kl = Mathf.Sqrt(k.x * k.x + k.y * k.y);
                float w = Mathf.Sqrt(k_EarthGravity * kl);
                float2 kx = new float2(k.x / kl, 0.0f);
                float2 ky = new float2(k.y / kl, 0.0f);

                float2 h0 = H0Buffer[index + bufferOffset];
                float2 ht = ComplexMult(h0, ComplexExp(w * simulationTime));
                float2 dx = ComplexMult(ComplexMult(new float2(0, -1), kx), ht);
                float2 dy = ComplexMult(ComplexMult(new float2(0, -1), ky), ht);

                if (float.IsNaN(dx.x)) dx.x = 0.0f;
                if (float.IsNaN(dx.y)) dx.y = 0.0f;
                if (float.IsNaN(dy.x)) dy.x = 0.0f;
                if (float.IsNaN(dy.y)) dy.y = 0.0f;

                // TODO: This is a work around to handle singularity at origin.
                int halfBandResolution = simulationResolution / 2;
                if ((x == halfBandResolution) && (y == halfBandResolution))
                {
                    dx = new float2(0, 0);
                    dy = new float2(0, 0);
                }

                // Output the complex values
                HtRealBuffer[index] = new float4(ht.x, dx.x, dy.x, 0);
                HtImaginaryBuffer[index] = new float4(ht.y, dx.y, dy.y, 0);
            }
        }

        [BurstCompile]
        internal struct InverseFFT : IJobParallelFor
        {
            // Resolution at which the simulation will be evaluate
            public int simulationResolution;
            // Number of butterfly passes to run
            public int butterflyCount;
            // Offset when outputting the partially transformed ifft values
            public int bufferOffset;
            // Flag that defines if this is the column (second) ifft pass
            public bool columnPass;

            // Input frequency spectrum (real and imaginary buffers)
            [ReadOnly]
            public NativeArray<float4> HtRealBufferInput;
            [ReadOnly]
            public NativeArray<float4> HtImaginaryBufferInput;

            // The ping-pong array is used as read/write buffer
            // These are mimicing the LDS on the shader side.
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> pingPongArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<uint4> textureIndicesArray;

            // Output spectrum (real and imaginary buffers)
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float4> HtRealBufferOutput;
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float4> HtImaginaryBufferOutput;

            uint2 reversebits_uint2(uint2 input)
            {
                uint2 x = input;
                x = (((x & 0xaaaaaaaa) >> 1) | ((x & 0x55555555) << 1));
                x = (((x & 0xcccccccc) >> 2) | ((x & 0x33333333) << 2));
                x = (((x & 0xf0f0f0f0) >> 4) | ((x & 0x0f0f0f0f) << 4));
                x = (((x & 0xff00ff00) >> 8) | ((x & 0x00ff00ff) << 8));
                return ((x >> 16) | (x << 16));
            }

            void GetButterflyValues(uint passIndex, uint x, out uint2 indices, out float2 weights)
            {
                uint sectionWidth = 2u << (int)passIndex;
                uint halfSectionWidth = sectionWidth / 2;

                uint sectionStartOffset = x & ~(sectionWidth - 1);
                uint halfSectionOffset = x & (halfSectionWidth - 1);
                uint sectionOffset = x & (sectionWidth - 1);

                float angle = Mathf.PI * 2.0f * sectionOffset / (float)sectionWidth;
                weights.y = -Mathf.Sin(angle);
                weights.x = Mathf.Cos(angle);

                indices.x = sectionStartOffset + halfSectionOffset;
                indices.y = sectionStartOffset + halfSectionOffset + halfSectionWidth;

                if (passIndex == 0)
                {
                    uint2 reversedIndices = reversebits_uint2(indices.xy);
                    indices = new uint2(reversedIndices.x >> (32 - butterflyCount) & (uint)(simulationResolution - 1), reversedIndices.y >> (32 - butterflyCount) & (uint)(simulationResolution - 1));
                }
            }

            void ButterflyPass(uint passIndex, uint x, uint t0, uint t1, int ppOffset, out float3 resultR, out float3 resultI)
            {
                uint2 indices;
                float2 weights;
                GetButterflyValues(passIndex, x, out indices, out weights);

                float3 inputR1 = pingPongArray[ppOffset + (int)t0 * simulationResolution + (int)indices.x];
                float3 inputI1 = pingPongArray[ppOffset + (int)t1 * simulationResolution + (int)indices.x];

                float3 inputR2 = pingPongArray[ppOffset + (int)t0 * simulationResolution + (int)indices.y];
                float3 inputI2 = pingPongArray[ppOffset + (int)t1 * simulationResolution + (int)indices.y];

                resultR = (inputR1 + weights.x * inputR2 + weights.y * inputI2);
                resultI = (inputI1 - weights.y * inputR2 + weights.x * inputI2);
            }

            public void Execute(int index)
            {
                // Compute the offset in the ping-pong array
                int ppOffset = 4 * simulationResolution * index;

                // Load the real and imaginary numbers to the "LDS"
                for (int x = 0; x < simulationResolution; ++x)
                {
                    // Compute the coordinates of the current pixel to process
                    int y = index;

                    // Depending on which pass we are in we need to flip
                    uint2 texturePos;
                    if (columnPass)
                        texturePos = new uint2((uint)y, (uint)x);
                    else
                        texturePos = new uint2((uint)x, (uint)y);

                    // Load entire row or column into scratch array
                    uint tapCoord = texturePos.x + texturePos.y * (uint)simulationResolution;
                    pingPongArray[ppOffset + 0 * simulationResolution + x] = HtRealBufferInput[(int)tapCoord].xyz;
                    pingPongArray[ppOffset + 1 * simulationResolution + x] = HtImaginaryBufferInput[(int)tapCoord].xyz;
                }

                // Initialize the texture indices
                for (int x = 0; x < simulationResolution; ++x)
                    textureIndicesArray[index * simulationResolution + x] = new uint4(0, 1, 2, 3);

                // Do all the butterfly passes
                for (int i = 0; i < butterflyCount - 1; i++)
                {
                    // Having this loop inside this one, acts like a GroupMemoryBarrierWithGroupSync
                    for (int x = 0; x < simulationResolution; ++x)
                    {
                        // Build the position
                        int2 position = new int2(x, index);

                        // Grab the texture index from last butterfly pass
                        uint4 currentTexIndices = textureIndicesArray[index * simulationResolution + x];

                        // Do the butterfly pass pass
                        float3 realValue;
                        float3 imaginaryValue;
                        ButterflyPass((uint)i, (uint)x, currentTexIndices.x, currentTexIndices.y, ppOffset, out realValue, out imaginaryValue);

                        // Output the results back to the ping pong array
                        pingPongArray[ppOffset + (int)currentTexIndices.z * simulationResolution + position.x] = realValue;
                        pingPongArray[ppOffset + (int)currentTexIndices.w * simulationResolution + position.x] = imaginaryValue;

                        // Revert the indices
                        currentTexIndices.xyzw = currentTexIndices.zwxy;

                        // Save the indices for the next butterfly pass
                        textureIndicesArray[index * simulationResolution + position.x] = currentTexIndices;
                    }
                }

                // Mimic the synchronization point here
                for (int x = 0; x < simulationResolution; ++x)
                {
                    // Compute the coordinates of the current pixel to process
                    int y = index;

                    // Depending on which pass we are in we need to flip
                    uint2 texturePos;
                    if (columnPass)
                        texturePos = new uint2((uint)y, (uint)x);
                    else
                        texturePos = new uint2((uint)x, (uint)y);

                    // Load entire row or column into scratch array
                    uint tapCoord = texturePos.x + texturePos.y * (uint)simulationResolution;

                    // Grab the texture indices
                    uint4 currentTexIndices = textureIndicesArray[index * simulationResolution + x];

                    // Output values of the ifft pass
                    float3 realValue;
                    float3 imaginaryValue;
                    ButterflyPass((uint)(butterflyCount - 1), (uint)x, currentTexIndices.x, currentTexIndices.y, ppOffset, out realValue, out imaginaryValue);

                    // The final pass writes to the output UAV texture
                    if (columnPass)
                    {
                        // last pass of the inverse transform. The imaginary value is no longer needed
                        float sign_correction_and_normalization = ((x + y) & 0x01) != 0 ? -1.0f : 1.0f;
                        HtRealBufferOutput[(int)tapCoord + bufferOffset] = new float4(realValue * sign_correction_and_normalization, 0.0f);
                    }
                    else
                    {
                        HtRealBufferOutput[(int)tapCoord] = new float4(realValue, 0.0f);
                        HtImaginaryBufferOutput[(int)tapCoord] = new float4(imaginaryValue, 0.0f);
                    }
                }
            }
        }
    }

    public partial class HDRenderPipeline
    {
        // Function that returns the number of butterfly passes depending on the resolution
        internal static int ButterFlyCount(WaterSimulationResolution simRes)
        {
            switch (simRes)
            {
                case WaterSimulationResolution.High256:
                    return 8;
                case WaterSimulationResolution.Medium128:
                    return 7;
                case WaterSimulationResolution.Low64:
                    return 6;
                default:
                    return 0;
            }
        }

        // Flag that allows us to track if the CPU simulation was initialized
        bool m_ActiveWaterSimulationCPU = false;

        // CPU Simulation Data
        internal NativeArray<float4> htR0;
        internal NativeArray<float4> htI0;
        internal NativeArray<float4> htR1;
        internal NativeArray<float4> htI1;
        internal NativeArray<float3> pingPong;
        internal NativeArray<uint4> indices;

        void InitializeCPUWaterSimulation()
        {
            // Only initialize if the asset supports it
            if (!m_Asset.currentPlatformRenderPipelineSettings.waterCPUSimulation)
                return;

            // Flag required for freeing the resources at the end
            m_ActiveWaterSimulationCPU = true;

            // Convert the resolution to and int
            int res = (int)m_WaterBandResolution;

            // Allocate all the intermediary buffer
            htR0 = new NativeArray<float4>(res * res, Allocator.Persistent);
            htI0 = new NativeArray<float4>(res * res, Allocator.Persistent);
            htR1 = new NativeArray<float4>(res * res, Allocator.Persistent);
            htI1 = new NativeArray<float4>(res * res, Allocator.Persistent);
            pingPong = new NativeArray<float3>(res * res * 4, Allocator.Persistent);
            indices = new NativeArray<uint4>(res * res, Allocator.Persistent);
        }

        void ReleaseCPUWaterSimulation()
        {
            // If it was not previously initialized, we don't have anything to do
            if (!m_ActiveWaterSimulationCPU)
                return;

            // Free the native buffers
            htR0.Dispose();
            htI0.Dispose();
            htR1.Dispose();
            htI1.Dispose();
            pingPong.Dispose();
            indices.Dispose();
        }

        void UpdateCPUWaterSimulation(WaterSurface waterSurface, bool evaluateSpetrum, ShaderVariablesWater shaderVariablesWater)
        {
            // If the asset doesn't support the CPU simulation or the surface doesn't, we don't have to do anything
            if (!m_ActiveWaterSimulationCPU || !waterSurface.cpuSimulation)
                return;

            // Based on if the simulation has been flagged as half or full resolution
            WaterSimulationResolution cpuSimResolution = waterSurface.GetSimulationResolutionCPU();
            uint cpuSimResUint = (uint)cpuSimResolution;

            // Number of pixels per band
            uint numPixels = cpuSimResUint * cpuSimResUint;
            int waterSampleOffset = EvaluateWaterNoiseSampleOffset(cpuSimResolution);

            // Re-evaluate the spectrum if needed.
            if (evaluateSpetrum)
            {
                // To avoid re-evaluating
                // If we get here, it means the spectrum is invalid and we need to go re-evaluate it
                for (int bandIndex = 0; bandIndex < shaderVariablesWater._WaterBandCount; ++bandIndex)
                {
                    // Prepare the first band
                    WaterCPUSimulation.PhillipsSpectrumInitialization spectrumInit = new WaterCPUSimulation.PhillipsSpectrumInitialization();
                    spectrumInit.simulationResolution = (int)cpuSimResolution;
                    spectrumInit.waterSampleOffset = waterSampleOffset;
                    spectrumInit.sliceIndex = bandIndex;
                    spectrumInit.windOrientation = shaderVariablesWater._PatchWindOrientation[bandIndex];
                    spectrumInit.windSpeed = shaderVariablesWater._PatchWindSpeed[bandIndex];
                    spectrumInit.patchSize = shaderVariablesWater._PatchSize[bandIndex];
                    spectrumInit.directionDampner = shaderVariablesWater._PatchDirectionDampener[bandIndex];
                    spectrumInit.bufferOffset = (int)(bandIndex * numPixels);
                    spectrumInit.H0Buffer = waterSurface.simulation.cpuBuffers.h0BufferCPU;

                    // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
                    JobHandle handle = spectrumInit.Schedule((int)numPixels, 1);
                    handle.Complete();
                }
            }

            // For each band, we evaluate the dispersion then the two inverse fft passes
            int activeBandCount = HDRenderPipeline.EvaluateCPUBandCount(waterSurface.surfaceType, waterSurface.ripples, waterSurface.cpuEvaluateRipples);
            for (int bandIndex = 0; bandIndex < activeBandCount; ++bandIndex)
            {
                // Prepare the first band
                WaterCPUSimulation.EvaluateDispersion dispersion = new WaterCPUSimulation.EvaluateDispersion();
                dispersion.simulationResolution = (int)cpuSimResolution;
                dispersion.patchSize = shaderVariablesWater._PatchSize[bandIndex];
                dispersion.bufferOffset = (int)(bandIndex * numPixels);
                dispersion.simulationTime = shaderVariablesWater._SimulationTime;

                // Input buffers
                dispersion.H0Buffer = waterSurface.simulation.cpuBuffers.h0BufferCPU;

                // Output buffers
                dispersion.HtRealBuffer = htR0;
                dispersion.HtImaginaryBuffer = htI0;

                // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
                JobHandle dispersionHandle = dispersion.Schedule((int)numPixels, 1);
                dispersionHandle.Complete();

                // Prepare the first band
                WaterCPUSimulation.InverseFFT inverseFFT0 = new WaterCPUSimulation.InverseFFT();
                inverseFFT0.simulationResolution = (int)cpuSimResolution;
                inverseFFT0.butterflyCount = ButterFlyCount(cpuSimResolution);
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
                JobHandle ifft0Handle = inverseFFT0.Schedule((int)cpuSimResolution, 1, dispersionHandle);
                ifft0Handle.Complete();

                // Second inverse FFT
                WaterCPUSimulation.InverseFFT inverseFFT1 = new WaterCPUSimulation.InverseFFT();
                inverseFFT1.simulationResolution = (int)cpuSimResolution;
                inverseFFT1.butterflyCount = ButterFlyCount(cpuSimResolution);
                inverseFFT1.bufferOffset = (int)(bandIndex * numPixels);
                inverseFFT1.columnPass = true;

                // Input buffers
                inverseFFT1.HtRealBufferInput = htR1;
                inverseFFT1.HtImaginaryBufferInput = htI1;

                // Temp buffers
                inverseFFT1.pingPongArray = pingPong;
                inverseFFT1.textureIndicesArray = indices;

                // Output buffers buffers
                inverseFFT1.HtRealBufferOutput = waterSurface.simulation.cpuBuffers.displacementBufferCPU;
                inverseFFT1.HtImaginaryBufferOutput = htR0;

                // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
                JobHandle ifft1Handle = inverseFFT1.Schedule((int)cpuSimResolution, 1, ifft0Handle);
                ifft1Handle.Complete();
            }
        }

        static int SignedMod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        // This function does a "repeat" load
        static float4 LoadDisplacementData(NativeArray<float4> displacementBuffer, int2 coord, int bandIndex, int simResolution)
        {
            int2 repeatCoord = coord;
            repeatCoord.x = SignedMod(repeatCoord.x, simResolution);
            repeatCoord.y = SignedMod(repeatCoord.y, simResolution);
            int bandOffset = simResolution * simResolution * bandIndex;
            return displacementBuffer[repeatCoord.x + repeatCoord.y * simResolution + bandOffset];
        }

        static void PrepareCoordinates(float2 uv, int resolution, out int2 tapCoord, out float2 fract)
        {
            float2 unnormalized = (uv * resolution) - 0.5f;
            tapCoord = (int2)floor(floor(unnormalized) + 0.5f);
            fract = frac(unnormalized);
        }

        static float4 SampleDisplacementBilinear(NativeArray<float4> displacementBuffer, float2 uvCoord, int bandIndex, int simResolution)
        {
            // Convert the position from uv to floating pixel coords (for the bilinear interpolation)
            PrepareCoordinates(uvCoord, simResolution, out int2 currentTapCoord, out float2 fract);

            // Read the four samples we want
            float4 p0 = LoadDisplacementData(displacementBuffer, currentTapCoord, bandIndex, simResolution);
            float4 p1 = LoadDisplacementData(displacementBuffer, currentTapCoord + new int2(1, 0), bandIndex, simResolution);
            float4 p2 = LoadDisplacementData(displacementBuffer, currentTapCoord + new int2(0, 1), bandIndex, simResolution);
            float4 p3 = LoadDisplacementData(displacementBuffer, currentTapCoord + new int2(1, 1), bandIndex, simResolution);

            // Do the bilinear interpolation
            float4 i0 = lerp(p0, p1, fract.x);
            float4 i1 = lerp(p2, p3, fract.x);
            return lerp(i0, i1, fract.y);
        }

        internal static float3 EvaluateWaterDisplacement(WaterSimSearchData wsd, Vector3 positionAWS, Vector3 currentOrientation, Vector3 currentSpeed, float simulationTime, float4 bandsMultiplier)
        {
            // Compute the simulation coordinates
            Vector2 uv = new Vector2(positionAWS.x, positionAWS.z);
            Vector2 uvBand0 = (uv - HDRenderPipeline.OrientationToDirection(currentOrientation.x) * currentSpeed.x * simulationTime) / wsd.spectrum.patchSizes.x;
            Vector2 uvBand1 = (uv - HDRenderPipeline.OrientationToDirection(currentOrientation.y) * currentSpeed.y * simulationTime) / wsd.spectrum.patchSizes.y;
            Vector2 uvBand2 = (uv - HDRenderPipeline.OrientationToDirection(currentOrientation.z) * currentSpeed.z * simulationTime) / wsd.spectrum.patchSizes.z;

            // Accumulate the displacement from the various layers
            float3 totalDisplacement = 0.0f;

            // Attenuate using the water mask
            // First band
            float3 rawDisplacement = SampleDisplacementBilinear(wsd.displacementData, uvBand0, 0, wsd.simulationRes).xyz * wsd.rendering.patchAmplitudeMultiplier.x * bandsMultiplier.x;
            totalDisplacement += rawDisplacement;

            // If we have more than two bounds, we need to add the displacement of the high frequency bounds
            if (wsd.activeBandCount >= 2)
            {
                // Second band
                rawDisplacement = SampleDisplacementBilinear(wsd.displacementData, uvBand1, 1, wsd.simulationRes).xyz * wsd.rendering.patchAmplitudeMultiplier.y * bandsMultiplier.y;
                totalDisplacement += rawDisplacement;
            }

            // If we have more than two bounds, we need to add the displacement of the high frequency bounds
            if (wsd.activeBandCount == 3)
            {
                // Third band
                rawDisplacement = SampleDisplacementBilinear(wsd.displacementData, uvBand2, 2, wsd.simulationRes).xyz * wsd.rendering.patchAmplitudeMultiplier.z * bandsMultiplier.z;
                totalDisplacement += rawDisplacement;
            }

            // We only apply the choppiness tot he first two bands, doesn't behave very good past those
            totalDisplacement.yz *= WaterConsts.k_WaterMaxChoppinessValue;

            // The vertical displacement is stored in the X channel and the XZ displacement in the YZ channel
            return new float3(-totalDisplacement.y, totalDisplacement.x, -totalDisplacement.z);
        }

        internal struct WaterSimulationTapData
        {
            public float3 currentDisplacement;
            public float3 displacedPoint;
            public float2 offset;
            public float distance;
            public float height;
        }

        static WaterSimulationTapData EvaluateDisplacementData(WaterSimSearchData wsd, float3 currentLocation, float3 referencePosition)
        {
            WaterSimulationTapData data;

            // Evaluate the displacement at the current point
            data.currentDisplacement = EvaluateWaterDisplacement(wsd, currentLocation, wsd.rendering.patchCurrentOrientation, wsd.rendering.patchCurrentSpeed, wsd.rendering.simulationTime, new float4(1, 1, 1, 1));

            // Evaluate the complete position
            data.displacedPoint = currentLocation + data.currentDisplacement;

            // Evaluate the distance to the reference point
            data.offset = referencePosition.xz - data.displacedPoint.xz;

            // Length of the offset vector
            data.distance = Mathf.Sqrt(data.offset.x * data.offset.x + data.offset.y * data.offset.y);

            // Simulation height of the position of the offset vector
            data.height = data.currentDisplacement.y + wsd.waterSurfaceElevation;
            return data;
        }

        internal static void FindWaterSurfaceHeight(WaterSimSearchData wsd,
                                                    WaterSearchParameters wsp,
                                                    out WaterSearchResult sr)
        {
            // Initialize the search data
            WaterSimulationTapData tapData = EvaluateDisplacementData(wsd, wsp.startPosition, wsp.targetPosition);
            float2 stepSize = tapData.offset;
            sr.error = tapData.distance;
            sr.height = tapData.height;
            sr.candidateLocation = wsp.startPosition;
            sr.numIterations = 0;

            // Go through the steps until we found a position that satisfies our constraints
            while (sr.numIterations < wsp.maxIterations)
            {
                // Is the point close enough to target position?
                if (sr.error < wsp.error)
                    break;

                // Reset the search progress flag
                bool progress = false;

                float3 candidateLocation = sr.candidateLocation + new float3(stepSize.x, 0, stepSize.y);
                tapData = EvaluateDisplacementData(wsd, candidateLocation, wsp.targetPosition);
                if (tapData.distance < sr.error)
                {
                    sr.candidateLocation = candidateLocation;
                    stepSize = tapData.offset;
                    sr.error = tapData.distance;
                    sr.height = tapData.height;
                    progress = true;
                }

                // If we didn't make any progress in this step, this means out steps are probably too big make them smaller
                if (!progress)
                    stepSize *= 0.5f;

                sr.numIterations++;
            }
        }
    }

    /// <summary>
    /// C# Job that evaluate the height for a set of WaterSearchParameters and returns a set of WaterSearchResult (and stored them into native buffers).
    /// </summary>
    [BurstCompile]
    public struct WaterSimulationSearchJob : IJobParallelFor
    {
        /// <summary>
        /// Input simulation search data produced by the water surface.
        /// </summary>
        public WaterSimSearchData simSearchData;

        /// <summary>
        /// Native array that holds the set of position that the job will need to evaluate the height for.
        /// </summary>
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> targetPositionBuffer;

        /// <summary>
        /// Native array that holds the set of "hint" position that the algorithm starts from.
        /// </summary>
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> startPositionBuffer;

        /// <summary>
        /// Target error value at which the algorithm should stop.
        /// </summary>
        public float error;

        /// <summary>
        /// Number of iterations of the search algorithm.
        /// </summary>
        public int maxIterations;

        /// <summary>
        /// Output native array that holds the set of heights that were evaluated for each target position.
        /// </summary>
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> heightBuffer;

        /// <summary>
        /// Output native array that holds the set of horizontal error for each target position.
        /// </summary>
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> errorBuffer;

        /// <summary>
        /// Output native array that holds the set of positions that were used to generate the height value.
        /// </summary>
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> candidateLocationBuffer;

        /// <summary>
        /// Output native array that holds the set of steps that were executed to find the height.
        /// </summary>
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> stepCountBuffer;

        /// <summary>
        /// Function that evaluates the height for a given element in the input buffer.
        /// </summary>
        /// <param name="index">The index of the element that the function will process.</param>
        public void Execute(int index)
        {
            // Fill the search parameters
            WaterSearchParameters wsp = new WaterSearchParameters();
            wsp.targetPosition = targetPositionBuffer[index];
            wsp.startPosition = startPositionBuffer[index];
            wsp.error = error;
            wsp.maxIterations = maxIterations;

            // Do the search
            WaterSearchResult wsr = new WaterSearchResult();
            HDRenderPipeline.FindWaterSurfaceHeight(simSearchData, wsp, out wsr);

            // Output the result to the output buffers
            heightBuffer[index] = wsr.height;
            errorBuffer[index] = wsr.error;
            candidateLocationBuffer[index] = wsr.candidateLocation;
            stepCountBuffer[index] = wsr.numIterations;
        }
    }
}
