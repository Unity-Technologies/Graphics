using System;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class Light2D
    {
        [SerializeField] float m_PointLightInnerAngle = 360.0f;
        [SerializeField] float m_PointLightOuterAngle = 360.0f;
        [SerializeField] float m_PointLightInnerRadius = 0.0f;
        [SerializeField] float m_PointLightOuterRadius = 1.0f;

        /// <summary>
        /// The inner angle of the point light shape. The bigger the angle, the wider the gap.
        /// The gap between the innner and outer angle will determine the size of the light's penumbra.
        /// </summary>
        public float pointLightInnerAngle
        {
            get => m_PointLightInnerAngle;
            set => m_PointLightInnerAngle = value;
        }

        /// <summary>
        /// The angle that determins the shape of the inner light area.
        /// The gap between the innner and outer angle will determine the size of the light's penumbra.
        /// </summary>
        public float pointLightOuterAngle
        {
            get => m_PointLightOuterAngle;
            set => m_PointLightOuterAngle = value;
        }

        /// <summary>
        /// The radius of the inner light area that has full brightness.
        /// The gap between the inner and outer radius will determine the size of the light's penumbra.
        /// </summary>
        public float pointLightInnerRadius
        {
            get => m_PointLightInnerRadius;
            set => m_PointLightInnerRadius = value;
        }

        /// <summary>
        /// The outer radius that determines the size of the light.
        /// The gap between the inner and outer radius will determine the size of the light's penumbra.
        /// </summary>
        public float pointLightOuterRadius
        {
            get => m_PointLightOuterRadius;
            set => m_PointLightOuterRadius = value;
        }

        [Obsolete("pointLightDistance has been changed to normalMapDistance", true)]
        public float pointLightDistance => m_NormalMapDistance;

        [Obsolete("pointLightQuality has been changed to normalMapQuality", true)]
        public NormalMapQuality pointLightQuality => m_NormalMapQuality;


        internal bool isPointLight => m_LightType == LightType.Point;
    }
}
