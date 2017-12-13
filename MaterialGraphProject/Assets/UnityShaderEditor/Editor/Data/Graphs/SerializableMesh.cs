using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SerializableMesh
    {
        [SerializeField]
        private string m_SerializedMesh;

        [Serializable]
        private class MeshHelper
        {
            public Mesh mesh;
        }

        public Mesh mesh
        {
            get
            {
                if (string.IsNullOrEmpty(m_SerializedMesh))
                    return null;

                var meshHelper = new MeshHelper();
                EditorJsonUtility.FromJsonOverwrite(m_SerializedMesh, meshHelper);
                return meshHelper.mesh;
            }
            set
            {
                if (mesh == value)
                    return;

                var meshHelper = new MeshHelper();
                meshHelper.mesh = value;
                m_SerializedMesh = EditorJsonUtility.ToJson(meshHelper, true);
            }
        }
    }
}
