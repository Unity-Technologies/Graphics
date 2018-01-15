namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [SkyUniqueID((int)SkyType.HDRISky)]
    public class HDRISky : SkySettings
    {
        [Tooltip("Cubemap used to render the sky.")]
        public CubemapParameter skyHDRI = new CubemapParameter(null);

        public override SkyRenderer CreateRenderer()
        {
            return new HDRISkyRenderer(this);
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                hash = skyHDRI.value != null ? hash * 23 + skyHDRI.GetHashCode() : hash;
            }

            return hash;
        }
    }
}
