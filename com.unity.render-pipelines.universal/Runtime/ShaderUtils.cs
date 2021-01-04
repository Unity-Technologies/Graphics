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
        SpeedTree7,
        SpeedTree7Billboard,
        SpeedTree8,
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
            "Universal Render Pipeline/Nature/SpeedTree7",
            "Universal Render Pipeline/Nature/SpeedTree7 Billboard",
            "Universal Render Pipeline/Nature/SpeedTree8",
        };

        public static string GetShaderPath(ShaderPathID id)
        {
            int index = (int)id;
            int arrayLength = s_ShaderPaths.Length;
            if (arrayLength > 0 && index >= 0 && index < arrayLength)
                return s_ShaderPaths[index];

            Debug.LogError("Trying to access universal shader path out of bounds: (" + id + ": " + index + ")");
            return "";
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
            "0406db5a14f94604a8c57ccfbc9f3b46",
            "0ca6dca7396eb48e5849247ffd444914",
            "0f4122b9a743b744abe2fb6a0a88868b",
            "5ec81c81908db34429b4f6ddecadd3bd",
            "99134b1f0c27d54469a840832a28fadf",
        };

        internal static string GetShaderGUID(ShaderPathID id)
        {
            int index = (int)id;
            int arrayLength = s_ShaderGUIDs.Length;
            if (arrayLength > 0 && index >= 0 && index < arrayLength)
                return s_ShaderGUIDs[index];

            Debug.LogError("Trying to access universal shader GUID out of bounds: (" + id + ": " + index + ")");
            return "";
        }

#endif
    }
}
