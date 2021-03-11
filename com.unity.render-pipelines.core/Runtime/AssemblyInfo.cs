using System.Runtime.CompilerServices;

#if ENABLE_HYBRID_RENDERER_V2 && !HYBRID_0_6_0_OR_NEWER
#error Core SRP 10.0.0 or newer with Hybrid Renderer V2 requires at least version 0.6.0 of com.unity.rendering.hybrid
#endif

[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Editor")]
