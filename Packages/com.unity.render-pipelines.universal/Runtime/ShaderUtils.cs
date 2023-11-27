using System;
using System.Linq;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Options to get a shader path to URP shaders when calling ShaderUtils.GetShaderGUID();
    /// <see cref="ShaderUtils"/>.
    /// </summary>
    public enum ShaderPathID
    {
        /// <summary>
        /// Use this for URP Lit shader.
        /// </summary>
        Lit,

        /// <summary>
        /// Use this for URP Simple Lit shader.
        /// </summary>
        SimpleLit,

        /// <summary>
        /// Use this for URP Unlit shader.
        /// </summary>
        Unlit,

        /// <summary>
        /// Use this for URP Terrain Lit shader.
        /// </summary>
        TerrainLit,

        /// <summary>
        /// Use this for URP Particles Lit shader.
        /// </summary>
        ParticlesLit,

        /// <summary>
        /// Use this for URP Particles Simple Lit shader.
        /// </summary>
        ParticlesSimpleLit,

        /// <summary>
        /// Use this for URP Particles Simple Unlit shader.
        /// </summary>
        ParticlesUnlit,

        /// <summary>
        /// Use this for URP Baked Lit shader.
        /// </summary>
        BakedLit,

        /// <summary>
        /// Use this for URP SpeedTree 7 shader.
        /// </summary>
        SpeedTree7,

        /// <summary>
        /// Use this for URP SpeedTree 7 Billboard shader.
        /// </summary>
        SpeedTree7Billboard,

        /// <summary>
        /// Use this for URP SpeedTree 8 shader.
        /// </summary>
        SpeedTree8,

        /// <summary>
        /// Use this for URP SpeedTree 9 shader.
        /// </summary>
        SpeedTree9,
        // If you add a value here, also add it to ShaderID in Editor/ShaderUtils.cs

        /// <summary>
        /// Use this for URP Complex Lit shader.
        /// </summary>
        ComplexLit,
    }

    /// <summary>
    /// Various utility functions for shaders in URP.
    /// </summary>
    public static class ShaderUtils
    {
        static readonly string[] s_ShaderPaths =
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
            "Universal Render Pipeline/Nature/SpeedTree8_PBRLit",
            "Universal Render Pipeline/Complex Lit",
        };

        /// <summary>
        /// Retrieves a shader path for the given URP Shader Path ID.
        /// </summary>
        /// <param name="id">The URP Shader Path ID.</param>
        /// <returns>The path to the URP shader.</returns>
        public static string GetShaderPath(ShaderPathID id)
        {
            int index = (int)id;
            int arrayLength = s_ShaderPaths.Length;
            if (arrayLength > 0 && index >= 0 && index < arrayLength)
                return s_ShaderPaths[index];

            Debug.LogError("Trying to access universal shader path out of bounds: (" + id + ": " + index + ")");
            return "";
        }

        /// <summary>
        /// Retrieves a URP Shader Path ID from a path given.
        /// </summary>
        /// <param name="path">The path to the shader.</param>
        /// <returns>The URP Shader Path ID.</returns>
        public static ShaderPathID GetEnumFromPath(string path)
        {
            var index = Array.FindIndex(s_ShaderPaths, m => m == path);
            return (ShaderPathID)index;
        }

        /// <summary>
        /// Checks if a given shader is a URP shader or not.
        /// </summary>
        /// <param name="shader">The shader.</param>
        /// <returns>True or false if it's a URP shader or not.</returns>
        public static bool IsLWShader(Shader shader)
        {
            return s_ShaderPaths.Contains(shader.name);
        }

#if UNITY_EDITOR
        private static float s_MostRecentValidDeltaTime = 0.0f;
#endif

        // A delta time that does not get reset to zero when stepping paused Play Mode or using the FrameDebugger
        // (unless Time.timeScale is zero)
        // * Can be zero on the first frame after domain reload (if Time.deltaTime is also zero)
        // * The value depends on when it was last called
        // * In in practice it should not get stale as it's called at least once during a URP frame
        // * Currently only used when calculating '_LastTimeParameters' for shader upload
        // * Please validate your use case if trying to reuse this somewhere else (as it might not transfer)
        internal static float PersistentDeltaTime
        {
            get
            {
#if UNITY_EDITOR
                float deltaTime = Time.deltaTime;
                // The only case I'm aware of when a deltaTime of 0 is valid is when Time.timeScale is 0
                if (deltaTime > 0.0f || Time.timeScale == 0.0f)
                    s_MostRecentValidDeltaTime = deltaTime;
                return s_MostRecentValidDeltaTime;
#else
                return Time.deltaTime;
#endif
            }
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
            "9920c1f1781549a46ba081a2a15a16ec",
            "ee7e4c9a5f6364b688a332c67fc32cca",
        };

        /// <summary>
        /// Returns a GUID for a URP shader from Shader Path ID.
        /// </summary>
        /// <param name="id">ID of shader path.</param>
        /// <returns>GUID for the shader.</returns>
        public static string GetShaderGUID(ShaderPathID id)
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
