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
        // Generic Ray Data
        [Range(0.0f, 0.1f)]
        public float rayBias = 0.001f;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Ambient Occlusion Data
        // Flag that defines if the Ambient Occlusion should be Ray-traced
        public bool raytracedAO = false;

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

        // Max Ray Length for the Reflections
        [Range(0.001f, 50.0f)]
        public float reflRayLength = 5.0f;

        // Number of Samples for the Reflections
        [Range(1, 64)]
        public int reflNumMaxSamples = 8;

        public enum ReflectionsFilterMode
        {
            None,
            Bilateral
        };
        public ReflectionsFilterMode reflFilterMode = ReflectionsFilterMode.None;

        // Reflection Bilateral Filter Data
        [Range(1, 27)]
        public int reflBilateralRadius = 10;
        [Range(0.001f, 9.0f)]
        public float reflBilateralSigma = 5.0f;

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
        [Range(2, 32)]
        public int shadowNumSamples = 4;
        [Range(0, 4)]
        public int numAreaLightShadows = 1;
        [Range(1, 27)]
        public int shadowFilterRadius = 10;
        [Range(0.001f, 9.0f)]
        public float shadowFilterSigma = 5.0f;

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
