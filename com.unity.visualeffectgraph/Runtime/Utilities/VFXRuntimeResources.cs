#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VFX;
#endif


namespace UnityEngine.VFX
{
    class VFXRuntimeResources : ScriptableObject
    {
        [SerializeField]
        ComputeShader m_SDFRayMapCS;
        [SerializeField]
        ComputeShader m_SDFNormalsCS;
        [SerializeField]
        Shader m_SDFRayMapShader;

        internal ComputeShader sdfRayMapCS
        {
            get
            {
                return m_SDFRayMapCS;
            }
            set
            {
                m_SDFRayMapCS = value;
            }
        }

        internal ComputeShader sdfNormalsCS
        {
            get
            {
                return m_SDFNormalsCS;
            }
            set
            {
                m_SDFNormalsCS = value;
            }
        }

        internal Shader sdfRayMapShader
        {
            get
            {
                return m_SDFRayMapShader;
            }
            set
            {
                m_SDFRayMapShader = value;
            }
        }

        public static VFXRuntimeResources runtimeResources
        {
            get
            {
                return VFXManager.runtimeResources as VFXRuntimeResources;
            }
        }
    }
}
