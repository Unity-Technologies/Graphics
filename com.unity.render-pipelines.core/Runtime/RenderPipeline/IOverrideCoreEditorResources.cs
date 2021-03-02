namespace UnityEngine.Rendering
{
    public interface IOverrideCoreEditorResources
    {
#if UNITY_EDITOR
        Shader GetProbeVolumeProbeShader();
#endif
    }
}
