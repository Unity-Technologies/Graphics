using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Experimental.Rendering.LWRP
{

    sealed public partial class Light2D : MonoBehaviour
    {
        public enum PointLightQuality
        {
            Fast,
            Accurate
        }

        //------------------------------------------------------------------------------------------
        //                                      Static
        //------------------------------------------------------------------------------------------
        static Material m_PointLightMaterial = null;
        static Material m_PointLightVolumeMaterial = null;


        //------------------------------------------------------------------------------------------
        //                                Variables/Properties
        //------------------------------------------------------------------------------------------
        public float pointLightInnerAngle
        {
            get { return m_PointLightInnerAngle; }
            set { m_PointLightInnerAngle = value; }
        }
        [SerializeField]
        private float m_PointLightInnerAngle = 360;

        public float pointLightOuterAngle
        {
            get { return m_PointLightOuterAngle; }
            set { m_PointLightOuterAngle = value; }
        }
        [SerializeField]
        private float m_PointLightOuterAngle = 360;

        public float pointLightInnerRadius
        {
            get { return m_PointLightInnerRadius; }
            set { m_PointLightInnerRadius = value; }
        }
        [SerializeField]
        private float m_PointLightInnerRadius = 0;

        public float pointLightOuterRadius
        {
            get { return m_PointLightOuterRadius; }
            set { m_PointLightOuterRadius = value; }
        }
        [SerializeField]
        private float m_PointLightOuterRadius = 1;

        public float pointLightDistance
        {
            get { return m_PointLightDistance; }
            set { m_PointLightDistance = value; }
        }
        [SerializeField]
        private float m_PointLightDistance = 3;

        public PointLightQuality pointLightQuality
        {
            get { return m_PointLightQuality; }
            set { m_PointLightQuality = value; }
        }
        [SerializeField]
        private PointLightQuality m_PointLightQuality = PointLightQuality.Accurate;


        //==========================================================================================
        //                              Functions
        //==========================================================================================

        private BoundingSphere GetPointLightBoundingSphere()
        {
            BoundingSphere boundingSphere;

            boundingSphere.radius = m_PointLightOuterRadius;
            boundingSphere.position = transform.position;

            return boundingSphere;
        }

        private Material GetPointLightVolumeMaterial()
        {
            if (m_PointLightVolumeMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/Light2d-Point-Volumetric");
                if (shader != null)
                    m_PointLightVolumeMaterial = new Material(shader);
            }

            return m_PointLightVolumeMaterial;
        }

        private Material GetPointLightMaterial()
        {
            if (m_PointLightMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/Light2D-Point");
                if (shader != null)
                    m_PointLightMaterial = new Material(shader);
                else
                    Debug.LogError("Missing shader Light2D-Point");
            }

            return m_PointLightMaterial;
        }

    }
}
