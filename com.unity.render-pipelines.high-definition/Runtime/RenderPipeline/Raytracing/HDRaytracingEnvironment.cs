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
        // Recursive Rendering
        public LayerMask raytracedLayerMask = -1;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Area Light Shadows
        public LayerMask shadowLayerMask = -1;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Indirect diffuse
        public LayerMask indirectDiffuseLayerMask = -1;

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
