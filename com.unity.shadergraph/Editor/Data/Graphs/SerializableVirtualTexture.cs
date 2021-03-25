using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    enum LayerTextureType
    {
        Default,
        NormalTangentSpace,
        NormalObjectSpace
    };

    [Serializable]
    internal class SerializableVirtualTextureLayer
    {
        public string layerName;
        public string layerRefName;
        public SerializableTexture layerTexture;
        public LayerTextureType layerTextureType;
        [SerializeField]
        private Guid guid;

        public SerializableVirtualTextureLayer(string name, string refName, SerializableTexture texture)
        {
            this.layerName = name; this.layerName = name;
            this.guid = Guid.NewGuid();
            this.layerRefName = refName; this.layerRefName = refName;
            this.layerTexture = texture; this.layerTexture = texture;
            this.layerTextureType = LayerTextureType.Default; this.layerTextureType = LayerTextureType.Default;
        }

        public SerializableVirtualTextureLayer(string name, SerializableTexture texture)
        {
            this.layerName = name;
            this.guid = Guid.NewGuid();
            this.layerRefName = $"Layer_{GuidEncoder.Encode(this.guid)}";
            this.layerTexture = texture;
            this.layerTextureType = LayerTextureType.Default;
        }

        public SerializableVirtualTextureLayer(SerializableVirtualTextureLayer other)
        {
            this.layerName = other.layerName;
            this.guid = Guid.NewGuid();
            this.layerRefName = $"Layer_{GuidEncoder.Encode(this.guid)}";
            this.layerTexture = other.layerTexture;
            this.layerTextureType = LayerTextureType.Default;
        }
    }

    [Serializable]
    internal sealed class SerializableVirtualTexture
    {
        [SerializeField]
        public List<SerializableVirtualTextureLayer> layers = new List<SerializableVirtualTextureLayer>();

        [SerializeField]
        public bool procedural;
    }
}
