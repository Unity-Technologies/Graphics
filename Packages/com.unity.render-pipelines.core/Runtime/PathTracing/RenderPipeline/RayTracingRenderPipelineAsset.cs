#if ENABLE_PATH_TRACING_SRP
using UnityEngine.PathTracing.Core;

#if ENABLE_PATH_TRACING_SRP
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
#endif

namespace UnityEngine.Rendering.LiveGI
{
#if ENABLE_PATH_TRACING_SRP
    // The CreateAssetMenu attribute lets you create instances of this class in the Unity Editor.
    [CreateAssetMenu(menuName = "Rendering/RayTracingRenderPipelineAsset")]
#endif
    internal class RayTracingRenderPipelineAsset : RenderPipelineAsset
    {
        public PathTracingSettings settings;

        // Unity calls this method before rendering the first frame.
        // If a setting on the Render Pipeline Asset changes, Unity destroys the current Render Pipeline Instance and calls this method again before rendering the next frame.
        protected override RenderPipeline CreatePipeline()
        {
            // Instantiate the Render Pipeline that this custom SRP uses for rendering.
            return new RayTracingRenderPipelineInstance(this);
        }
    }
}
#endif
