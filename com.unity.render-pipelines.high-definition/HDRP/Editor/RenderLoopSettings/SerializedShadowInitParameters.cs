using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    class SerializedShadowInitParameters
    {
        public SerializedProperty root;

        public SerializedProperty shadowAtlasWidth;
        public SerializedProperty shadowAtlasHeight;

        public SerializedShadowInitParameters(SerializedProperty root)
        {
            this.root = root;

            shadowAtlasWidth = root.Find((ShadowInitParameters s) => s.shadowAtlasWidth);
            shadowAtlasHeight = root.Find((ShadowInitParameters s) => s.shadowAtlasHeight);
        }
    }
}
