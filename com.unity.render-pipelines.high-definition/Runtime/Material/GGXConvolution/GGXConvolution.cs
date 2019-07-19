namespace UnityEngine.Rendering.HighDefinition
{
    // For multiple importance sampling
    // TODO: not working currently, will be updated later
    [GenerateHLSL(PackingRules.Exact)]
    enum LightSamplingParameters
    {
        TextureHeight = 256,
        TextureWidth = 512
    }
}
