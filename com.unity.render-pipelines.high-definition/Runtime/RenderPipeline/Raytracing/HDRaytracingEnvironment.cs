using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    public class HDRaytracingEnvironment : MonoBehaviour
    {
#if ENABLE_RAYTRACING

        // The set of raytracing passes that we support
        public enum RaytracingPass
        {
            AmbientOcclusion = 0,
            Reflection = (1<<0),
            AreaShadow = (1<<1) ,
            PrimaryVisibility = (1<<2),
        }
        public readonly static int numRaytracingPasses = 4;

        // Generic Ray Data
        [Range(0.0f, 0.1f)]
        public float rayBias = 0.001f;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Ambient Occlusion Data
        // Flag that defines if the Ambient Occlusion should be Ray-traced
        public bool raytracedAO = false;

        // Culling mask that defines the layers that the subscene used for this effect should use
        public LayerMask aoLayerMask = -1;

        // Filter Type for the ambient occlusion
        public enum AOFilterMode
        {
            None,
            Bilateral,
            Nvidia
        };
        public AOFilterMode aoFilterMode = AOFilterMode.None;

        // Max Ray Length for the AO
        [Range(0.001f, 20.0f)]
        public float aoRayLength = 5.0f;

        // Number of Samples for Ambient Occlusion
        [Range(1, 64)]
        public int aoNumSamples = 4;

        // AO Bilateral Filter Data
        [Range(1, 27)]
        public int aoBilateralRadius = 10;
        [Range(0.001f, 9.0f)]
        public float aoBilateralSigma = 5.0f;

        // Nvidia AO Filter Data
        [Range(1, 27)]
        public int maxFilterWidthInPixels = 25;
        [Range(0.0f, 10.0f)]
        public float filterRadiusInMeters = 1.0f;
        [Range(1.0f, 50.0f)]
        public float normalSharpness = 30.0f;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Reflection Data
        // Flag that defines if the Reflections should be Ray-traced
        public bool raytracedReflections = false;

        // Culling mask that defines the layers that the subscene used for this effect should use
        public LayerMask reflLayerMask = -1;

        // Generic reflection Data
        // Max Ray Length for the Reflections
        [Range(0.001f, 50.0f)]
        public float reflRayLength = 5.0f;
        // The distance at which the blend between the different strategies starts
        [Range(0.001f, 50.0f)]
        public float reflBlendDistance = 5.0f;
        // The smoothness at which raytraced reflections are not used anymore
        [Range(0.0f, 1.0f)]
        public float reflMinSmoothness = 0.5f;
        // Value that is used to clamp the intensity to avoid fireflies
        [Range(0.01f, 10.0f)]
        public float reflClampValue = 5.0f;

        // The different reflection qualities that we implement
        public enum ReflectionsQuality
        {
            // 1 ray for every 4 pixels
            QuarterRes,
            // Full integration
            Integration
        };
        public ReflectionsQuality reflQualityMode = ReflectionsQuality.QuarterRes;

        // Reflection Quarter Res Data
        [Range(0.01f, 1.0f)]
        public float reflTemporalAccumulationWeight = 0.1f;
        [Range(1, 5)]
        public int reflSpatialFilterRadius = 3;

        // Data for the integration modeJe su
        // Number of Samples for the integration
        [Range(1, 64)]
        public int reflNumMaxSamples = 8;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Light Cluster
        [Range(0, 24)]
        public int maxNumLightsPercell = 10;
        [Range(0.001f, 50.0f)]
        public float cameraClusterRange = 10;


        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Primary Visibility
        // Flag that defines if raytraced objects should be rendered
        public bool raytracedObjects = false;

        // Culling mask that defines the layers that the subscene used for this effect should use
        public LayerMask raytracedLayerMask = -1;

        // This is the maximal depth that a ray can have for the primary visibility pass
        const int maxRayDepth = 10;
        [Range(1, maxRayDepth)]
        public int rayMaxDepth = 3;

        // Max Ray Length for the primary visibility
        [Range(0.001f, 50.0f)]
        public float raytracingRayLength = 20.0f;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Area Light Shadows
        public bool raytracedShadows = false;

        // Culling mask that defines the layers that the subscene used for this effect should use
        public LayerMask shadowLayerMask = -1;

        [Range(1, 32)]
        public int shadowNumSamples = 4;
        [Range(0, 4)]
        public int numAreaLightShadows = 1;
        [Range(1, 27)]
        public int shadowFilterRadius = 1;
        [Range(0.001f, 9.0f)]
        public float shadowFilterSigma = 0.001f;

        void Start()
        {
            // Grab the High Definition RP
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                hdPipeline.m_RayTracingManager.RegisterEnvironment(this);
            }
        }
        void OnDestroy()
        {
            // Grab the High Definition RP
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                hdPipeline.m_RayTracingManager.UnregisterEnvironment(this);
            }
        }
#endif
    }
}
