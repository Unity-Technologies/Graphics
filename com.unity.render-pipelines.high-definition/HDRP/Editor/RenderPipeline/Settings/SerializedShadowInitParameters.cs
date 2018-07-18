using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    class SerializedShadowInitParameters
    {
        public SerializedProperty root;

        public SerializedProperty shadowAtlasWidth;
        public SerializedProperty shadowAtlasHeight;

        public SerializedProperty maxPointLightShadows;
        public SerializedProperty maxSpotLightShadows;
        public SerializedProperty maxDirectionalLightShadows;

        public SerializedShadowInitParameters(SerializedProperty root)
        {
            this.root = root;

            shadowAtlasWidth = root.Find((ShadowInitParameters s) => s.shadowAtlasWidth);
            shadowAtlasHeight = root.Find((ShadowInitParameters s) => s.shadowAtlasHeight);
            maxPointLightShadows = root.Find((ShadowInitParameters s) => s.maxPointLightShadows);
            maxSpotLightShadows = root.Find((ShadowInitParameters s) => s.maxSpotLightShadows);
            maxDirectionalLightShadows = root.Find((ShadowInitParameters s) => s.maxDirectionalLightShadows);
        }
    }
}
