using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class InspectorPreviewData
    {
        public SerializableMesh serializedMesh = new SerializableMesh();

        [NonSerialized]
        public Quaternion rotation = Quaternion.identity;

        [NonSerialized]
        public float scale = 1f;
    }
}
