using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SerializableCubemap : ISerializationCallbackReceiver
    {
        [SerializeField]
        private string m_SerializedCubemap;

        [Serializable]
        private class CubemapHelper
        {
            public Cubemap cubemap;
        }

        Cubemap m_Cubemap;

        public Cubemap cubemap
        {
            get
            {
                if (m_Cubemap == null && !string.IsNullOrEmpty(m_SerializedCubemap))
                {
                    var cube = new CubemapHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedCubemap, cube);
                    m_Cubemap = cube.cubemap;
                    m_SerializedCubemap = null;
                }
                return m_Cubemap;
            }
            set { m_Cubemap = value; }
        }

        public void OnBeforeSerialize()
        {
            var cube = new CubemapHelper { cubemap = cubemap };
            m_SerializedCubemap = EditorJsonUtility.ToJson(cube, true);
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
