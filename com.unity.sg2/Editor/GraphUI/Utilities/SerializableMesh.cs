using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Serializable]
    class SerializableMesh : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_SerializedMesh;

        [SerializeField]
        string m_Guid;

        [NonSerialized]
        Mesh m_Mesh;

        [Serializable]
        class MeshHelper
        {
#pragma warning disable 649
            public Mesh mesh;
#pragma warning restore 649
        }
        public bool IsNotInitialized
            => string.IsNullOrEmpty(m_SerializedMesh)
                && string.IsNullOrEmpty(m_Guid)
                && m_Mesh == null;

        public Mesh mesh
        {
            get
            {
                // If using one of the preview meshes
                if (!string.IsNullOrEmpty(m_SerializedMesh))
                {
                    var meshHelper = new MeshHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedMesh, meshHelper);
                    m_SerializedMesh = null;
                    m_Guid = null;
                    m_Mesh = meshHelper.mesh;
                }
                // If using custom mesh asset
                else if (!string.IsNullOrEmpty(m_Guid) && m_Mesh == null)
                {
                    m_Mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(m_Guid));
                    m_Guid = null;
                }
                else
                {
                    m_Mesh = Resources.GetBuiltinResource(typeof(Mesh), $"Sphere.fbx") as Mesh;
                }

                return m_Mesh;
            }
            set
            {
                m_Mesh = value;
                m_Guid = null;
                m_SerializedMesh = null;
            }
        }

        public void OnBeforeSerialize()
        {
            m_SerializedMesh = EditorJsonUtility.ToJson(new MeshHelper { mesh = mesh }, false);
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
