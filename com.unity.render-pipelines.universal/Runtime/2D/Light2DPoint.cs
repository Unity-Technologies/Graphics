using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public sealed partial class Light2D
    {
        [SerializeField] float m_PointLightInnerAngle = 360.0f;
        [SerializeField] float m_PointLightOuterAngle = 360.0f;
        [SerializeField] float m_PointLightInnerRadius = 0.0f;
        [SerializeField] float m_PointLightOuterRadius = 1.0f;

        /// <summary>
        /// 
        /// </summary>
        public float pointLightInnerAngle
        {
            get => m_PointLightInnerAngle;
            set => m_PointLightInnerAngle = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public float pointLightOuterAngle
        {
            get => m_PointLightOuterAngle;
            set => m_PointLightOuterAngle = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public float pointLightInnerRadius
        {
            get => m_PointLightInnerRadius;
            set => m_PointLightInnerRadius = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public float pointLightOuterRadius
        {
            get => m_PointLightOuterRadius;
            set => m_PointLightOuterRadius = value;
        }

        /// <summary>
        /// 
        /// </summary>
        [Obsolete("pointLightDistance has been changed to normalMapDistance", true)]
        public float pointLightDistance => m_NormalMapDistance;

        /// <summary>
        /// 
        /// </summary>
        [Obsolete("pointLightQuality has been changed to normalMapQuality", true)]
        public NormalMapQuality pointLightQuality => m_NormalMapQuality;


        internal bool isPointLight => m_LightType == LightType.Point;
    }
}
