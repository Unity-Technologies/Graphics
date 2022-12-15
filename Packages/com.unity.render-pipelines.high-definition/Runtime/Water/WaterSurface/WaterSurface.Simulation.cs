using System;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class WaterSurface
    {
        #region Swell/Agitation
        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float repetitionSize = 500.0f;

        /// <summary>
        ///
        /// </summary>
        public float largeOrientationValue = 0.0f;

        /// <summary>
        ///
        /// </summary>
        public float largeWindSpeed = 30.0f;

        /// <summary>
        ///
        /// </summary>
        public float largeChaos = 0.8f;

        /// <summary>
        ///
        /// </summary>
        public float largeBand0Multiplier = 1.0f;

        /// <summary>
        ///
        /// </summary>
        public bool largeBand0FadeToggle = true;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float largeBand0FadeStart = 1500.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float largeBand0FadeDistance = 3000.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float largeBand1Multiplier = 1.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public bool largeBand1FadeToggle = true;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float largeBand1FadeStart = 300.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
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
        [Tooltip("")]
        public WaterPropertyOverrideMode ripplesMotionMode = WaterPropertyOverrideMode.Inherit;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float ripplesOrientationValue = 0.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float ripplesWindSpeed = 8.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float ripplesChaos = 0.8f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public bool ripplesFadeToggle = true;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float ripplesFadeStart = 50.0f;

        /// <summary>
        ///
        /// </summary>
        [Tooltip("")]
        public float ripplesFadeDistance = 200.0f;
        #endregion

        // Internal simulation data
        internal WaterSimulationResources simulation = null;

        internal void CheckResources(int bandResolution, int bandCount, bool cpuSimActive, out bool gpuSpectrumValid, out bool cpuSpectrumValid, out bool historyValid)
        {
            // By default we shouldn't need an update
            gpuSpectrumValid = true;
            cpuSpectrumValid = true;
            historyValid = true;

            // If the previously existing resources are not valid, just release them
            if (simulation != null && !simulation.ValidResources(bandResolution, bandCount))
            {
                simulation.ReleaseSimulationResources();
                simulation = null;
            }

            // Will we need to enable the CPU simulation?
            bool cpuSimulationActive = cpuSimActive && cpuSimulation;

            // If the resources have not been allocated for this water surface, allocate them
            if (simulation == null)
            {
                // In this case the CPU buffers are invalid and we need to rebuild them
                gpuSpectrumValid = false;
                cpuSpectrumValid = false;
                historyValid = false;

                // Create the simulation resources
                simulation = new WaterSimulationResources();

                // Initialize for the allocation
                simulation.InitializeSimulationResources(bandResolution, bandCount);

                // GPU buffers should always be allocated
                simulation.AllocateSimulationBuffersGPU();

                // CPU buffers should be allocated only if required
                if (cpuSimulationActive)
                    simulation.AllocateSimulationBuffersCPU();
            }

            // One more case that we need check here is that if the CPU became required
            if (!cpuSimulationActive && simulation.cpuBuffers != null)
            {
                simulation.ReleaseSimulationBuffersCPU();
                cpuSpectrumValid = false;
            }

            // One more case that we need check here is that if the CPU became required
            if (cpuSimulationActive && simulation.cpuBuffers == null)
            {
                simulation.AllocateSimulationBuffersCPU();
                cpuSpectrumValid = false;
            }

            // Evaluate the spectrum parameters
            WaterSpectrumParameters spectrum = EvaluateSpectrumParams(surfaceType);

            if (simulation.spectrum.numActiveBands != spectrum.numActiveBands)
            {
                historyValid = false;
            }

            // If the spectrum defining data changed, we need to invalidate the buffers
            if (simulation.spectrum != spectrum)
            {
                // Mark the spectrums as invalid and assign the new one
                gpuSpectrumValid = false;
                cpuSpectrumValid = false;
                simulation.spectrum = spectrum;
            }

            // TODO: Handle properly the change of resolution to be able to not do this every frame.
            cpuSpectrumValid = false;

            // Re-evaluate the simulation data
            simulation.rendering = EvaluateRenderingParams(surfaceType);
        }

        bool SpectrumParametersAreValid(WaterSpectrumParameters spectrum)
        {
            return (simulation.spectrum == spectrum);
        }

        internal static void EvaluateWaterSurfaceMatrices(bool instancedQuads, Vector3 position, Quaternion rotation, ref float4x4 waterToWorld, ref float4x4 worldToWater)
        {
            // Evaluate the right transform based on the type of surface
            waterToWorld = instancedQuads ? Matrix4x4.Translate(new Vector3(0.0f, position.y, 0.0f)):  Matrix4x4.TRS(position, rotation, Vector3.one);
            worldToWater = math.inverse(waterToWorld);
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
                    float swellSecondBandRatio = HDRenderPipeline.EvaluateSwellSecondPatchSize(swellPatchSize);

                    // Propagate the high frequency bands flag
                    spectrum.numActiveBands = ripples ? 3 : 2;

                    // Set the patch groups
                    spectrum.patchGroup.x = 0;
                    spectrum.patchGroup.y = 0;
                    spectrum.patchGroup.z = ripplesMotionMode == WaterPropertyOverrideMode.Inherit ? 0 : 1;

                    // Deduce the patch sizes from the max patch size for the swell
                    spectrum.patchSizes.x = swellPatchSize;
                    spectrum.patchSizes.y = swellPatchSize / swellSecondBandRatio;
                    spectrum.patchSizes.z = WaterConsts.k_RipplesBandSize;

                    // Keep track of the directionality is used
                    float largeAngle = HDRenderPipeline.NormalizeAngle(largeOrientationValue);
                    float ripplesAngle = HDRenderPipeline.NormalizeAngle(ripplesOrientationValue);
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
                    // Propagate the high frequency bands flag
                    spectrum.numActiveBands = ripples ? 2 : 1;

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
                    float largeAngle = HDRenderPipeline.NormalizeAngle(largeOrientationValue);
                    float ripplesAngle = HDRenderPipeline.NormalizeAngle(ripplesOrientationValue);
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
                    // Propagate the high frequency bands flag
                    spectrum.numActiveBands = 1;

                    // Set the patch groups
                    spectrum.patchGroup.x = 1;

                    // Deduce the patch sizes from the max patch size for the swell
                    spectrum.patchSizes.x = WaterConsts.k_RipplesBandSize;

                    // Wind speed per band
                    spectrum.patchWindSpeed.x = ripplesWindSpeed * WaterConsts.k_KilometerPerHourToMeterPerSecond;

                    // Keep track of the directionality is used
                    spectrum.patchOrientation.x = HDRenderPipeline.NormalizeAngle(ripplesOrientationValue);

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

        WaterRenderingParameters EvaluateRenderingParams(WaterSurfaceType type)
        {
            WaterRenderingParameters rendering = new WaterRenderingParameters();

            // Propagate the simulation time to the rendering structure
            rendering.simulationTime = simulation.simulationTime;

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
                    rendering.patchFadeStart.x = largeBand0FadeStart;
                    rendering.patchFadeStart.y = largeBand1FadeStart;
                    rendering.patchFadeStart.z = ripplesFadeStart;
                    rendering.patchFadeDistance.x = largeBand0FadeDistance;
                    rendering.patchFadeDistance.y = largeBand1FadeDistance;
                    rendering.patchFadeDistance.z = ripplesFadeDistance;
                    rendering.patchFadeValue.x = largeBand0FadeToggle ? 0.0f : 1.0f;
                    rendering.patchFadeValue.y = largeBand1FadeToggle ? 0.0f : 1.0f;
                    rendering.patchFadeValue.z = ripplesFadeToggle ? 0.0f : 1.0f;
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
                    rendering.patchFadeStart.x = largeBand0FadeStart;
                    rendering.patchFadeStart.y = ripplesFadeStart;
                    rendering.patchFadeDistance.x = largeBand0FadeDistance;
                    rendering.patchFadeDistance.y = ripplesFadeDistance;
                    rendering.patchFadeValue.x = largeBand0FadeToggle ? 0.0f : 1.0f;
                    rendering.patchFadeValue.y = ripplesFadeToggle ? 0.0f : 1.0f;
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
                    rendering.patchFadeStart.x = ripplesFadeStart;
                    rendering.patchFadeDistance.x = ripplesFadeDistance;
                    rendering.patchFadeValue.x = ripplesFadeToggle ? 0.0f : 1.0f;
                }
                break;
            }

            // Make sure the matrices are evaluated
            EvaluateWaterSurfaceMatrices(IsInstancedQuads(), transform.position, transform.rotation, ref rendering.waterToWorldMatrix, ref rendering.worldToWaterMatrix);

            return rendering;
        }
    }
}
