using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.VFX
{
    [Serializable]
    class VFXAssetSerializedData
    {
        public List<VFXSystemSerializedData> systems;
    }

    // Data models

    [Serializable]
    class VFXDataNodeSerializedData
    {
        public bool Exposed;
        public Vector2 UIPosition;

        public List<VFXBlockSerializedData> blocks;
    }

    // Same as VFXBlockSerializeData
    /*[Serializable]
    class VFXDataBlockSerializedData
    {
        public string DescId;
        public bool UICollapsed;

        public List<VFXPropertySerializeData> properties;   
    }*/

    // System models

    [Serializable]
    class VFXSystemSerializedData
    {
        public uint MaxNb;
        public float SpawnRate;
        public BlendMode BlendingMode;
        public int OrderPriority;
        public int ID;

        public List<VFXContextSerializedData> contexts;
    }

    [Serializable]
    class VFXContextSerializedData
    {
        public string DescId;

        public Vector2 UIPosition;
        public bool UICollapsed;

        public List<VFXBlockSerializedData> blocks;
    }

    [Serializable]
    class VFXBlockSerializedData
    {
        public string DescId;
        public bool UICollapsed;

        List<VFXPropertySerializeData> properties;   
    }

    // Properties and links

    [Serializable]
    class VFXPropertySerializeData
    {

        List<VFXLinkSerializedData> links;
    }

    [Serializable]
    class VFXLinkSerializedData
    {
        uint Slot;
        uint Depth;
    }
}
