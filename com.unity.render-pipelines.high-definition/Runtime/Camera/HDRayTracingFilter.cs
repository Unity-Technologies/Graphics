using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class HDRayTracingFilter : MonoBehaviour
    {
        [HideInInspector]
        const int currentVersion = 1;

#if ENABLE_RAYTRACING
        // Culling mask that defines the layers that this acceleration structure should handle
        public LayerMask layermask = -1;

        // Flag that requests a rebuild of the matching acceleration structure
        private bool dirty = false;

        // This exposes a context menu  that allows us to manually rebuild the matching acceleration structure
        [ContextMenu("Rebuild Acceleration Structure")]
        public void SetDirty()
        {
            dirty = true;
        }

        // Access the dirty flag
        public bool IsDirty()
        {
            return dirty;
        }

        // Reset the dirty flag
        public void ResetDirty()
        {
            dirty = false;
        }

        private void Start()
        {
            // Grab the High Definition RP
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                hdPipeline.m_RayTracingManager.RegisterFilter(this);
            }
        }
        private void OnDestroy()
        {
            // Grab the High Definition RP
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                hdPipeline.m_RayTracingManager.UnregisterFilter(this);
            }
        }

#endif
    }
}
