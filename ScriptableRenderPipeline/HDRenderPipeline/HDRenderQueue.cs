using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum HDRenderQueue
    {
        Background = UnityEngine.Rendering.RenderQueue.Background,
        Geometry = UnityEngine.Rendering.RenderQueue.Geometry,
        AlphaTest = UnityEngine.Rendering.RenderQueue.AlphaTest,
        GeometryLast = UnityEngine.Rendering.RenderQueue.GeometryLast,
        PreTransparent = 2750,
        Transparent = UnityEngine.Rendering.RenderQueue.Transparent,
        Overlay = UnityEngine.Rendering.RenderQueue.Overlay
    }
}
