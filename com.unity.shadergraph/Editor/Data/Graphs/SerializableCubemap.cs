using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SerializableCubemap
    {
        [SerializeField]
        private string m_SerializedCubemap;

        [Serializable]
        private class CubemapHelper
        {
            public Cubemap cubemap;
        }

        public Cubemap cubemap
        {
            get
            {
                if (string.IsNullOrEmpty(m_SerializedCubemap))
                    return null;

                var cube = new CubemapHelper();
                EditorJsonUtility.FromJsonOverwrite(m_SerializedCubemap, cube);
                return cube.cubemap;
            }
            set
            {
                if(cubemap == value)
                    return;

                var cubemapHelper = new CubemapHelper();
                cubemapHelper.cubemap = value;
                m_SerializedCubemap = EditorJsonUtility.ToJson(cubemapHelper, true);
            }
        }
    }
}
