using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    // TODO: make TextureType work like this, combine the two enums (needs versioning for old data)
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

        public SerializableVirtualTextureLayer(string name, string refName, SerializableTexture texture)
        {
            this.layerName = name;
            this.layerRefName = refName;
            this.layerTexture = texture;
            this.layerTextureType = LayerTextureType.Default;
        }
    }

    [Serializable]
    internal sealed class SerializableVirtualTexture : ISerializationCallbackReceiver
    {
        [SerializeField]
        public List<SerializableVirtualTextureLayer> layers = new List<SerializableVirtualTextureLayer>();

//         [SerializeField]
//         public bool procedural;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
        }
    }
}

