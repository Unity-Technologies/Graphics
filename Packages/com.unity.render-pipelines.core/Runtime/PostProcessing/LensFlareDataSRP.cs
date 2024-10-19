using NUnit.Framework;
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
    [GenerateHLSL]
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
        Polygon,
        /// <summary>
        /// shape as a ring
        /// </summary>
        Ring,
        /// <summary>
        /// Hoop
        /// </summary>
        LensFlareDataSRP
    }

    /// <summary>
    /// SRPLensFlareColorType describe how to colorize LensFlare
    /// </summary>
    [System.Serializable]
    [GenerateHLSL]
    public enum SRPLensFlareColorType
    {
        /// <summary>
        /// Constant Color
        /// </summary>
        Constant = 0,
        /// <summary>
        /// Radial Gradient
        /// </summary>
        RadialGradient,
        /// <summary>
        /// Angular Gradient
        /// </summary>
        AngularGradient
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
            lensFlareDataSRP = null;

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
            preserveAspectRatio = false;

            // Ring
            ringThickness = 0.25f;

            // Hoop
            hoopFactor = 1.0f;

            // Shimmer
            noiseAmplitude = 1.0f;
            noiseFrequency = 1;
            noiseSpeed = 0;

            shapeCutOffSpeed = 0.0f;
            shapeCutOffRadius = 10.0f;

            tintColorType = SRPLensFlareColorType.Constant;
            tint = new Color(1.0f, 1.0f, 1.0f, 0.5f);
            tintGradient = new TextureGradient(
                new GradientColorKey[] { new GradientColorKey(Color.black, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) });
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
            uniformAngle = 0.0f;
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

        /// <summary>
        /// Clone the current LensFlareDataElementSRP.
        /// </summary>
        /// <returns>Cloned LensFlareDataElementSRP.</returns>
        public LensFlareDataElementSRP Clone()
        {
            LensFlareDataElementSRP clone = new LensFlareDataElementSRP();
            clone.lensFlareDataSRP = lensFlareDataSRP;

            clone.visible = visible;

            clone.localIntensity = localIntensity;
            clone.position = position;
            clone.positionOffset = positionOffset;
            clone.angularOffset = angularOffset;
            clone.translationScale = translationScale;
            clone.lensFlareTexture = lensFlareTexture;
            clone.uniformScale = uniformScale;
            clone.sizeXY = sizeXY;
            clone.allowMultipleElement = allowMultipleElement;
            clone.count = count;
            clone.rotation = rotation;
            clone.preserveAspectRatio = preserveAspectRatio;

            clone.ringThickness = ringThickness;

            clone.hoopFactor = hoopFactor;

            clone.noiseAmplitude = noiseAmplitude;
            clone.noiseFrequency = noiseFrequency;
            clone.noiseSpeed = noiseSpeed;

            clone.shapeCutOffSpeed = shapeCutOffSpeed;
            clone.shapeCutOffRadius = shapeCutOffRadius;

            clone.tintColorType = tintColorType;
            clone.tint = tint;
            clone.tintGradient = new TextureGradient(tintGradient.colorKeys, tintGradient.alphaKeys, tintGradient.mode, tintGradient.colorSpace, tintGradient.textureSize);
            clone.tintGradient = new TextureGradient(tintGradient.colorKeys, tintGradient.alphaKeys);
            clone.blendMode = blendMode;

            clone.autoRotate = autoRotate;
            clone.isFoldOpened = isFoldOpened;
            clone.flareType = flareType;

            clone.distribution = distribution;

            clone.lengthSpread = lengthSpread;
            clone.colorGradient = new Gradient();
            clone.colorGradient.SetKeys(colorGradient.colorKeys, colorGradient.alphaKeys);
            clone.colorGradient.mode =  colorGradient.mode;
            clone.colorGradient.colorSpace = colorGradient.colorSpace;
            clone.positionCurve = new AnimationCurve(positionCurve.keys);
            clone.scaleCurve = new AnimationCurve(scaleCurve.keys);
            clone.uniformAngle = uniformAngle;
            clone.uniformAngleCurve = new AnimationCurve(uniformAngleCurve.keys);

            clone.seed = seed;
            clone.intensityVariation = intensityVariation;
            clone.positionVariation = positionVariation;
            clone.scaleVariation = scaleVariation;
            clone.rotationVariation = rotationVariation;

            clone.enableRadialDistortion = enableRadialDistortion;
            clone.targetSizeDistortion = targetSizeDistortion;
            clone.distortionCurve = new AnimationCurve(distortionCurve.keys);
            clone.distortionRelativeToCenter = distortionRelativeToCenter;

            clone.fallOff = fallOff;
            clone.edgeOffset = edgeOffset;
            clone.sdfRoundness = sdfRoundness;
            clone.sideCount = sideCount;
            clone.inverseSDF = inverseSDF;

            return clone;
        }

        /// <summary> Current Element is himselft another LensFlareDataSRP </summary>
        public LensFlareDataSRP lensFlareDataSRP;

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

        // For Hoop
        /// <summary>Ring thickness</summary>
        [Range(0.0f, 1.0f)]
        public float ringThickness;

        /// <summary>Hoop thickness</summary>
        [Range(-1.0f, 1.0f)]
        public float hoopFactor;

        // For Ring
        /// <summary>Noise parameter amplitude</summary>
        public float noiseAmplitude;
        /// <summary>Noise parameter frequency</summary>
        public int noiseFrequency;
        /// <summary>Noise parameter Speed</summary>
        public float noiseSpeed;

        /// <summary>To simulate the cutoff of the flare by the circular shape of the lens. How quickly this cutoff happen.</summary>
        public float shapeCutOffSpeed;
        /// <summary>To simulate the cutoff of the flare by the circular shape of the lens.</summary>
        public float shapeCutOffRadius;

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

        /// <summary>Specify how to tint flare.</summary>
        public SRPLensFlareColorType tintColorType;

        /// <summary> Tint of the texture can be modulated by the light we are attached to</summary>
        public Color tint;

        /// <summary> Tint radially of the texture can be modulated by the light we are attached to . </summary>
        public TextureGradient tintGradient;

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
    [CurrentPipelineHelpURL("shared/lens-flare/lens-flare-asset")]
    [System.Serializable]
    public sealed class LensFlareDataSRP : ScriptableObject
    {
        /// <summary> Initialize default value </summary>
        public LensFlareDataSRP()
        {
            elements = null;
        }

        /// <summary>
        /// Check if we have at last one 'modulatedByLightColor' enabled.
        /// </summary>
        /// <returns>true if we have at least one 'modulatedByLightColor' on the asset.</returns>
        public bool HasAModulateByLightColorElement()
        {
            if (elements != null)
            {
                foreach (LensFlareDataElementSRP e in elements)
                {
                    if (e.modulateByLightColor)
                        return true;
                }
            }

            return false;
        }

        /// <summary> List of LensFlareDataElementSRP </summary>
        public LensFlareDataElementSRP[] elements;
    }
}
