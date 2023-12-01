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
        internal float2 deformationRegionScale;
        internal float2 deformationRegionOffset;
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

    public partial class HDRenderPipeline
    {
        static void EvaluateWaterDisplacement(WaterSimSearchData wsd, float3 positionAWS, bool includeSimulation, bool includeDeformation, out float3 totalDisplacement, out float2 dir)
        {
            // Compute the simulation coordinates
            float2 uv = float2(positionAWS.x, positionAWS.z);
            float3 waterMask = EvaluateWaterMask(wsd, uv);

            // Will hold the total displacement
            totalDisplacement = 0.0f;
            dir = OrientationToDirection(wsd.spectrum.patchOrientation.x);

            if (includeSimulation)
            {
                // The behavior is different if we have a current map or we don't
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
                    float3 totalDisplacement0 = EvaluateWaterSimulation(wsd, finalSimCoord, waterMask);

                    // Sample the simulation (second time)
                    ComputeWaterUVs(wsd, gr0uv.zw, out gr0SimCoord);
                    ComputeWaterUVs(wsd, gr1uv.zw, out gr1SimCoord);
                    AggregateWaterSimCoords(wsd, gr0SimCoord, gr1SimCoord, gr0CurrentData, gr1CurrentData, false, out finalSimCoord);
                    float3 totalDisplacement1 = EvaluateWaterSimulation(wsd, finalSimCoord, waterMask);

                    // Combine both contributions
                    totalDisplacement = totalDisplacement0 + totalDisplacement1;
                }
                else
                {
                    WaterSimCoord coords;
                    ComputeWaterUVs(wsd, uv, out coords);
                    totalDisplacement = EvaluateWaterSimulation(wsd, coords, waterMask);
                }
            }

            // We only apply the choppiness tot he first two bands, doesn't behave very good past those
            totalDisplacement.yz *= WaterConsts.k_WaterMaxChoppinessValue;

            // The vertical displacement is stored in the X channel and the XZ displacement in the YZ channel
            totalDisplacement =  float3(-totalDisplacement.y, totalDisplacement.x, -totalDisplacement.z);

            // Apply the deformation if required
            if (includeDeformation && wsd.activeDeformation)
                totalDisplacement.y += EvaluateDeformers(wsd, positionAWS + totalDisplacement);
        }

        internal struct WaterSimulationTapData
        {
            public float3 currentDisplacement;
            public float2 direction;
            public float3 displacedPoint;
            public float2 offset;
            public float distance;
            public float height;
        }

        static WaterSimulationTapData EvaluateDisplacementData(WaterSimSearchData wsd, float3 currentLocation, float3 referencePosition, bool includeSimulation, bool includeDeformation)
        {
            WaterSimulationTapData data;

            // Evaluate the displacement at the current point
            EvaluateWaterDisplacement(wsd, currentLocation, includeSimulation, includeDeformation, out data.currentDisplacement, out data.direction);

            // Evaluate the complete position
            data.displacedPoint = currentLocation + data.currentDisplacement;

            // Evaluate the distance to the reference point
            data.offset = referencePosition.xz - data.displacedPoint.xz;

            // Length of the offset vector
            data.distance = Mathf.Sqrt(data.offset.x * data.offset.x + data.offset.y * data.offset.y);

            // Simulation height of the position of the offset vector
            data.height = data.currentDisplacement.y;
            return data;
        }

        internal static void ProjectPointOnWaterSurface(WaterSimSearchData wsd,
                                                    WaterSearchParameters wsp,
                                                    out WaterSearchResult sr)
        {
            // Convert the target position to the water object space
            float3 targetPositionOS = mul(wsd.rendering.worldToWaterMatrix, float4(wsp.targetPositionWS, 1.0f)).xyz;

            // Convert the target position to the water object space
            float3 startPositionOS = mul(wsd.rendering.worldToWaterMatrix, float4(wsp.startPositionWS, 1.0f)).xyz;

            // Initialize the search data
            WaterSimulationTapData tapData = EvaluateDisplacementData(wsd, startPositionOS, targetPositionOS, !wsp.excludeSimulation, wsp.includeDeformation);
            float2 stepSize = tapData.offset;
            sr.error = tapData.distance;
            float currentHeight = tapData.height;
            sr.candidateLocationWS = startPositionOS;
            sr.currentDirectionWS = float3(tapData.direction.x, 0, tapData.direction.y);
            sr.numIterations = 0;

            // Go through the steps until we found a position that satisfies our constraints
            while (sr.numIterations < wsp.maxIterations)
            {
                // Is the point close enough to target position?
                if (sr.error < wsp.error)
                    break;

                // Reset the search progress flag
                bool progress = false;

                float3 candidateLocation = sr.candidateLocationWS + new float3(stepSize.x, 0, stepSize.y);
                tapData = EvaluateDisplacementData(wsd, candidateLocation, targetPositionOS, !wsp.excludeSimulation, wsp.includeDeformation);
                if (tapData.distance < sr.error)
                {
                    sr.candidateLocationWS = candidateLocation;
                    stepSize = tapData.offset;
                    sr.error = tapData.distance;
                    currentHeight = tapData.height;
                    sr.currentDirectionWS = float3(tapData.direction.x, 0, tapData.direction.y);
                    progress = true;
                }

                // If we didn't make any progress in this step, this means out steps are probably too big make them smaller
                if (!progress)
                    stepSize *= 0.5f;

                sr.numIterations++;
            }

            // Convert the positions from OS to world space
            sr.projectedPositionWS = mul(wsd.rendering.waterToWorldMatrix, float4(targetPositionOS.x, currentHeight, targetPositionOS.z, 1.0f)).xyz;
            sr.candidateLocationWS = mul(wsd.rendering.waterToWorldMatrix, float4(sr.candidateLocationWS, 1.0f)).xyz;
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
        /// Output native array that holds the set of world space position projected on the water surface along the up vector of the water surface.
        /// </summary>
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> projectedPositionWSBuffer;

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

            // Do the search
            WaterSearchResult wsr = new WaterSearchResult();
            HDRenderPipeline.ProjectPointOnWaterSurface(simSearchData, wsp, out wsr);

            // Output the result to the output buffers
            errorBuffer[index] = wsr.error;
            candidateLocationWSBuffer[index] = wsr.candidateLocationWS;
            projectedPositionWSBuffer[index] = wsr.projectedPositionWS;
            directionBuffer[index] = wsr.currentDirectionWS;
            stepCountBuffer[index] = wsr.numIterations;
        }
    }
}
