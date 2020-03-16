namespace UnityEngine.Rendering
{
    /*
        Render pipelines which are aware of virtual texturing and support it should implement this interface.
    */
    public interface IVirtualTexturingEnabledRenderPipeline
    {
        bool virtualTexturingEnabled { get; }
    }
}
