using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class LightweightKeywords : MonoBehaviour
    {
        // Pipeline specific keywords
        public static readonly ShaderKeyword AdditionalLights = new ShaderKeyword("_ADDITIONAL_LIGHTS");
        public static readonly ShaderKeyword VertexLights = new ShaderKeyword("_VERTEX_LIGHTS");
        public static readonly ShaderKeyword SubtractiveLight = new ShaderKeyword("_MIXED_LIGHTING_SUBTRACTIVE");
        public static readonly ShaderKeyword DirectionalShadows = new ShaderKeyword("_SHADOWS_ENABLED");
        public static readonly ShaderKeyword LocalShadows = new ShaderKeyword("_LOCAL_SHADOWS_ENABLED");

        public static readonly ShaderKeyword Lightmap = new ShaderKeyword("DIRLIGHTMAP_COMBINED");
        public static readonly ShaderKeyword DirectionalLightmap = new ShaderKeyword("LIGHTMAP_ON");
    }
}

