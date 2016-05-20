using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.VFX
{
    [Serializable]
    public class VFXAssetSerializedData
    {
        public List<VFXSystemSerializedData> systems;
    }

    // Data models

    [Serializable]
    public class VFXDataNodeSerializedData
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
    public class VFXSystemSerializedData
    {
        public uint MaxNb;
        public float SpawnRate;
        public BlendMode BlendingMode;
        public int OrderPriority;
        public int ID;

        public List<VFXContextSerializedData> contexts;
    }

    [Serializable]
    public class VFXContextSerializedData
    {
        public string DescId;

        public Vector2 UIPosition;
        public bool UICollapsed;

        public List<VFXBlockSerializedData> blocks;
    }

    [Serializable]
    public class VFXBlockSerializedData
    {
        public string DescId;
        public bool UICollapsed;

        List<VFXPropertySerializeData> properties;   
    }

    // Properties and links

    [Serializable]
    public class VFXPropertySerializeData
    {

        List<VFXLinkSerializedData> links;
    }

    [Serializable]
    public class VFXLinkSerializedData
    {
        uint Slot;
        uint Depth;
    }
}
