namespace UnityEngine.Rendering.Universal
{
    public abstract class CustomPostProcessVolumeComponent : VolumeComponent, IPostProcessComponent
    {
        public abstract void Setup();

        public abstract bool IsActive();

        public abstract bool IsTileCompatible();

        public abstract void Render(Camera camera, CommandBuffer cmd, RTHandle source, RTHandle destination);
    }
}
