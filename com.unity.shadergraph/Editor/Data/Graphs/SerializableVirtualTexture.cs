using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public sealed class SerializableVirtualTexture : ISerializationCallbackReceiver
    {
        [SerializeField]
        public List<string> layerNames = new List<string>();

        [SerializeField]
        public List<SerializableTexture> layerTextures = new List<SerializableTexture>();

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

