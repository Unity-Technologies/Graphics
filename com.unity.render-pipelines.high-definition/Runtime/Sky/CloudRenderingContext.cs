using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class CloudRenderingContext
    {
        public RTHandle cloudTextureRT { get; private set; }
        public RTHandle cloudShadowsRT { get; private set; }
        public int numLayers { get; private set; }
        public bool supportShadows { get; private set; }

        public CloudRenderingContext(int resolution, int numLayers, bool supportShadows, int shadowResolution)
        {
            this.numLayers = numLayers;
            this.supportShadows = supportShadows;

            cloudTextureRT = RTHandles.Alloc(resolution, resolution / 2, numLayers, colorFormat: GraphicsFormat.R16G16_SFloat,
                dimension: TextureDimension.Tex2DArray, enableRandomWrite: true, useMipMap: false,
                filterMode: FilterMode.Bilinear, name: "Cloud Texture");

            if (supportShadows)
                cloudShadowsRT = RTHandles.Alloc(shadowResolution, shadowResolution, colorFormat: GraphicsFormat.R8_SNorm,
                    dimension: TextureDimension.Tex2D, enableRandomWrite: true, useMipMap: false,
                    filterMode: FilterMode.Bilinear, name: "Cloud Shadows");
        }

        public void Cleanup()
        {
            RTHandles.Release(cloudTextureRT);
            if (supportShadows)
                CoreUtils.Destroy(cloudShadowsRT);
        }
    }
}
