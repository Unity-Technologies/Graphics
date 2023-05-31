using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGlobalDecalSettings
    {
        public SerializedProperty root;

        public SerializedProperty drawDistance;
        public SerializedProperty atlasWidth;
        public SerializedProperty atlasHeight;
        public SerializedProperty perChannelMask;

        public SerializedScalableSetting transparentTextureResolution;

        public SerializedGlobalDecalSettings(SerializedProperty root)
        {
            this.root = root;

            drawDistance = root.Find((GlobalDecalSettings s) => s.drawDistance);
            atlasWidth = root.Find((GlobalDecalSettings s) => s.atlasWidth);
            atlasHeight = root.Find((GlobalDecalSettings s) => s.atlasHeight);
            perChannelMask = root.Find((GlobalDecalSettings s) => s.perChannelMask);
            transparentTextureResolution = new SerializedScalableSetting(root.Find((GlobalDecalSettings s) => s.transparentTextureResolution));
        }
    }
}
