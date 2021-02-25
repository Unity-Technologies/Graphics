using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using System.Linq;
using UnityEditorInternal;
#endif
namespace UnityEngine.Rendering
{
    // TODO: Should we call this just CoreAsset as it doesn't inherit from RenderPipelineAsset?

    /// NOTE: Core Pipeline asset should not initialize its own resources.
    /// <summary>
    /// Core Render Pipeline asset shared between pipelines.
    /// </summary>
    public class CoreRenderPipelineAsset : IDisposable
    {
        public void Dispose() => Dispose(true);
        void Dispose(bool disposing)
        {
            s_Blitter?.Dispose();
        }

        private static Blitter s_Blitter = null;
        public static Blitter blitter => s_Blitter;


        public static bool IsSupportedFeatureBlitter => s_Blitter != null;
        public void InitFeatureBlitter(Shader blitPS, Shader blitColorAndDepthPS)
        {
            s_Blitter = new Blitter(blitPS, blitColorAndDepthPS);
        }

    #if UNITY_EDITOR
        // TODO: ...
    #endif
    }
}
