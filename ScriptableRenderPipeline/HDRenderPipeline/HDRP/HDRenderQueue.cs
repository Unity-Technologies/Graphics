using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // In HD we don't expose HDRenderQueue instead we create as much value as needed in the enum for our different pass
    // and use inspector to manipulate the value.
    // In the case of transparent we want to use RenderQueue to help with sorting. We define a neutral value for the RenderQueue and priority going from -X to +X
    // going from -X to +X instead of 0 to +X as builtin Unity is better for artists as they can decide late to sort behind or in front of the scene.

    public enum HDRenderQueuePriority
    {
        TransparentPriorityQueueRange = 100
    }

    public enum HDRenderQueue
    {
        Background = UnityEngine.Rendering.RenderQueue.Background,
        Geometry = UnityEngine.Rendering.RenderQueue.Geometry,
        AlphaTest = UnityEngine.Rendering.RenderQueue.AlphaTest,
        GeometryLast = UnityEngine.Rendering.RenderQueue.GeometryLast,
        // For transparent pass we define a range of 200 value to define the priority
        // Warning: Be sure no range are overlapping
        PreRefractionMin = 2750 - HDRenderQueuePriority.TransparentPriorityQueueRange,
        PreRefraction = 2750,
        PreRefractionMax = 2750 + HDRenderQueuePriority.TransparentPriorityQueueRange,
        TransparentMin = UnityEngine.Rendering.RenderQueue.Transparent - HDRenderQueuePriority.TransparentPriorityQueueRange,
        Transparent = UnityEngine.Rendering.RenderQueue.Transparent,
        TransparentMax = UnityEngine.Rendering.RenderQueue.Transparent + HDRenderQueuePriority.TransparentPriorityQueueRange,
        Overlay = UnityEngine.Rendering.RenderQueue.Overlay
    }
}
