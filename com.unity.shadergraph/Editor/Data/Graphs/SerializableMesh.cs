using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
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

        public Mesh mesh
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SerializedMesh))
                {
                    var textureHelper = new MeshHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedMesh, textureHelper);
                    m_SerializedMesh = null;
                    m_Guid = null;
                    m_Mesh = textureHelper.mesh;
                }
                else if (!string.IsNullOrEmpty(m_Guid) && m_Mesh == null)
                {
                    m_Mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(m_Guid));
                    m_Guid = null;
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
