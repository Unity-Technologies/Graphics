using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedShadowInitParameters
    {
        public SerializedProperty root;

        public SerializedProperty shadowAtlasWidth;
        public SerializedProperty shadowAtlasHeight;
        public SerializedProperty shadowMap16Bit;

        public SerializedProperty maxPointLightShadows;
        public SerializedProperty maxSpotLightShadows;
        public SerializedProperty maxDirectionalLightShadows;

        public SerializedShadowInitParameters(SerializedProperty root)
        {
            this.root = root;

            shadowAtlasWidth = root.Find((ShadowInitParameters s) => s.shadowAtlasWidth);
            shadowAtlasHeight = root.Find((ShadowInitParameters s) => s.shadowAtlasHeight);
            shadowMap16Bit = root.Find((ShadowInitParameters s) => s.shadowMap16Bit);
            maxPointLightShadows = root.Find((ShadowInitParameters s) => s.maxPointLightShadows);
            maxSpotLightShadows = root.Find((ShadowInitParameters s) => s.maxSpotLightShadows);
            maxDirectionalLightShadows = root.Find((ShadowInitParameters s) => s.maxDirectionalLightShadows);
        }
    }
}
