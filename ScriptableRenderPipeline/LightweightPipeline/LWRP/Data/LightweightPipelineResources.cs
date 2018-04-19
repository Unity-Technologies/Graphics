using UnityEngine;

public class LightweightPipelineResources : ScriptableObject
{
    public Shader BlitShader;
    public Shader CopyDepthShader;
    public Shader CopyDepthMSAAShader;
    public Shader ScreenSpaceShadowShader;
    public Shader SamplingShader;
}
