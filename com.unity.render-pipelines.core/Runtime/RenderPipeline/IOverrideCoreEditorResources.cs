namespace UnityEngine.Rendering
{
    /// <summary>
    /// Allows a render pipeline to override some of the default resources used in core. This interface needs to be implemented on the RenderPipelineAsset.
    /// </summary>
    public interface IOverrideCoreEditorResources
    {
#if UNITY_EDITOR
        /// <summary>
        /// Overrides the shader used to show the probes in the Probe Volume system. This is useful when your probe relies on render pipeline specific data to be used, like exposure for example.
        /// </summary>
        /// <returns>The shader used to render the probe debug gizmo.</returns>
        Shader GetProbeVolumeProbeShader();
#endif
    }
}
