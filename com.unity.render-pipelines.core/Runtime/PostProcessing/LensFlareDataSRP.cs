using UnityEngine.Serialization;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// SRPLensFlareBlendMode defined the available blend mode for each LensFlareElement
    /// </summary>
    [System.Serializable]
    public enum SRPLensFlareBlendMode
    {
        /// <summary>
        /// Additive: Blend One One
        /// </summary>
        Additive,
        /// <summary>
        /// Screen:
        ///     Blend One OneMinusSrcColor
        /// </summary>
        Screen,
        /// <summary>
        /// Premultiply:
        ///     Blend One OneMinusSrcAlpha
        ///     ColorMask RGB
        /// </summary>
        Premultiply,
        /// <summary>
        /// Lerp: Blend SrcAlpha OneMinusSrcAlpha
        /// </summary>
        Lerp
    }

    /// <summary>
    /// SRPLensFlareDistribution defined how we spread the flare element when count > 1
    /// </summary>
    [System.Serializable]
    public enum SRPLensFlareDistribution
    {
        /// <summary>
        /// Uniformly spread
        /// </summary>
        Uniform,
        /// <summary>
        /// Controlled with curved
        /// </summary>
        Curve,
        /// <summary>
        /// Random distribution
        /// </summary>
        Random
    }

    /// <summary>
    /// SRPLensFlareType which can be an image of a procedural shape
    /// If change order or add new member, need to update preview
    /// shader: LensFlareDataDrivenPreview.shader
    /// </summary>
    [System.Serializable]
    public enum SRPLensFlareType
    {
        /// <summary>
        /// Image from a file or a RenderTexture
        /// </summary>
        Image,
        /// <summary>
        /// Procedural Circle
        /// </summary>
        Circle,
        /// <summary>
        /// Polygon
        /// </summary>
        Polygon
    }

    /// <summary>
    /// LensFlareDataElementSRP defines collection of parameters describing the behavior a Lens Flare Element.
    /// </summary>
    [System.Serializable]
    public sealed class LensFlareDataElementSRP
    {
        /// <summary> Initialize default values </summary>
        public LensFlareDataElementSRP()
        {
            visible = true;

            localIntensity = 1.0f;
            position = 0.0f;
            positionOffset = new Vector2(0.0f, 0.0f);
            angularOffset = 0.0f;
            translationScale = new Vector2(1.0f, 1.0f);
            lensFlareTexture = null;
            uniformScale = 1.0f;
            sizeXY = Vector2.one;
            allowMultipleElement = false;
            count = 5;
            rotation = 0.0f;
            tint = new Color(1.0f, 1.0f, 1.0f, 0.5f);
            blendMode = SRPLensFlareBlendMode.Additive;
            autoRotate = false;
            isFoldOpened = true;
            flareType = SRPLensFlareType.Circle;

            distribution = SRPLensFlareDistribution.Uniform;

            lengthSpread = 1f;
            colorGradient = new Gradient();
            colorGradient.SetKeys(new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) });
            positionCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f, 1.0f, 1.0f), new Keyframe(1.0f, 1.0f, 1.0f, -1.0f));
            scaleCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));
            uniformAngleCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 0.0f));

            // Random
            seed = 0;
            intensityVariation = 0.75f;
            positionVariation = new Vector2(1.0f, 0.0f);
            scaleVariation = 1.0f;
            rotationVariation = 180.0f;

            // Distortion
            enableRadialDistortion = false;
            targetSizeDistortion = Vector2.one;
            distortionCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f, 1.0f, 1.0f), new Keyframe(1.0f, 1.0f, 1.0f, -1.0f));
            distortionRelativeToCenter = false;

            // Parameters for Procedural
            fallOff = 1.0f;
            edgeOffset = 0.1f;
            sdfRoundness = 0.0f;
            sideCount = 6;
            inverseSDF = false;
        }

        /// <summary> Visibility checker for current element </summary>
        public bool visible;

        /// <summary> Position </summary>
        public float position;

        /// <summary> Position offset </summary>
        public Vector2 positionOffset;

        /// <summary> Angular offset </summary>
        public float angularOffset;

        /// <summary> Translation Scale </summary>
        public Vector2 translationScale;

        [Min(0), SerializeField, FormerlySerializedAs("localIntensity")]
        float m_LocalIntensity;

        /// <summary> Intensity of this element </summary>
        public float localIntensity
        {
            get => m_LocalIntensity;
            set => m_LocalIntensity = Mathf.Max(0, value);
        }

        /// <summary> Texture used to for this Lens Flare Element </summary>
        public Texture lensFlareTexture;

        /// <summary> Uniform scale applied </summary>
        public float uniformScale;

        /// <summary> Scale size on each dimension </summary>
        public Vector2 sizeXY;

        /// <summary> Enable multiple elements </summary>
        public bool allowMultipleElement;

        [Min(1), SerializeField, FormerlySerializedAs("count")]
        int m_Count;

        /// <summary> Element can be repeated 'count' times </summary>
        public int count
        {
            get => m_Count;
            set => m_Count = Mathf.Max(1, value);
        }

        /// <summary> Preserve  Aspect Ratio </summary>
        public bool preserveAspectRatio;

        /// <summary> Local rotation of the texture </summary>
        public float rotation;

        /// <summary> Tint of the texture can be modulated by the light we are attached to </summary>
        public Color tint;

        /// <summary> Blend mode used </summary>
        public SRPLensFlareBlendMode blendMode;

        /// <summary> Rotate the texture relative to the angle on the screen (the rotation will be added to the parameter 'rotation') </summary>
        public bool autoRotate;

        /// <summary> FlareType used </summary>
        public SRPLensFlareType flareType;

        /// <summary>  Modulate by light color if the asset is used in a 'SRP Lens Flare Source Override' </summary>
        public bool modulateByLightColor;

