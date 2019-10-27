using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Experimental.Rendering.Universal
{
    sealed public partial class Light2D : MonoBehaviour
    {
        public enum PointLightQuality
        {
            Fast = 0,
            Accurate = 1
        }

        [SerializeField] float m_PointLightInnerAngle = 360.0f;
        [SerializeField] float m_PointLightOuterAngle = 360.0f;
        [SerializeField] float m_PointLightInnerRadius = 0.0f;
        [SerializeField] float m_PointLightOuterRadius = 1.0f;
        [SerializeField] float m_PointLightDistance = 3.0f;

        [UnityEngine.Animations.NotKeyable]
        [SerializeField] PointLightQuality m_PointLightQuality = PointLightQuality.Accurate;

        public float pointLightInnerAngle
        {
            get => m_PointLightInnerAngle;
            set => m_PointLightInnerAngle = value;
        }

        public float pointLightOuterAngle
        {
            get => m_PointLightOuterAngle;
            set => m_PointLightOuterAngle = value;
        }

        public float pointLightInnerRadius
        {
            get => m_PointLightInnerRadius;
            set => m_PointLightInnerRadius = value;
        }

        public float pointLightOuterRadius
        {
            get => m_PointLightOuterRadius;
            set => m_PointLightOuterRadius = value;
        }

        public float pointLightDistance => m_PointLightDistance;
        public PointLightQuality pointLightQuality => m_PointLightQuality;

        private BoundingSphere GetPointLightBoundingSphere()
        {
            BoundingSphere boundingSphere;

            boundingSphere.radius = m_PointLightOuterRadius;
            boundingSphere.position = transform.position;

            return boundingSphere;
        }
    }
}
