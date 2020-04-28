using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public struct SerializableVirtualTextureLayer
    {
        public string layerName;
        public SerializableTexture layerTexture;

        public SerializableVirtualTextureLayer(string name, SerializableTexture texture)
        {
            this.layerName = name;
            this.layerTexture = texture;
        }
    }

    [Serializable]
    public sealed class SerializableVirtualTexture : ISerializationCallbackReceiver
    {
        [SerializeField]
        public List<SerializableVirtualTextureLayer> entries = new List<SerializableVirtualTextureLayer>();

        [SerializeField]
        public bool procedural;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
        }
    }
}

