using UnityEngine;

public class LightweightPipelineResources : ScriptableObject
{
    public Shader BlitShader;
    public Shader CopyDepthShader;
    public Shader ScreenSpaceShadowShader;
    public Shader SamplingShader;
    public Shader BlitTransientShader;
}
