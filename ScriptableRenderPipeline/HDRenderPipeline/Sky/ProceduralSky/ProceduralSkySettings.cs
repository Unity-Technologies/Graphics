namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent]
    public class ProceduralSkySettings : SkySettings
    {
        [Range(0.0f,1.0f)]
        public float sunSize = 0.04f;
        [Range(1.0f,10.0f)]
        public float sunSizeConvergence = 5.0f;
        [Range(0.0f,5.0f)]
        public float atmosphereThickness = 1.0f;
        public Color skyTint = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        public Color groundColor = new Color(0.369f, 0.349f, 0.341f, 1.0f);
        public bool enableSunDisk = true;

        public override SkyRenderer GetRenderer()
        {
            return new ProceduralSkyRenderer(this);
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                hash = hash * 23 + sunSize.GetHashCode();
                hash = hash * 23 + sunSizeConvergence.GetHashCode();
                hash = hash * 23 + atmosphereThickness.GetHashCode();
                hash = hash * 23 + skyTint.GetHashCode();
                hash = hash * 23 + groundColor.GetHashCode();
                hash = hash * 23 + multiplier.GetHashCode();
                hash = hash * 23 + enableSunDisk.GetHashCode();
            }

            return hash;
        }
    }
}
