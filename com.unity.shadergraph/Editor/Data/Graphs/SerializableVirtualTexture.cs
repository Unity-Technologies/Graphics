using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public struct SerializableVirtualTextureLayer
    {
        public string layerName;
        public string layerRefName;
        public SerializableTexture layerTexture;

        public SerializableVirtualTextureLayer(string name, string refName, SerializableTexture texture)
        {
            this.layerName = name;
            this.layerRefName = refName;
            this.layerTexture = texture;
        }
    }

    [Serializable]
    public sealed class SerializableVirtualTexture : ISerializationCallbackReceiver
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

