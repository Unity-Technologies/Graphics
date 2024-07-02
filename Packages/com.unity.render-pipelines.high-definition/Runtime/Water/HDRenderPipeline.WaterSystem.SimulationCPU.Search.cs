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
        #region Simulation
        internal float simulationTime;
        internal int simulationRes;
        internal bool cpuSimulation;
        [ReadOnly] internal NativeArray<float4> displacementDataCPU;
        [ReadOnly] internal NativeArray<half4> displacementDataGPU;
        internal WaterSpectrumParameters spectrum;
        internal WaterRenderingParameters rendering;
        internal int activeBandCount;

        internal bool decalWorkflow;
        internal float2 decalRegionCenter;
        internal float2 decalRegionScale;
        #endregion

        #region Water Mask
        internal bool activeMask;
        [ReadOnly] internal NativeArray<uint> maskBuffer;
        internal TextureWrapMode maskWrapModeU;
        internal TextureWrapMode maskWrapModeV;
        internal int2 maskResolution;
        internal float2 maskRemap;
        internal float2 maskScale;
        internal float2 maskOffset;
        #endregion

        #region Deformation
        internal bool activeDeformation;
        [ReadOnly] internal NativeArray<half> deformationBuffer;
        internal int2 deformationResolution;
        internal float2 waterForwardXZ;
        #endregion

        #region Current Map
        // Group 0
        internal bool activeGroup0CurrentMap;
        [ReadOnly] internal NativeArray<uint> group0CurrentMap;
        internal TextureWrapMode group0CurrentMapWrapModeU;
        internal TextureWrapMode group0CurrentMapWrapModeV;
        internal int2 group0CurrentMapResolution;
        internal float2 group0CurrentRegionScale;
        internal float2 group0CurrentRegionOffset;
        internal float group0CurrentMapInfluence;

        // Group 1
        internal bool activeGroup1CurrentMap;
        [ReadOnly] internal NativeArray<uint> group1CurrentMap;
        internal TextureWrapMode group1CurrentMapWrapModeU;
        internal TextureWrapMode group1CurrentMapWrapModeV;
        internal int2 group1CurrentMapResolution;
        internal float2 group1CurrentRegionScale;
        internal float2 group1CurrentRegionOffset;
        internal float group1CurrentMapInfluence;

        // Common
        [ReadOnly] internal NativeArray<float4> sectorData;
        #endregion
    }

    // NOTE: make sure that any new feature on the CPU requests are matched on the VFX sammple node

    /// <summary>
    /// Structure that holds the input parameters of the search.
    /// </summary>
    public struct WaterSearchParameters
    {
        /// <summary>
        /// Target position in world space that the search needs to evaluate the height at.
        /// </summary>
        public float3 targetPositionWS;

        /// <summary>
        /// World Space Position that the search starts from. Can be used as a hint for the search algorithm.
        /// </summary>
        public float3 startPositionWS;

        /// <summary>
        /// Target error value at which the algorithm should stop.
        /// </summary>
        public float error;

        /// <summary>
        /// Number of iterations of the search algorithm.
        /// </summary>
        public int maxIterations;

        /// <summary>
        /// Specifies if the search should include deformation.
        /// </summary>
        public bool includeDeformation;

        /// <summary>
        /// Specifies if the search should ignore the simulation.
        /// </summary>
        public bool excludeSimulation;

        /// <summary>
        /// Specifies if the search should compute the normal of the water surface at the projected position.
        /// </summary>
        public bool outputNormal;
    }

    /// <summary>
    /// Structure that holds the output parameters of the search.
    /// </summary>
    public struct WaterSearchResult
    {
        /// <summary>
        /// Returns the world space position projected on the water surface along the up vector of the water surface.
        /// </summary>
        public float3 projectedPositionWS;

        /// <summary>
        /// If requested in the search parameters, returns the world space normal of the water surface at the projected position.
        /// </summary>
        public float3 normalWS;

        /// <summary>
        /// Location of the 3D world space point that has been displaced to the target positions
        /// </summary>
        public float3 candidateLocationWS;

        /// <summary>
        /// Vector that gives the local current orientation (if any).
        /// </summary>
        public float3 currentDirectionWS;

        /// <summary>
        /// Number of iterations of the search algorithm to find the height value.
        /// </summary>
        public int numIterations;

        /// <summary>
        /// Horizontal error value of the search algorithm.
        /// </summary>
        public float error;
    }

    partial class WaterSystem
    {
        static void EvaluateSimulationDisplacement(WaterSimSearchData wsd, float3 positionAWS, bool includeSimulation, out float2 horizontalDisplacement, out float2 dir, out float3 verticalDisplacements)
        {
            dir = OrientationToDirection(wsd.spectrum.patchOrientation.x);

            if (includeSimulation)
            {
                // Compute the simulation coordinates
                float2 uv = float2(positionAWS.x, positionAWS.z);

                if (wsd.activeGroup0CurrentMap || wsd.activeGroup1CurrentMap)
                {
                    // Read the current data
                    CurrentData gr0CurrentData, gr1CurrentData;
                    EvaluateGroup0CurrentData(wsd, uv, out gr0CurrentData);
                    EvaluateGroup1CurrentData(wsd, uv, out gr1CurrentData);

                    // Rotate locally the current direction
                    float cosAngle = cos(gr0CurrentData.angle);
                    float sinAngle = sin(gr0CurrentData.angle);
                    dir = float2(dir.x * cosAngle - dir.y * sinAngle, dir.y * cosAngle + dir.x * sinAngle);

                    // Compute the simulation coordinates
                    float4 gr0uv, gr1uv;
                    SwizzleSamplingCoordinates(uv, gr0CurrentData.quadrant, wsd.sectorData, out gr0uv);
                    SwizzleSamplingCoordinates(uv, gr1CurrentData.quadrant, wsd.sectorData, out gr1uv);

                    // Compute the 2 simulation coordinates
                    WaterSimCoord gr0SimCoord;
                    WaterSimCoord gr1SimCoord;
                    WaterSimCoord finalSimCoord;

                    // Sample the simulation (first time)
                    ComputeWaterUVs(wsd, gr0uv.xy, out gr0SimCoord);
                    ComputeWaterUVs(wsd, gr1uv.xy, out gr1SimCoord);
                    AggregateWaterSimCoords(wsd, gr0SimCoord, gr1SimCoord, gr0CurrentData, gr1CurrentData, true, out finalSimCoord);
                    EvaluateWaterSimulation(wsd, finalSimCoord, out var horizontalDisplacement0, out var verticalDisplacements0);

                    // Sample the simulation (second time)
                    ComputeWaterUVs(wsd, gr0uv.zw, out gr0SimCoord);
                    ComputeWaterUVs(wsd, gr1uv.zw, out gr1SimCoord);
                    AggregateWaterSimCoords(wsd, gr0SimCoord, gr1SimCoord, gr0CurrentData, gr1CurrentData, false, out finalSimCoord);
                    EvaluateWaterSimulation(wsd, finalSimCoord, out var horizontalDisplacement1, out var verticalDisplacements1);

                    // Combine both contributions
                    horizontalDisplacement = horizontalDisplacement0 + horizontalDisplacement1;
                    verticalDisplacements = verticalDisplacements0 + verticalDisplacements1;
                }
                else
                {
                    ComputeWaterUVs(wsd, uv, out var coords);
                    EvaluateWaterSimulation(wsd, coords, out horizontalDisplacement, out verticalDisplacements);
                }

                horizontalDisplacement *= -WaterConsts.k_WaterMaxChoppinessValue;
            }
            else
            {

                horizontalDisplacement = 0.0f;
                verticalDisplacements = 0.0f;
            }
        }

        static void EvaluateVerticalDisplacement(WaterSimSearchData wsd, WaterSearchParameters wsp, float3 positionOS, float3 verticalDisplacements, out float verticalDisplacement)
        {
            float3 positionAWS = mul(wsd.rendering.waterToWorldMatrix, float4(positionOS, 1.0f)).xyz;

            float3 waterMask = EvaluateWaterMask(wsd, positionAWS);

            verticalDisplacement = dot(verticalDisplacements, waterMask);

            // Apply the deformation if required
            if (wsp.includeDeformation && wsd.activeDeformation)
            {
                float deformation = EvaluateDeformers(wsd, positionAWS);
                if (!float.IsNaN(deformation))
                    verticalDisplacement += deformation;
            }
        }

        static float3 LoadDisplacement(WaterSimSearchData wsd, int2 coord, int bandIdx)
        {
            return wsd.cpuSimulation ? LoadTexture2DArray(wsd.displacementDataCPU, coord, bandIdx, wsd.simulationRes).xyz :
                LoadTexture2DArray(wsd.displacementDataGPU, coord, bandIdx, wsd.simulationRes).xyz;
        }

        static float2 EvaluateWaterNormal(WaterSimSearchData wsd, WaterSimCoord waterCoord, float3 waterMask)
        {
            float2 surfaceGradient = 0;

            for (int bandIdx = 0; bandIdx < wsd.activeBandCount; bandIdx++)
            {
                PatchSimData data = bandIdx == 0 ? waterCoord.data0 : (bandIdx == 1 ? waterCoord.data1 : waterCoord.data2);
                int2 coord = (int2)(data.uv * wsd.simulationRes);

                // Get the displacement we need for the evaluate (and re-order them)
                // Note: we could do some sort of bilinear here to filter the result
                float3 displacementCenter = ShuffleDisplacement(LoadDisplacement(wsd, coord, bandIdx));
                float3 displacementRight = ShuffleDisplacement(LoadDisplacement(wsd, coord + int2(1, 0), bandIdx));
                float3 displacementUp = ShuffleDisplacement(LoadDisplacement(wsd, coord + int2(0, 1), bandIdx));

                // Evaluate the displacement normalization factor and pixel size
                float pixelSize = wsd.spectrum.patchSizes[bandIdx] / (float)wsd.simulationRes;

                // We evaluate the displacement without the choppiness as it doesn't behave properly for distance surfaces
                EvaluateDisplacedPoints(displacementCenter, displacementRight, displacementUp, wsd.rendering.patchAmplitudeMultiplier[bandIdx], pixelSize,
                    out var p0, out var p1, out var p2);

                // Compute the surface gradients of this band
                float2 additionalData = EvaluateSurfaceGradients(p0, p1, p2);
                additionalData.xy *= data.blend * waterMask[bandIdx];

                // Swizzle the displacement
                additionalData.xy = float2(dot(additionalData.xy, data.swizzle.xy), dot(additionalData.xy, data.swizzle.zw));

                // Evaluate the surface gradient
                surfaceGradient += additionalData.xy;
            }

            return surfaceGradient;
        }

        static float3 EvaluateNormal(WaterSimSearchData wsd, WaterSearchParameters wsp, float3 positionAWS)
        {
            float2 surfaceGradient = 0;
            float2 uv = float2(positionAWS.x, positionAWS.z);
            float3 waterMask = EvaluateWaterMask(wsd, positionAWS);

            if (!wsp.excludeSimulation)
            {
                if (wsd.activeGroup0CurrentMap || wsd.activeGroup1CurrentMap)
                {
                    // Read the current data
                    CurrentData gr0CurrentData, gr1CurrentData;
                    EvaluateGroup0CurrentData(wsd, uv, out gr0CurrentData);
                    EvaluateGroup1CurrentData(wsd, uv, out gr1CurrentData);

                    // Compute the simulation coordinates
                    float4 gr0uv, gr1uv;
                    SwizzleSamplingCoordinates(uv, gr0CurrentData.quadrant, wsd.sectorData, out gr0uv);
                    SwizzleSamplingCoordinates(uv, gr1CurrentData.quadrant, wsd.sectorData, out gr1uv);

                    // Compute the 2 simulation coordinates
                    WaterSimCoord gr0SimCoord;
                    WaterSimCoord gr1SimCoord;
                    WaterSimCoord finalSimCoord;

                    // Sample the simulation (first time)
                    ComputeWaterUVs(wsd, gr0uv.xy, out gr0SimCoord);
                    ComputeWaterUVs(wsd, gr1uv.xy, out gr1SimCoord);
                    AggregateWaterSimCoords(wsd, gr0SimCoord, gr1SimCoord, gr0CurrentData, gr1CurrentData, true, out finalSimCoord);
                    float2 surfaceGradient0 = EvaluateWaterNormal(wsd, finalSimCoord, waterMask);

                    // Sample the simulation (second time)
                    ComputeWaterUVs(wsd, gr0uv.zw, out gr0SimCoord);
                    ComputeWaterUVs(wsd, gr1uv.zw, out gr1SimCoord);
                    AggregateWaterSimCoords(wsd, gr0SimCoord, gr1SimCoord, gr0CurrentData, gr1CurrentData, false, out finalSimCoord);
                    float2 surfaceGradient1 = EvaluateWaterNormal(wsd, finalSimCoord, waterMask);

                    // Combine both contributions
                    surfaceGradient = surfaceGradient0 + surfaceGradient1;
                }
                else
                {
                    ComputeWaterUVs(wsd, uv, out var waterCoord);
                    surfaceGradient = EvaluateWaterNormal(wsd, waterCoord, waterMask);
                }
            }

            if (wsp.includeDeformation)
                surfaceGradient += EvaluateDeformerNormal(wsd, wsp.targetPositionWS);

            return SurfaceGradientResolveNormal(float3(0, 1, 0), float3(surfaceGradient.x, 0, surfaceGradient.y));
        }

        internal struct WaterSimulationTapData
        {
            public float2 direction;
            public float2 offset;
            public float distance;
            public float2 horizontalDisplacement;
            public float3 verticalDisplacements;
        }

        static WaterSimulationTapData EvaluateDisplacementData(WaterSimSearchData wsd, float3 currentLocation, float3 referencePosition, bool includeSimulation)
        {
            WaterSimulationTapData data;

            // Evaluate the displacement at the current point
            EvaluateSimulationDisplacement(wsd, currentLocation, includeSimulation, out data.horizontalDisplacement, out data.direction, out data.verticalDisplacements);

            // Evaluate the distance to the reference point
            data.offset = (currentLocation.xz + data.horizontalDisplacement) - referencePosition.xz;
            data.distance = length(data.offset);

            return data;
        }

        internal static bool ProjectPointOnWaterSurface(WaterSimSearchData wsd,
                                                    WaterSearchParameters wsp,
                                                    ref WaterSearchResult sr)
        {
            // Convert the target position to the water object space
            float3 targetPositionOS = mul(wsd.rendering.worldToWaterMatrix, float4(wsp.targetPositionWS, 1.0f)).xyz;

            // Convert the target position to the water object space
            float3 startPositionOS = mul(wsd.rendering.worldToWaterMatrix, float4(wsp.startPositionWS, 1.0f)).xyz;

            // Initialize the search data
            WaterSimulationTapData tapData = EvaluateDisplacementData(wsd, startPositionOS, targetPositionOS, !wsp.excludeSimulation);
            if (float.IsNaN(tapData.distance))
                return false;

            float2 stepSize = tapData.offset;
            float3 currentVertical = tapData.verticalDisplacements;
            float3 currentPositionOS = startPositionOS;

            sr.error = tapData.distance;
            sr.currentDirectionWS = float3(tapData.direction.x, 0, tapData.direction.y);
            sr.numIterations = 0;

            // Go through the steps until we found a position that satisfies our constraints
            while (sr.numIterations < wsp.maxIterations)
            {
                if (sr.error < wsp.error)
                    break;

                float3 candidateLocation = currentPositionOS - new float3(stepSize.x, 0, stepSize.y);
                tapData = EvaluateDisplacementData(wsd, candidateLocation, targetPositionOS, !wsp.excludeSimulation);
                if (float.IsNaN(tapData.distance))
                    return false;

                if (tapData.distance < sr.error)
                {
                    stepSize = tapData.offset;
                    currentVertical = tapData.verticalDisplacements;
                    currentPositionOS = candidateLocation;
                    sr.error = tapData.distance;
                    sr.currentDirectionWS = float3(tapData.direction.x, 0, tapData.direction.y);
                }
                else // If we didn't make any progress in this step, this means out steps are probably too big make them smaller
                    stepSize *= 0.5f;

                sr.numIterations++;
            }

            EvaluateVerticalDisplacement(wsd, wsp, currentPositionOS, currentVertical, out var currentHeight);

            // Convert the positions from OS to world space
            sr.projectedPositionWS = mul(wsd.rendering.waterToWorldMatrix, float4(targetPositionOS.x, currentHeight, targetPositionOS.z, 1.0f)).xyz;
            sr.candidateLocationWS = mul(wsd.rendering.waterToWorldMatrix, float4(currentPositionOS, 1.0f)).xyz;

            if (wsp.outputNormal)
            {
                float3 normalOS = EvaluateNormal(wsd, wsp, sr.candidateLocationWS);
                sr.normalWS = mul((float3x3)wsd.rendering.waterToWorldMatrix, normalOS);
            }

            return true;
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
        public NativeArray<float3> targetPositionWSBuffer;

        /// <summary>
        /// Native array that holds the set of "hint" position that the algorithm starts from.
        /// </summary>
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> startPositionWSBuffer;

        /// <summary>
        /// Target error value at which the algorithm should stop.
        /// </summary>
        public float error;

        /// <summary>
        /// Number of iterations of the search algorithm.
        /// </summary>
        public int maxIterations;

        /// <summary>
        /// Specifies if the search job should include the deformations
        /// </summary>
        public bool includeDeformation;

        /// <summary>
        /// Specifies if the search should ignore the simulation.
        /// </summary>
        public bool excludeSimulation;

        /// <summary>
        /// Specifies if the search should compute the normal of the water surface at the projected position.
        /// </summary>
        public bool outputNormal;

        /// <summary>
        /// Output native array that holds the set of world space position projected on the water surface along the up vector of the water surface.
        /// </summary>
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> projectedPositionWSBuffer;

        /// <summary>
        /// Output native array that holds the set of normals at projected positions.
        /// </summary>
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> normalWSBuffer;

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
        public NativeArray<float3> candidateLocationWSBuffer;

        /// <summary>
        /// Output native array that holds the set of direction for each target position;
        /// </summary>
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> directionBuffer;

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
            wsp.targetPositionWS = targetPositionWSBuffer[index];
            wsp.startPositionWS = startPositionWSBuffer[index];
            wsp.error = error;
            wsp.maxIterations = maxIterations;
            wsp.includeDeformation = includeDeformation;
            wsp.excludeSimulation = excludeSimulation;
            wsp.outputNormal = outputNormal;

            // Do the search
            var wsr = new WaterSearchResult();
            WaterSystem.ProjectPointOnWaterSurface(simSearchData, wsp, ref wsr);

            // Output the result to the output buffers
            errorBuffer[index] = wsr.error;
            candidateLocationWSBuffer[index] = wsr.candidateLocationWS;
            projectedPositionWSBuffer[index] = wsr.projectedPositionWS;
            directionBuffer[index] = wsr.currentDirectionWS;
            stepCountBuffer[index] = wsr.numIterations;

            if (outputNormal)
                normalWSBuffer[index] = wsr.normalWS;
        }
    }
}
