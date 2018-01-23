using UnityEngine;

public class LightweightPipelineResource : ScriptableObject
{
    public Shader BlitShader;
    public Shader CopyDepthShader;
#if UNITY_EDITOR
    public Material DefaultMaterial;
    public Material DefaultParticleMaterial;
    public Material DefaultTerrainMaterial;
#endif
}
