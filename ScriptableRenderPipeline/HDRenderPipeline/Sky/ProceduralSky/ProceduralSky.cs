namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent]
    public class ProceduralSky : SkySettings
    {
        public ClampedFloatParameter sunSize = new ClampedFloatParameter { value = 0.04f, min = 0.0f, max = 1.0f };
        public ClampedFloatParameter sunSizeConvergence = new ClampedFloatParameter { value = 5.0f, min = 1.0f, max = 10.0f };
        public ClampedFloatParameter atmosphereThickness = new ClampedFloatParameter { value = 1.0f, min = 0.0f, max = 5.0f };
        public ColorParameter skyTint = new ColorParameter { value = new Color(0.5f, 0.5f, 0.5f, 1.0f) };
        public ColorParameter groundColor = new ColorParameter { value = new Color(0.369f, 0.349f, 0.341f, 1.0f) };
        public BoolParameter enableSunDisk = new BoolParameter { value = true };

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
