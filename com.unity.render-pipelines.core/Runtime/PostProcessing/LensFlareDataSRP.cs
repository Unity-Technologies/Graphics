using System.IO;
using UnityEditor;

namespace UnityEngine
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
        /// <summary>
        /// Initialize default values
        /// </summary>
        public LensFlareDataElementSRP()
        {
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

            // Random
            seed = 0;
            intensityVariation = 0.0f;
            positionVariation = new Vector2(0.0f, 0.0f);
            scaleVariation = 0.0f;
            rotationVariation = 0.0f;

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
        /// Position
        /// </summary>
        [Range(-1.0f, 1.0f)]
        public float position;
        /// <summary>
        /// Position offset
        /// </summary>
        public Vector2 positionOffset;
        /// <summary>
        /// Angular offset
        /// </summary>
        public float angularOffset;
        /// <summary>
        /// Translation Scale
        /// </summary>
        public Vector2 translationScale;
        /// <summary>
        /// Intensity of this element
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float localIntensity;
        /// <summary>
        /// Texture used to for this Lens Flare Element
        /// </summary>
        public Texture lensFlareTexture;
        /// <summary>
        /// Uniform scale applied
        /// </summary>
        public float uniformScale;
        /// <summary>
        /// Scale size on each dimension
        /// </summary>
        public Vector2 sizeXY;
        /// <summary>
        /// Enable multiple elements
        /// </summary>
        public bool allowMultipleElement;
        /// <summary>
        /// Element can be repeated 'count' times
        /// </summary>
        [Min(1)]
        public int count;
        /// <summary>
        /// Preserve  Aspect Ratio
        /// </summary>
        public bool preserveAspectRatio;
        /// <summary>
        /// Local rotation of the texture
        /// </summary>
        [Range(0, 360)]
        public float rotation;
        /// <summary>
        /// Tint of the texture can be modulated by the light we are attached to
        /// </summary>
        public Color tint;
        /// <summary>
        /// Blend mode used
        /// </summary>
        public SRPLensFlareBlendMode blendMode;
        /// <summary>
        /// Rotate the texture relative to the angle on the screen (the rotation will be added to the parameter 'rotation')
        /// </summary>
        public bool autoRotate;
        /// <summary>
        /// FlareType used
        /// </summary>
        public SRPLensFlareType flareType;
        /// <summary>
        /// Modulate by light color if the asset is used in a 'SRP Lens Flare Source Override'
        /// </summary>
        public bool modulateByLightColor;
        /// <summary>
        /// Internal value use to store the state of minimized or maximized LensFlareElement
        /// </summary>
        public bool isFoldOpened;
        /// <summary>
        /// SRPLensFlareDistribution defined how we spread the flare element when count > 1
        /// </summary>
        public SRPLensFlareDistribution distribution;
        /// <summary>
        /// Length to spread the distribution of flares, spread start at 'starting position'
        /// </summary>
        public float lengthSpread;
        /// <summary>
        /// Curve describing how to place flares distribution (Used only for Uniform and Curve 'distribution')
        /// </summary>
        public AnimationCurve positionCurve;
        /// <summary>
        /// Curve describing how to scale flares distribution (Used only for Uniform and Curve 'distribution')
        /// </summary>
        public AnimationCurve scaleCurve;
        /// <summary>
        /// Seed used to seed randomness
        /// </summary>
        public int seed;
        /// <summary>
        /// Colors used uniformly for Uniform or Curve Distribution and Random when the distribution is 'Random'.
        /// </summary>
        public Gradient colorGradient;
        /// <summary>
        /// Scale factor applied on the variation of the intensities.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float intensityVariation;
        /// <summary>
        /// Scale factor applied on the variation of the positions.
        /// </summary>
        public Vector2 positionVariation;
        /// <summary>
        /// Coefficient applied on the variation of the scale (relative to the current scale).
        /// </summary>
        public float scaleVariation;
        /// <summary>
        /// Scale factor applied on the variation of the rotation (relative to the current rotation or auto-rotate).
        /// </summary>
        public float rotationVariation;
        /// <summary>
        /// True to use or not the radial distortion.
        /// </summary>
        public bool enableRadialDistortion;
        /// <summary>
        /// Target size used on the edge of the screen.
        /// </summary>
        public Vector2 targetSizeDistortion;
        /// <summary>
        /// Curve blending from screen center to the edges of the screen.
        /// </summary>
        public AnimationCurve distortionCurve;
        /// <summary>
        /// If true the distortion is relative to center of the screen otherwise relative to lensFlare source screen position.
        /// </summary>
        public bool distortionRelativeToCenter;

        /// <summary>
        /// Fall of the gradient used for the Procedural Flare.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float fallOff;
        /// <summary>
        /// Gradient Offset used for the Procedural Flare.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float edgeOffset;
        /// <summary>
        /// Side count of the regular polygon generated.
        /// </summary>
        public int sideCount;
        /// <summary>
        /// Roundness of the polygon flare (0: Sharp Polygon, 1: Circle).
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float sdfRoundness;
        /// <summary>
        /// Inverse the gradient direction.
        /// </summary>
        public bool inverseSDF;
    }

    /// <summary>
    /// LensFlareDataSRP defines a Lens Flare with a set of LensFlareDataElementSRP
    /// </summary>
    [System.Serializable]
    public sealed class LensFlareDataSRP : ScriptableObject
    {
        /// <summary>
        /// Initialize default value
        /// </summary>
        public LensFlareDataSRP()
        {
            elements = null;
        }

        /// <summary>
        /// List of LensFlareDataElementSRP
        /// </summary>
        [SerializeField]
        public LensFlareDataElementSRP[] elements;
    }

