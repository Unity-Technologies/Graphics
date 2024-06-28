using System;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class WaterSurface
    {
        /// <summary>Fade modes</summary>
        public enum FadeMode
        {
            /// <summary>No fading</summary>
            None,
            /// <summary>Automatic fading</summary>
            Automatic,
            /// <summary>Custom fading</summary>
            Custom
        }

        #region Swell/Agitation
        /// <summary>
        ///
        /// </summary>
        [Range(WaterConsts.k_SwellMinPatchSize, WaterConsts.k_SwellMaxPatchSize)]
        public float repetitionSize = 500.0f;

        /// <summary>
        ///
        /// </summary>
        public float largeOrientationValue = 0.0f;

        /// <summary>
        ///
        /// </summary>
        [Range(0, WaterConsts.k_SwellMaximumWindSpeed)]
        public float largeWindSpeed = 30.0f;

        /// <summary>
        ///
        /// </summary>
        [Range(0, 1.0f)]
        public float largeChaos = 0.8f;

        /// <summary>
        ///
        /// </summary>
        [Range(0, 1.0f)]
        public float largeBand0Multiplier = 1.0f;

        /// <summary>
        ///
        /// </summary>
        public FadeMode largeBand0FadeMode = FadeMode.Automatic;

        /// <summary>
        ///
        /// </summary>
        public float largeBand0FadeStart = 1500.0f;

        /// <summary>
        ///
        /// </summary>
        public float largeBand0FadeDistance = 3000.0f;

        /// <summary>
        ///
        /// </summary>
        [Range(0, 1.0f)]
        public float largeBand1Multiplier = 1.0f;

        /// <summary>
        ///
        /// </summary>
        public FadeMode largeBand1FadeMode = FadeMode.Automatic;

        /// <summary>
        ///
        /// </summary>
        public float largeBand1FadeStart = 300.0f;

        /// <summary>
        ///
        /// </summary>
        public float largeBand1FadeDistance = 800.0f;
        #endregion

        #region Ripples
        /// <summary>
        /// When enabled, the water system allows you to simulate and render a ripples simulation for finer details.
        /// </summary>
        public bool ripples = true;

        /// <summary>
        ///
        /// </summary>
        public WaterPropertyOverrideMode ripplesMotionMode = WaterPropertyOverrideMode.Inherit;

        /// <summary>
        ///
        /// </summary>
        public float ripplesOrientationValue = 0.0f;

        /// <summary>
        ///
        /// </summary>
        [Range(0, WaterConsts.k_RipplesMaxWindSpeed)]
        public float ripplesWindSpeed = 8.0f;

        /// <summary>
        ///
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float ripplesChaos = 0.8f;

        /// <summary>
        ///
        /// </summary>
        public FadeMode ripplesFadeMode = FadeMode.Automatic;

        /// <summary>
        ///
        /// </summary>
        public float ripplesFadeStart = 50.0f;

        /// <summary>
        ///
        /// </summary>
        public float ripplesFadeDistance = 200.0f;
        #endregion

        /// <summary>Used to sync different water surfaces simulation time, for example across network.</summary>
        public DateTime simulationStart
        {
            get
            {
                float timeScale = Time.timeScale * timeMultiplier;
                if (timeScale == 0.0f) timeScale = 1.0f;

                return DateTime.Now - TimeSpan.FromSeconds(simulation != null ? simulation.simulationTime / timeScale : 0.0f);
            }
            set
            {
                TimeSpan elapsed = DateTime.Now - value;
                if (simulation != null)
                    simulation.simulationTime = (float)elapsed.TotalSeconds * Time.timeScale * timeMultiplier;
            }
        }

        /// <summary>Current simulation time in seconds.</summary>
        public float simulationTime
        {
            get
            {
                return simulation?.simulationTime ?? 0.0f;
            }
            set
            {
                if (simulation != null)
                    simulation.simulationTime = value;
            }
        }

        internal int numActiveBands => WaterSystem.EvaluateBandCount(surfaceType, ripples);

        // Optional CPU simulation data
        internal AsyncTextureSynchronizer<half4> displacementBufferSynchronizer = new AsyncTextureSynchronizer<half4>(GraphicsFormat.R16G16B16A16_SFloat);

        // Internal simulation data
        internal WaterSimulationResources simulation = null;

        internal void CheckResources(int bandResolution, bool gpuReadback)
        {
            int bandCount = numActiveBands;
            bool foam = HasSimulationFoam();

            // If the previously existing resources are not valid, just release them
            if (simulation != null && !simulation.ValidResources(bandResolution, bandCount, foam))
            {
                simulation.ReleaseSimulationResources();
                simulation = null;
            }

            // Will we need to enable the CPU simulation?
            bool cpuSimulationActive = scriptInteractions && !gpuReadback;

            // If the resources have not been allocated for this water surface, allocate them
            if (simulation == null)
            {
                // Create the simulation resources
                simulation = new WaterSimulationResources();

                // Initialize for the allocation
                simulation.InitializeSimulationResources(bandResolution, bandCount, foam);

                // GPU buffers should always be allocated
                simulation.AllocateSimulationBuffersGPU();

                // CPU buffers should be allocated only if required
                if (cpuSimulationActive)
                    simulation.AllocateSimulationBuffersCPU();

                CreatePropertyBlock();
            }

            // If the resources are no longer used, release them
            if (!cpuSimulationActive && simulation.cpuBuffers != null)
            {
                simulation.ReleaseSimulationBuffersCPU();
                simulation.cpuSpectrumValid = false;
            }

            // One more case that we need check here is that if the CPU became required
            if (cpuSimulationActive && simulation.cpuBuffers == null)
            {
                simulation.AllocateSimulationBuffersCPU();
                simulation.cpuSpectrumValid = false;
            }

            // Evaluate the spectrum parameters
            WaterSpectrumParameters spectrum = EvaluateSpectrumParams(surfaceType);

            // If the spectrum defining data changed, we need to invalidate the buffers
            if (simulation.spectrum != spectrum)
            {
                // Mark the spectrums as invalid and assign the new one
                simulation.gpuSpectrumValid = false;
                simulation.cpuSpectrumValid = false;
                simulation.spectrum = spectrum;
            }

            // TODO: Handle properly the change of resolution to be able to not do this every frame.
            simulation.cpuSpectrumValid = false;

            // Re-evaluate the simulation data
            simulation.rendering = EvaluateRenderingParams(surfaceType);
        }

        internal static void EvaluateWaterSurfaceMatrices(bool quad, bool customMesh, Vector3 position, Quaternion rotation, ref float4x4 waterToWorld, ref float4x4 worldToWater, ref float4x4 worldToWater2)
        {
            // Evaluate the right transform based on the type of surface
            if (customMesh)
            {
                waterToWorld = worldToWater = Matrix4x4.identity;
                worldToWater2 = math.inverse(Matrix4x4.TRS(position, rotation, Vector3.one));
            }
            else
            {
                waterToWorld = Matrix4x4.TRS(position, rotation, Vector3.one);
                worldToWater = worldToWater2 = math.inverse(waterToWorld);
            }
        }

        // Function that evaluates the spectrum data for the ocean/sea/lake case
        WaterSpectrumParameters EvaluateSpectrumParams(WaterSurfaceType type)
        {
            WaterSpectrumParameters spectrum = new WaterSpectrumParameters();
            switch (type)
            {
                case WaterSurfaceType.OceanSeaLake:
                {
                    // Compute the patch size of the biggest swell band
                    float swellPatchSize = repetitionSize;

                    // We need to evaluate the radio between the first and second band
                    float swellSecondBandRatio = WaterSystem.EvaluateSwellSecondPatchSize(swellPatchSize);

                    // Set the patch groups
                    spectrum.patchGroup.x = 0;
                    spectrum.patchGroup.y = 0;
                    spectrum.patchGroup.z = ripplesMotionMode == WaterPropertyOverrideMode.Inherit ? 0 : 1;

                    // Deduce the patch sizes from the max patch size for the swell
                    spectrum.patchSizes.x = swellPatchSize;
                    spectrum.patchSizes.y = swellPatchSize / swellSecondBandRatio;
                    spectrum.patchSizes.z = WaterConsts.k_RipplesBandSize;

                    // Keep track of the directionality is used
                    float largeAngle = WaterSystem.NormalizeAngle(largeOrientationValue);
                    float ripplesAngle = WaterSystem.NormalizeAngle(ripplesOrientationValue);
                    spectrum.patchOrientation.x = largeAngle;
                    spectrum.patchOrientation.y = largeAngle;
                    spectrum.patchOrientation.z = ripplesMotionMode == WaterPropertyOverrideMode.Inherit ? largeAngle : ripplesAngle;

                    // Set the patch groups
                    spectrum.groupOrientation.x = spectrum.patchOrientation.x;
                    spectrum.groupOrientation.y = spectrum.patchOrientation.z;

                    // Wind speed per band
                    spectrum.patchWindSpeed.x = largeWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;
                    spectrum.patchWindSpeed.y = largeWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;
                    spectrum.patchWindSpeed.z = ripplesWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Direction dampener
                    spectrum.patchWindDirDampener.x = largeChaos;
                    spectrum.patchWindDirDampener.y = largeChaos;
                    spectrum.patchWindDirDampener.z = ripplesChaos;
                }
                break;
                case WaterSurfaceType.River:
                {
                    // Set the patch groups
                    spectrum.patchGroup.x = 0;
                    spectrum.patchGroup.y = ripplesMotionMode == WaterPropertyOverrideMode.Inherit ? 0 : 1;

                    // Deduce the patch sizes from the max patch size for the swell
                    spectrum.patchSizes.x = repetitionSize;
                    spectrum.patchSizes.y = WaterConsts.k_RipplesBandSize;

                    // Wind speed per band
                    spectrum.patchWindSpeed.x = largeWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;
                    spectrum.patchWindSpeed.y = ripplesWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Keep track of the directionality is used
                    float largeAngle = WaterSystem.NormalizeAngle(largeOrientationValue);
                    float ripplesAngle = WaterSystem.NormalizeAngle(ripplesOrientationValue);
                    spectrum.patchOrientation.x = largeAngle;
                    spectrum.patchOrientation.y = ripplesMotionMode == WaterPropertyOverrideMode.Inherit ? largeAngle : ripplesAngle;

                    // Set the patch groups
                    spectrum.groupOrientation.x = spectrum.patchOrientation.x;
                    spectrum.groupOrientation.y = spectrum.patchOrientation.y;

                    // Direction dampener
                    spectrum.patchWindDirDampener.x = largeChaos;
                    spectrum.patchWindDirDampener.y = ripplesChaos;
                }
                break;
                case WaterSurfaceType.Pool:
                {
                    // Set the patch groups
                    spectrum.patchGroup.x = 1;

                    // Deduce the patch sizes from the max patch size for the swell
                    spectrum.patchSizes.x = WaterConsts.k_RipplesBandSize;

                    // Wind speed per band
                    spectrum.patchWindSpeed.x = ripplesWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Keep track of the directionality is used
                    spectrum.patchOrientation.x = WaterSystem.NormalizeAngle(ripplesOrientationValue);

                    // Set the patch groups
                    spectrum.groupOrientation.x = spectrum.patchOrientation.x;
                    spectrum.groupOrientation.y = spectrum.patchOrientation.x;

                    // Direction dampener
                    spectrum.patchWindDirDampener.x = ripplesChaos;
                }
                break;
            }

            return spectrum;
        }

        void ComputeDistanceFade(ref WaterRenderingParameters rendering, int index, FadeMode mode, float customStart, float customDistance)
        {
            if (mode == FadeMode.None)
            {
                rendering.patchFadeA[index] = 0.0f;
                rendering.patchFadeB[index] = 1.0f;
                rendering.maxFadeDistance = float.MaxValue;
            }
            else
            {
                if (mode == FadeMode.Automatic)
                {
                    // For an ocean with repetition size 500, will give following results
                    // band0:   start = 1500 / distance = 6000
                    // band1:   start = 339  / distance = 1357
                    // ripples: start = 67   / distance = 271
                    float factor = (index == 0) ? 3 : (index == 1 ? 5 : simulation.spectrum.patchSizes[1] / WaterConsts.k_RipplesBandSize);
                    customStart = factor * simulation.spectrum.patchSizes[index];
                    customDistance = 4 * customStart;
                }

                rendering.patchFadeA[index] = -1.0f / Mathf.Max(customDistance, 0.001f);
                rendering.patchFadeB[index] = 1.0f - customStart * rendering.patchFadeA[index];
                rendering.maxFadeDistance = Mathf.Max(rendering.maxFadeDistance, customStart + customDistance);
            }
        }

        WaterRenderingParameters EvaluateRenderingParams(WaterSurfaceType type)
        {
            WaterRenderingParameters rendering = new WaterRenderingParameters();

            // Propagate the simulation time to the rendering structure
            rendering.simulationTime = simulation.simulationTime;
            rendering.maxFadeDistance = 0.0f;

            switch (type)
            {
                case WaterSurfaceType.OceanSeaLake:
                {
                    // Deduce the patch sizes from the max patch size for the swell
                    rendering.patchAmplitudeMultiplier.x = largeBand0Multiplier;
                    rendering.patchAmplitudeMultiplier.y = largeBand1Multiplier;
                    rendering.patchAmplitudeMultiplier.z = 1.0f;

                    // Keep track of the directionality is used
                    float swellCurrentSpeed = largeCurrentSpeedValue * WaterConsts.k_KilometerPerHourToMeterPerSecond;
                    rendering.patchCurrentSpeed.x = swellCurrentSpeed;
                    rendering.patchCurrentSpeed.y = swellCurrentSpeed;
                    rendering.patchCurrentSpeed.z = ripplesMotionMode == WaterPropertyOverrideMode.Inherit ? swellCurrentSpeed : ripplesCurrentSpeedValue * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Fade parameters
                    ComputeDistanceFade(ref rendering, 0, largeBand0FadeMode, largeBand0FadeStart, largeBand0FadeDistance);
                    ComputeDistanceFade(ref rendering, 1, largeBand1FadeMode, largeBand1FadeStart, largeBand1FadeDistance);
                    if (ripples) ComputeDistanceFade(ref rendering, 2, ripplesFadeMode, ripplesFadeStart, ripplesFadeDistance);
                }
                break;
                case WaterSurfaceType.River:
                {
                    // Deduce the patch sizes from the max patch size for the swell
                    rendering.patchAmplitudeMultiplier.x = largeBand0Multiplier;
                    rendering.patchAmplitudeMultiplier.y = ripples ? 1.0f : 0.0f;

                    // Keep track of the directionality is used
                    rendering.patchCurrentSpeed.x = largeCurrentSpeedValue * WaterConsts.k_KilometerPerHourToMeterPerSecond;
                    rendering.patchCurrentSpeed.y = ripplesMotionMode == WaterPropertyOverrideMode.Inherit ? rendering.patchCurrentSpeed.x : ripplesCurrentSpeedValue * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Fade parameters
                    ComputeDistanceFade(ref rendering, 0, largeBand0FadeMode, largeBand0FadeStart, largeBand0FadeDistance);
                    if (ripples) ComputeDistanceFade(ref rendering, 1, ripplesFadeMode, ripplesFadeStart, ripplesFadeDistance);
                }
                break;
                case WaterSurfaceType.Pool:
                {
                    // Deduce the patch sizes from the max patch size for the swell
                    rendering.patchAmplitudeMultiplier.x = 1.0f;
                    rendering.patchAmplitudeMultiplier.y = 0.0f;

                    // Keep track of the directionality is used
                    rendering.patchCurrentSpeed.x = ripplesCurrentSpeedValue * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Fade parameters
                    ComputeDistanceFade(ref rendering, 0, ripplesFadeMode, ripplesFadeStart, ripplesFadeDistance);
                }
                break;
            }

            // Make sure the matrices are evaluated
            EvaluateWaterSurfaceMatrices(IsQuad(), IsCustomMesh(), transform.position, transform.rotation, ref rendering.waterToWorldMatrix, ref rendering.worldToWaterMatrix, ref rendering.worldToWaterMatrixCustom);
            return rendering;
        }

        internal void ReleaseSimulationResources()
        {
            displacementBufferSynchronizer.ReleaseATSResources();

            // Make sure to release the resources if they have been created (before HDRP destroys them)
            if (simulation != null)
                simulation.ReleaseSimulationResources();
            simulation = null;
        }
    }
}
