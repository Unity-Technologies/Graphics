#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    public enum ReflectionInfluenceShape { Box, Sphere };
    [ExecuteInEditMode]
    [RequireComponent(typeof(ReflectionProbe), typeof(MeshFilter), typeof(MeshRenderer))]
    public class HDAdditionalReflectionData : MonoBehaviour
    {
        public ReflectionInfluenceShape m_InfluenceShape;
        [Range(0.0f,1.0f)]
        public float m_Dimmer = 1.0f;
        public float m_InfluenceSphereRadius = 3.0f;
        public float m_SphereReprojectionVolumeRadius = 1.0f;
        public bool m_UseSeparateProjectionVolume = false;
        public Vector3 m_BoxReprojectionVolumeSize = Vector3.one;
        public Vector3 m_BoxReprojectionVolumeCenter = Vector3.zero;
        public float m_MaxSearchDistance = 8.0f;
        private MeshRenderer m_PreviewMeshRenderer;
        public Texture m_PreviewCubemap;
        private MeshFilter m_PreviewMeshFilter;
        private static Mesh m_SphereMesh;
        private static Material m_PreviewMaterial;
        private bool m_Visible;


#if UNITY_EDITOR
        private static Mesh sphereMesh
        {
            get { return m_SphereMesh ?? (m_SphereMesh = Resources.GetBuiltinResource(typeof(Mesh), "New-Sphere.fbx") as Mesh); }
        }

        public Material previewMaterial
        {
            get
            {
                if (m_PreviewMaterial == null )
                {
                    //m_PreviewMaterial = (Material)Instantiate(AssetDatabase.LoadAssetAtPath("Assets/ScriptableRenderPipeline/ScriptableRenderPipeline/HDRenderPipeline/Debug/PreviewCubemapMaterial.mat", typeof(Material)));
                    m_PreviewMaterial = new Material(Shader.Find("Debug/ReflectionProbePreview"));
                }
                if(m_PreviewCubemap != null)
                    m_PreviewMaterial.SetTexture("_Cubemap", m_PreviewCubemap);
                m_PreviewMaterial.hideFlags = HideFlags.HideAndDontSave;
                return m_PreviewMaterial;
            }
        }

        private void OnEnable()
        {
            m_PreviewMeshFilter = gameObject.GetComponent<MeshFilter>() != null ? gameObject.GetComponent<MeshFilter>() : gameObject.AddComponent<MeshFilter>();
            m_PreviewMeshRenderer = gameObject.GetComponent<MeshRenderer>() != null ? gameObject.GetComponent<MeshRenderer>() : gameObject.AddComponent<MeshRenderer>();
            m_PreviewMeshFilter.sharedMesh = sphereMesh;
            //m_PreviewMeshRenderer.sharedMaterial = previewMaterial;
            m_PreviewMeshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            m_PreviewMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_PreviewMeshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        public void ChangeVisibility(bool visible)
        {
            if(visible != m_Visible)
            {
                m_PreviewMeshRenderer.sharedMaterial = previewMaterial;
                m_PreviewMeshRenderer.enabled = visible ? true : false;
                m_Visible = visible;
            }
        }
#endif
    }
}
