using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.VFX
{
    [Serializable]
    public class VFXAssetSerializedData
    {
        public List<VFXSystemSerializedData> Systems = new List<VFXSystemSerializedData>();
        public List<VFXDataNodeSerializedData> DataNodes = new List<VFXDataNodeSerializedData>();
    }

    // Data models

    [Serializable]
    public class VFXDataNodeSerializedData
    {
        public bool Exposed;
        public Vector2 UIPosition;

        public List<VFXBlockSerializedData> Blocks = new List<VFXBlockSerializedData>();
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
        public uint ID;

        public List<VFXContextSerializedData> Contexts = new List<VFXContextSerializedData>();
    }

    [Serializable]
    public class VFXContextSerializedData
    {
        public string DescId;

        public Vector2 UIPosition;
        public bool UICollapsed;

        public List<VFXBlockSerializedData> Blocks = new List<VFXBlockSerializedData>();
    }

    [Serializable]
    public class VFXBlockSerializedData
    {
        public string DescId;
        public bool UICollapsed;

        List<VFXPropertySerializeData> Properties = new List<VFXPropertySerializeData>();   
    }

    // Properties and links

    [Serializable]
    public class VFXPropertySerializeData
    {
        uint SerializableID; // generated at serialization to link the slots
        List<VFXLinkSerializedData> Links = new List<VFXLinkSerializedData>();
        List<string> Values = new List<string>();
    }

    [Serializable]
    public class VFXLinkSerializedData
    {
        uint PropertyID;
        uint index;
    }
}
