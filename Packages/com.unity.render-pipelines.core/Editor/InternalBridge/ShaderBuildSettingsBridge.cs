using UnityEditor.Shaders;

namespace UnityEditor.Rendering
{
    // Forwards the internal define API of ShaderBuildSettings, so a render pipeline package can drive a shader
    // define from a setting without the editor assembly's internals being visible to the whole package.
    static class ShaderBuildSettingsBridge
    {
        public static bool HasInternalDefine(ref ShaderBuildSettings settings, string identifier)
            => settings.HasInternalDefine(identifier);

        public static void AddInternalDefine(ref ShaderBuildSettings settings, string define)
            => settings.AddInternalDefine(define);

        // Returns true when a define was removed.
        public static bool RemoveInternalDefine(ref ShaderBuildSettings settings, string identifier)
            => settings.RemoveInternalDefine(identifier);
    }
}