#pragma warning disable 0414 // never used (editor state)
        /// <summary> Internal value use to store the state of minimized or maximized LensFlareElement </summary>
        [SerializeField]
        bool isFoldOpened;
#pragma warning restore 0414

        /// <summary> SRPLensFlareDistribution defined how we spread the flare element when count > 1 </summary>
        public SRPLensFlareDistribution distribution;

        /// <summary> Length to spread the distribution of flares, spread start at 'starting position' </summary>
        public float lengthSpread;

        /// <summary> Curve describing how to place flares distribution (Used only for Uniform and Curve 'distribution') </summary>
        public AnimationCurve positionCurve;

        /// <summary> Curve describing how to scale flares distribution (Used only for Uniform and Curve 'distribution') </summary>
        public AnimationCurve scaleCurve;

        /// <summary> Seed used to seed randomness </summary>
        public int seed;

        /// <summary> Colors used uniformly for Uniform or Curve Distribution and Random when the distribution is 'Random'. </summary>
        public Gradient colorGradient;

        [Range(0, 1), SerializeField, FormerlySerializedAs("intensityVariation")]
        float m_IntensityVariation;

        /// <summary> Scale factor applied on the variation of the intensities. </summary>
        public float intensityVariation
        {
            get => m_IntensityVariation;
            set => m_IntensityVariation = Mathf.Max(0, value);
        }

        /// <summary> Scale factor applied on the variation of the positions. </summary>
        public Vector2 positionVariation;

        /// <summary> Coefficient applied on the variation of the scale (relative to the current scale). </summary>
        public float scaleVariation;

        /// <summary> Scale factor applied on the variation of the rotation (relative to the current rotation or auto-rotate). </summary>
        public float rotationVariation;

        /// <summary> True to use or not the radial distortion. </summary>
        public bool enableRadialDistortion;

        /// <summary> Target size used on the edge of the screen. </summary>
        public Vector2 targetSizeDistortion;

        /// <summary> Curve blending from screen center to the edges of the screen. </summary>
        public AnimationCurve distortionCurve;

        /// <summary> If true the distortion is relative to center of the screen otherwise relative to lensFlare source screen position. </summary>
        public bool distortionRelativeToCenter;

        [Range(0, 1), SerializeField, FormerlySerializedAs("fallOff")]
        float m_FallOff;

        /// <summary> Fall of the gradient used for the Procedural Flare. </summary>
        public float fallOff
        {
            get => m_FallOff;
            set => m_FallOff = Mathf.Clamp01(value);
        }

        [Range(0, 1), SerializeField, FormerlySerializedAs("edgeOffset")]
        float m_EdgeOffset;

        /// <summary> Gradient Offset used for the Procedural Flare. </summary>
        public float edgeOffset
        {
            get => m_EdgeOffset;
            set => m_EdgeOffset = Mathf.Clamp01(value);
        }

        [Min(3), SerializeField, FormerlySerializedAs("sideCount")]
        int m_SideCount;

        /// <summary> Side count of the regular polygon generated. </summary>
        public int sideCount
        {
            get => m_SideCount;
            set => m_SideCount = Mathf.Max(3, value);
        }

        [Range(0, 1), SerializeField, FormerlySerializedAs("sdfRoundness")]
        float m_SdfRoundness;

        /// <summary> Roundness of the polygon flare (0: Sharp Polygon, 1: Circle). </summary>
        public float sdfRoundness
        {
            get => m_SdfRoundness;
            set => m_SdfRoundness = Mathf.Clamp01(value);
        }

        /// <summary> Inverse the gradient direction. </summary>
        public bool inverseSDF;

        /// <summary> Uniform angle (in degrees) used with multiple element enabled with Uniform distribution. </summary>
        public float uniformAngle;

        /// <summary> Uniform angle (remap from -180.0f to 180.0f) used with multiple element enabled with Curve distribution. </summary>
        public AnimationCurve uniformAngleCurve;
    }

    /// <summary> LensFlareDataSRP defines a Lens Flare with a set of LensFlareDataElementSRP </summary>
    [System.Serializable]
    public sealed class LensFlareDataSRP : ScriptableObject
    {
        /// <summary> Initialize default value </summary>
        public LensFlareDataSRP()
        {
            elements = null;
        }

        /// <summary> List of LensFlareDataElementSRP </summary>
        public LensFlareDataElementSRP[] elements;
    }
}
