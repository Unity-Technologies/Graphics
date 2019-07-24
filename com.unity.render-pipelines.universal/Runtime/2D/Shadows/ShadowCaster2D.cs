using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Experimental.Rendering.Universal
{
    public abstract class ShadowCaster2D : MonoBehaviour
    {
        internal IShadowCasterGroup2D m_ShadowCasterGroup = null;

        [SerializeField] Vector3[] m_ShapePath;
        [SerializeField] int m_ShapePathHash = 0;
        [SerializeField] int m_PreviousPathHash = 0;
        [SerializeField] Mesh m_Mesh;

        internal Mesh mesh => m_Mesh; 
        internal Vector3[] shapePath => m_ShapePath;
        internal int shapePathHash { get { return m_ShapePathHash; } set { m_ShapePathHash = value; } }

        Mesh m_ShadowMesh;

        private void Awake()
        {
            if (m_ShapePath == null || m_ShapePath.Length == 0)
                m_ShapePath = new Vector3[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };
        }

        protected void OnEnable()
        {
            if (m_Mesh == null)
            {
                m_Mesh = new Mesh();
                ShadowUtility.GenerateShadowMesh(ref m_Mesh, m_ShapePath);
                m_PreviousPathHash = m_ShapePathHash;
            }

            LightUtility.AddToShadowCasterToGroup(this, out m_ShadowCasterGroup);
        }

        protected void OnDisable()
        {
            LightUtility.RemoveShadowCasterFromGroup(this, m_ShadowCasterGroup);
        }

        protected void Update()
        {
            bool rebuildMesh = false;
            rebuildMesh |= LightUtility.CheckForChange(m_ShapePathHash, ref m_PreviousPathHash);

            if (rebuildMesh)
                ShadowUtility.GenerateShadowMesh(ref m_Mesh, m_ShapePath);
        }
    }
}
