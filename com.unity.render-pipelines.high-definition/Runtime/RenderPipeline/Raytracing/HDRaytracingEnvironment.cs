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
            IndirectDiffuse = (1<<3),
        }
        public readonly static int numRaytracingPasses = 5;

        // Generic Ray Data
        [Range(0.0f, 0.1f)]
        public float rayBias = 0.001f;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Ambient Occlusion Data
        public LayerMask aoLayerMask = -1;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Reflection Data
        public LayerMask reflLayerMask = -1;

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
        public LayerMask shadowLayerMask = -1;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Indirect diffuse
        public bool raytracedIndirectDiffuse = false;

        // Culling mask that defines the layers that the subscene used for this effect should use
        public LayerMask indirectDiffuseLayerMask = -1;

        [Range(1, 32)]
        public int indirectDiffuseNumSamples = 4;
        // Max Ray Length for the indirect diffuse
        [Range(0.001f, 50.0f)]
        public float indirectDiffuseRayLength = 20.0f;
        // Value that is used to clamp the intensity to avoid fireflies
        [Range(0.01f, 10.0f)]
        public float indirectDiffuseClampValue = 1.0f;

        // The different reflection filtering modes
        public enum IndirectDiffuseFilterMode
        {
            SpatioTemporal,
            None
        };
        public IndirectDiffuseFilterMode indirectDiffuseFilterMode = IndirectDiffuseFilterMode.None;

        // The radius for the spatio temporal filter
        [Range(1, 27)]
        public int indirectDiffuseFilterRadius = 16;

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
