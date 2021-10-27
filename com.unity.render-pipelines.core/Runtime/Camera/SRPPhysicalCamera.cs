using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Holds the physical settings set on cameras.
    /// </summary>
    [Serializable]
    public class SRPPhysicalCamera
    {
        /// <summary>
        /// The minimum allowed aperture.
        /// </summary>
        public const float kMinAperture = 0.7f;

        /// <summary>
        /// The maximum allowed aperture.
        /// </summary>
        public const float kMaxAperture = 32f;

        /// <summary>
        /// The minimum blade count for the aperture diaphragm.
        /// </summary>
        public const int kMinBladeCount = 3;

        /// <summary>
        /// The maximum blade count for the aperture diaphragm.
        /// </summary>
        public const int kMaxBladeCount = 11;

        // Camera body
        [SerializeField][Min(1f)] int m_Iso;
        [SerializeField][Min(0f)] float m_ShutterSpeed;

        // Lens
        // Note: focalLength is already defined in the regular camera component
        [SerializeField][Range(kMinAperture, kMaxAperture)] float m_Aperture;
        [SerializeField][Min(0.1f)] float m_FocusDistance;

        // Aperture shape
        [SerializeField][Range(kMinBladeCount, kMaxBladeCount)] int m_BladeCount;
        [SerializeField] Vector2 m_Curvature;
        [SerializeField][Range(0f, 1f)] float m_BarrelClipping;
        [SerializeField][Range(-1f, 1f)] float m_Anamorphism;

        /// <summary>
        /// The focus distance of the lens. The Depth of Field Volume override uses this value if you set focusDistanceMode to FocusDistanceMode.Camera.
        /// </summary>
        public float focusDistance
        {
            get => m_FocusDistance;
            set => m_FocusDistance = Mathf.Max(value, 0.1f);
        }

        /// <summary>
        /// The sensor sensitivity (ISO).
        /// </summary>
        public int iso
        {
            get => m_Iso;
            set => m_Iso = Mathf.Max(value, 1);
        }

        /// <summary>
        /// The exposure time, in second.
        /// </summary>
        public float shutterSpeed
        {
            get => m_ShutterSpeed;
            set => m_ShutterSpeed = Mathf.Max(value, 0f);
        }

        /// <summary>
        /// The aperture number, in f-stop.
        /// </summary>
        public float aperture
        {
            get => m_Aperture;
            set => m_Aperture = Mathf.Clamp(value, kMinAperture, kMaxAperture);
        }

        /// <summary>
        /// The number of diaphragm blades.
        /// </summary>
        public int bladeCount
        {
            get => m_BladeCount;
            set => m_BladeCount = Mathf.Clamp(value, kMinBladeCount, kMaxBladeCount);
        }

        /// <summary>
        /// Maps an aperture range to blade curvature.
        /// </summary>
        public Vector2 curvature
        {
            get => m_Curvature;
            set
            {
                m_Curvature.x = Mathf.Max(value.x, kMinAperture);
                m_Curvature.y = Mathf.Min(value.y, kMaxAperture);
            }
        }

        /// <summary>
        /// The strength of the "cat eye" effect on bokeh (optical vignetting).
        /// </summary>
        public float barrelClipping
        {
            get => m_BarrelClipping;
            set => m_BarrelClipping = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Stretches the sensor to simulate an anamorphic look. Positive values distort the Camera
        /// vertically, negative will distort the Camera horizontally.
        /// </summary>
        public float anamorphism
        {
            get => m_Anamorphism;
            set => m_Anamorphism = Mathf.Clamp(value, -1f, 1f);
        }

        /// <summary>
        /// Copies the settings of this instance to another instance.
        /// </summary>
        /// <param name="c">The instance to copy the settings to.</param>
        [Obsolete("The CopyTo method is obsolete and does not work anymore. Use the assignement operator instead to get a copy of the HDPhysicalCamera parameters.", true)]
        public void CopyTo(SRPPhysicalCamera c)
        { }

        /// <summary>
        /// A set of default physical camera parameters.
        /// </summary>
        /// <returns>Returns a set of default physical camera parameters.</returns>
        public static SRPPhysicalCamera GetDefaults()
        {
            SRPPhysicalCamera val = new SRPPhysicalCamera
            {
                iso = 200,
                shutterSpeed = 1f / 200f,
                aperture = 16,
                focusDistance = 10,
                bladeCount = 5,
                curvature = new Vector2(2f, 11f),
                barrelClipping = 0.25f,
                anamorphism = 0
            };

            return val;
        }
    }
}
