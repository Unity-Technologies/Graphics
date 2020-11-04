using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public sealed partial class Light2D
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

#if USING_ANIMATION_MODULE        
        [UnityEngine.Animations.NotKeyable]
#endif
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

        internal bool isPointLight => m_LightType == LightType.Point;
    }
}
