namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public enum ShaderPathID
    {
        PhysicallyBased,
        SimpleLit,
        Unlit,
        TerrainPhysicallyBased,
        ParticlesPhysicallyBased,
        ParticlesUnlit,

        Count
    }

    public static class LightweightShaderUtils
    {
        static readonly string[] s_ShaderPaths  =
        {
            "LightweightPipeline/Standard (Physically Based)",
            "LightweightPipeline/Standard (Simple Lighting)",
            "LightweightPipeline/Standard Unlit",
            "LightweightPipeline/Terrain/Standard Terrain",
            "LightweightPipeline/Particles/Standard",
            "LightweightPipeline/Particles/Standard Unlit",
            "Hidden/LightweightPipeline/Blit",
            "Hidden/LightweightPipeline/CopyDepth"
        };

        public static string GetShaderPath(ShaderPathID id)
        {
            int index = (int)id;
            if (index < 0 && index >= (int)ShaderPathID.Count)
            {
                Debug.LogError("Trying to access lightweight shader path out of bounds");
                return "";
            }

            return s_ShaderPaths[index];
        }
    }
}
