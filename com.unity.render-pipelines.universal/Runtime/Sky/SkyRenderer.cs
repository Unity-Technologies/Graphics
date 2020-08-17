namespace UnityEngine.Rendering.Universal
{
    public abstract class SkyRenderer
    {
        public abstract void Build();
        public abstract void Cleanup();

        public virtual void PrerenderSky(ref CameraData cameraData, CommandBuffer cmd) { }

        public abstract void RenderSky(ref CameraData cameraData, CommandBuffer cmd);

        protected static float GetSkyIntensity(SkySettings skySettings)
        {
            float skyIntensity = 1.0f;

            // TODO Debug display settings

            switch (skySettings.skyIntensityMode.value)
            {
                case SkyIntensityMode.Exposure:
                    skyIntensity *= ColorUtils.ConvertEV100ToExposure(-skySettings.exposure.value);
                    break;
                case SkyIntensityMode.Multiplier:
                    skyIntensity *= skySettings.multiplier.value;
                    break;
                case SkyIntensityMode.Lux:
                    skyIntensity *= skySettings.desiredLuxValue.value / skySettings.upperHemisphereLuxValue.value;
                    break;
            }

            return skyIntensity;
        }

        // TODO
    }
}
