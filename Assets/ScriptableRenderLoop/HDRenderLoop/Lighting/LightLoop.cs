using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    public class BaseLightLoop
    {
        // TODO: We should rather put the texture settings in LightLoop, but how do we serialize it ?
        public virtual void Build(TextureSettings textureSettings) {}

        public virtual void Cleanup() {}

        public virtual bool NeedResize() { return false;  }

        public virtual void AllocResolutionDependentBuffers(int width, int height) { }

        public virtual void ReleaseResolutionDependentBuffers() {}

        public virtual void NewFrame() {}

        public virtual void PrepareLightsForGPU(CullResults cullResults, Camera camera, ref ShadowOutput shadowOutput) { }
        
        // TODO: this should not be part of the interface but for now make something working
        public virtual void BuildGPULightLists(Camera camera, RenderLoop loop, RenderTargetIdentifier cameraDepthBufferRT) { }

        public virtual void PushGlobalParams(Camera camera, RenderLoop loop) {}

        public virtual void RenderDeferredLighting(HDRenderLoop.HDCamera hdCamera, RenderLoop renderLoop, RenderTargetIdentifier cameraColorBufferRT) {}

        public virtual void RenderForward(Camera camera, RenderLoop renderLoop, bool renderOpaque) {}
    }
}
