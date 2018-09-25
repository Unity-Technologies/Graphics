namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public enum ShaderPathID
    {
        PhysicallyBased,
        SimpleLit,
        Unlit,
        TerrainPhysicallyBased,
        ParticlesPhysicallyBased,
        ParticlesSimpleLit,
        ParticlesUnlit,
        Count
    }

    public static class ShaderUtils
    {
        static readonly string[] s_ShaderPaths  =
        {
            "Lightweight Render Pipeline/Lit",
            "Lightweight Render Pipeline/Simple Lit",
            "Lightweight Render Pipeline/Unlit",
            "Lightweight Render Pipeline/Terrain/Lit",
            "Lightweight Render Pipeline/Particles/Lit",
            "Lightweight Render Pipeline/Particles/Simple Lit",
            "Lightweight Render Pipeline/Particles/Unlit",
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
