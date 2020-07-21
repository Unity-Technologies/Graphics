using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class CloudRenderingContext
    {
        public RTHandle cloudTextureRT { get; private set; }
        public RTHandle cloudShadowsRT { get; private set; } = null;
        public bool supportShadows { get; private set; }

        internal bool ambientProbeIsReady = false;

        public CloudRenderingContext(int resolution, bool supportShadows, int shadowResolution)
        {
            this.supportShadows = supportShadows;

            cloudTextureRT = RTHandles.Alloc(resolution, resolution / 2, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                dimension: TextureDimension.Tex2D, enableRandomWrite: true, useMipMap: false,
                filterMode: FilterMode.Bilinear, name: "Cloud Texture");

            if (supportShadows)
                cloudShadowsRT = RTHandles.Alloc(shadowResolution, shadowResolution, colorFormat: GraphicsFormat.R8_SNorm,
                    dimension: TextureDimension.Tex2D, enableRandomWrite: true, useMipMap: false,
                    filterMode: FilterMode.Bilinear, name: "Cloud Shadows");
        }

        public void Cleanup()
        {
            RTHandles.Release(cloudTextureRT);
            if (cloudShadowsRT != null)
                CoreUtils.Destroy(cloudShadowsRT);
        }
    }
}
