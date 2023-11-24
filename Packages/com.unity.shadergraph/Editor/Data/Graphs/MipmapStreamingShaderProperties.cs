using System;

namespace UnityEditor.ShaderGraph.Internal
{
    internal static class MipmapStreamingShaderProperties
    {
        public sealed class MipmapStreamingShaderProperty : Texture2DShaderProperty
        {
            internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
            {
                // No Texture2D declaration needed, already declared by internal files.
                // We do want to declare related mipmap streaming debugging properties, wrapped inside a macro
                action(new HLSLProperty(HLSLType._CUSTOM, "UNITY_TEXTURE_STREAMING_DEBUG_VARS", HLSLDeclaration.UnityPerMaterial)
                    {
                        customDeclaration = (ssb) =>
                        {
                            ssb.TryAppendIndentation();
                            ssb.Append("UNITY_TEXTURE_STREAMING_DEBUG_VARS;");
                        }
                    }
                );
            }
        }

        public static readonly MipmapStreamingShaderProperty kDebugTex = new MipmapStreamingShaderProperty()
        {
            overrideReferenceName = "unity_MipmapStreaming_DebugTex",
            generatePropertyBlock = false,
            value = new SerializableTexture(),
        };
    }
}
