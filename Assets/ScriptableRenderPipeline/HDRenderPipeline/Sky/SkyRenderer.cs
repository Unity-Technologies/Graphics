using System;


namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    abstract public class SkyRenderer
    {
        public abstract void Build();
        public abstract void Cleanup();
        public abstract void SetRenderTargets(BuiltinSkyParameters builtinParams);
        // renderForCubemap: When rendering into a cube map, no depth buffer is available so user has to make sure not to use depth testing or the depth texture.
        public abstract void RenderSky(BuiltinSkyParameters builtinParams, SkySettings skyParameters, bool renderForCubemap);
        public abstract bool IsSkyValid();
    }
}
