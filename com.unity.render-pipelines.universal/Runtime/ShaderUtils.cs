using System;
using System.Linq;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    [MovedFrom("UnityEngine.Rendering.LWRP")] public enum ShaderPathID
    {
        Lit,
        SimpleLit,
        Unlit,
        TerrainLit,
        ParticlesLit,
        ParticlesSimpleLit,
        ParticlesUnlit,
        BakedLit,
        Count
    }

    [MovedFrom("UnityEngine.Rendering.LWRP")] public static class ShaderUtils
    {
        static readonly string[] s_ShaderPaths  =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Terrain/Lit",
            "Universal Render Pipeline/Particles/Lit",
            "Universal Render Pipeline/Particles/Simple Lit",
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Baked Lit",
        };

        public static string GetShaderPath(ShaderPathID id)
        {
            int index = (int)id;
            if (index < 0 && index >= (int)ShaderPathID.Count)
            {
                Debug.LogError("Trying to access universal shader path out of bounds");
                return "";
            }

            return s_ShaderPaths[index];
        }

        public static ShaderPathID GetEnumFromPath(string path)
        {
            var index = Array.FindIndex(s_ShaderPaths, m => m == path);
            return (ShaderPathID)index;
        }

        public static bool IsLWShader(Shader shader)
        {
            return s_ShaderPaths.Contains(shader.name);
        }
    }
}