#if UNITY_EDITOR
    internal static class SRPLensFlareMenu
    {
        private static string GetSelectedAssetFolder()
        {
            if ((Selection.activeObject != null) && AssetDatabase.Contains(Selection.activeObject))
            {
                string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                string assetPathAbsolute = string.Format("{0}/{1}", Path.GetDirectoryName(Application.dataPath), assetPath);

                if (Directory.Exists(assetPathAbsolute))
                {
                    return assetPath;
                }
                else
                {
                    return Path.GetDirectoryName(assetPath);
                }
            }

            return "Assets";
        }

        private static ScriptableObject Create(string className, string assetName, string folder)
        {
            ScriptableObject asset = ScriptableObject.CreateInstance<LensFlareDataSRP>();
            if (asset == null)
            {
                Debug.LogError("failed to create instance of " + className);
                return null;
            }

            asset.name = assetName ?? typeof(LensFlareDataSRP).Name;

            string assetPath = GetUnusedAssetPath(folder, asset.name);
            AssetDatabase.CreateAsset(asset, assetPath);

            return asset;
        }

        private static string GetUnusedAssetPath(string folder, string assetName)
        {
            for (int n = 0; n < 9999; n++)
            {
                string assetPath = string.Format("{0}/{1}{2}.asset", folder, assetName, (n == 0 ? "" : n.ToString()));
                string existingGUID = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(existingGUID))
                {
                    return assetPath;
                }
            }

            return null;
        }

        [MenuItem("Assets/Create/Lens Flare (SRP)", priority = UnityEngine.Rendering.CoreUtils.Priorities.srpLensFlareMenuPriority)]
        private static void CreateSRPLensFlareAsset()
        {
            string className = typeof(LensFlareDataSRP).Name;
            string assetName = "New Lens Flare (SRP)";
            string folder = GetSelectedAssetFolder();

            string[] standardNames = new string[] { "Asset", "Attributes", "Container" };
            foreach (string standardName in standardNames)
            {
                assetName = assetName.Replace(standardName, "");
            }

            Create(className, assetName, folder);
        }
    }
#endif
}
