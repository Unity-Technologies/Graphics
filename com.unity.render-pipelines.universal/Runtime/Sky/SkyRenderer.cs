namespace UnityEngine.Rendering.Universal
{
    public abstract class SkyRenderer
    {
        public abstract void Build();
        public abstract void Cleanup();

        public virtual void PrerenderSky(ref CameraData cameraData, CommandBuffer cmd) { }

        public abstract void RenderSky(ref CameraData cameraData, CommandBuffer cmd);

        // TODO
    }
}
