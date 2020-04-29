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

#if UNITY_EDITOR
        static readonly string[] s_ShaderGUIDs =
        {
            "933532a4fcc9baf4fa0491de14d08ed7",
            "8d2bb70cbf9db8d4da26e15b26e74248",
            "650dd9526735d5b46b79224bc6e94025",
            "69c1f799e772cb6438f56c23efccb782",
            "b7839dad95683814aa64166edc107ae2",
            "8516d7a69675844a7a0b7095af7c46af",
            "0406db5a14f94604a8c57ccfbc9f3b46t",
            "0ca6dca7396eb48e5849247ffd444914",
        };

        public static string GetShaderGUID(ShaderPathID id)
        {
            int index = (int)id;
            if (index < 0 && index >= (int)ShaderPathID.Count)
            {
                Debug.LogError("Trying to access universal shader path out of bounds");
                return "";
            }

            return s_ShaderGUIDs[index];
        }
#endif
    }
}
