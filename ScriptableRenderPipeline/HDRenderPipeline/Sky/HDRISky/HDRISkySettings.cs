namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent]
    public class HDRISkySettings : SkySettings
    {
        public Cubemap skyHDRI;

        public override SkyRenderer GetRenderer()
        {
            return new HDRISkyRenderer(this);
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                hash = skyHDRI != null ? hash * 23 + skyHDRI.GetHashCode() : hash;
            }

            return hash;
        }
    }
}
