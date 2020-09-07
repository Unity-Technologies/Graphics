using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    internal class SkyRenderingContext
    {
        public SphericalHarmonicsL2 ambientProbe { get; private set; }
        public RTHandle skyboxCubemapRT { get; }
        public Cubemap skyboxCubemap { get; }

        public SkyRenderingContext(int resolution, SphericalHarmonicsL2 ambientProbe, string name)
        {
            this.ambientProbe = ambientProbe;
            skyboxCubemapRT = RTHandles.Alloc(resolution, resolution, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Cube, useMipMap: true, autoGenerateMips: false, filterMode: FilterMode.Trilinear, name: name);
            skyboxCubemap = new Cubemap(resolution, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.MipChain);
        }

        public void Cleanup()
        {
            RTHandles.Release(skyboxCubemapRT);
            CoreUtils.Destroy(skyboxCubemap);
        }

        public void ClearAmbientProbe()
        {
            ambientProbe = new SphericalHarmonicsL2();
        }

        public void UpdateAmbientProbe(in SphericalHarmonicsL2 probe)
        {
            ambientProbe = probe;
        }
    }
}
