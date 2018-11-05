using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class LightReactor : MonoBehaviour
    {
        [SerializeField]
        private bool m_RecievesShadows;
        [SerializeField]
        private bool m_CastsShadows;

        [SerializeField]
        private int[] m_ShadowMeshTriangles;
        [SerializeField]
        private Vector3[] m_ShadowMeshVertices;
        [SerializeField]
        private Vector3[] m_ShadowMeshNormals;

        private Mesh m_ShadowMesh;

        //public void CreateShadowMesh()
        //{

        //}

        //public void Awake()
        //{

        //}
    }
}