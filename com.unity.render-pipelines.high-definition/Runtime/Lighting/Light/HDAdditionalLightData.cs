using System;
using System.Linq;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    // This structure contains all the old values for every recordable fields from the HD light editor
    // so we can force timeline to record changes on other fields from the LateUpdate function (editor only)
    struct TimelineWorkaround
    {
        public float oldSpotAngle;
        public Color oldLightColor;
        public Vector3 oldLossyScale;
        public bool oldDisplayAreaLightEmissiveMesh;
        public float oldLightColorTemperature;
        public float oldIntensity;
        public bool lightEnabled;
    }

    //@TODO: We should continuously move these values
    // into the engine when we can see them being generally useful
    /// <summary>
    /// HDRP Additional light data component. It contains the light API and fields used by HDRP.
    /// </summary>
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Light-Component" + Documentation.endURL)]
    [AddComponentMenu("")] // Hide in menu
    [RequireComponent(typeof(Light))]
    [ExecuteAlways]
    public partial class HDAdditionalLightData : MonoBehaviour, ISerializationCallbackReceiver
    {
        internal static class ScalableSettings
        {
            public static IntScalableSetting ShadowResolutionArea(HDRenderPipelineAsset hdrp) =>
                hdrp.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowResolutionArea;
            public static IntScalableSetting ShadowResolutionPunctual(HDRenderPipelineAsset hdrp) =>
                hdrp.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowResolutionPunctual;
            public static IntScalableSetting ShadowResolutionDirectional(HDRenderPipelineAsset hdrp) =>
                hdrp.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowResolutionDirectional;

            public static BoolScalableSetting UseContactShadow(HDRenderPipelineAsset hdrp) =>
                hdrp.currentPlatformRenderPipelineSettings.lightSettings.useContactShadow;
        }

        /// <summary>
        /// The default intensity value for directional lights in Lux
        /// </summary>
        public const float k_DefaultDirectionalLightIntensity = Mathf.PI; // In lux
        /// <summary>
        /// The default intensity value for punctual lights in Lumen
        /// </summary>
        public const float k_DefaultPunctualLightIntensity = 600.0f;      // Light default to 600 lumen, i.e ~48 candela
        /// <summary>
        /// The default intensity value for area lights in Lumen
        /// </summary>
        public const float k_DefaultAreaLightIntensity = 200.0f;          // Light default to 200 lumen to better match point light

        /// <summary>
        /// Minimum value for the spot light angle
        /// </summary>
        public const float k_MinSpotAngle = 1.0f;
        /// <summary>
        /// Maximum value for the spot light angle
        /// </summary>
        public const float k_MaxSpotAngle = 179.0f;

        /// <summary>
        /// Minimum aspect ratio for pyramid spot lights
        /// </summary>
        public const float k_MinAspectRatio = 0.05f;
        /// <summary>
        /// Maximum aspect ratio for pyramid spot lights
        /// </summary>
        public const float k_MaxAspectRatio = 20.0f;

        /// <summary>
        /// Minimum shadow map view bias scale
        /// </summary>
        public const float k_MinViewBiasScale = 0.0f;
        /// <summary>
        /// Maximum shadow map view bias scale
        /// </summary>
        public const float k_MaxViewBiasScale = 15.0f;

        /// <summary>
        /// Minimum area light size
        /// </summary>
        public const float k_MinAreaWidth = 0.01f; // Provide a small size of 1cm for line light

        /// <summary>
        /// Default shadow resolution
        /// </summary>
        public const int k_DefaultShadowResolution = 512;

        // EVSM limits
        internal const float k_MinEvsmExponent = 5.0f;
        internal const float k_MaxEvsmExponent = 42.0f;
        internal const float k_MinEvsmLightLeakBias = 0.0f;
        internal const float k_MaxEvsmLightLeakBias = 1.0f;
        internal const float k_MinEvsmVarianceBias = 0.0f;
        internal const float k_MaxEvsmVarianceBias = 0.001f;
        internal const int k_MinEvsmBlurPasses = 0;
        internal const int k_MaxEvsmBlurPasses = 8;

        internal const float k_MinSpotInnerPercent = 0.0f;
        internal const float k_MaxSpotInnerPercent = 100.0f;

        internal const float k_MinAreaLightShadowCone = 10.0f;
        internal const float k_MaxAreaLightShadowCone = 179.0f;

        /// <summary>List of the lights that overlaps when the OverlapLight scene view mode is enabled</summary>
        internal static HashSet<HDAdditionalLightData> s_overlappingHDLights = new HashSet<HDAdditionalLightData>();

#region HDLight Properties API

        [SerializeField, FormerlySerializedAs("displayLightIntensity")]
        float m_Intensity;
        /// <summary>
        /// Get/Set the intensity of the light using the current light unit.
        /// </summary>
        public float intensity
        {
            get => m_Intensity;
            set
            {
                if (m_Intensity == value)
                    return;

                m_Intensity = Mathf.Clamp(value, 0, float.MaxValue);
                UpdateLightIntensity();
            }
        }

        // Only for Spotlight, should be hide for other light
        [SerializeField, FormerlySerializedAs("enableSpotReflector")]
        bool m_EnableSpotReflector = true;
        /// <summary>
        /// Get/Set the Spot Reflection option on spot lights.
        /// </summary>
        public bool enableSpotReflector
        {
            get => m_EnableSpotReflector;
            set
            {
                if (m_EnableSpotReflector == value)
                    return;

                m_EnableSpotReflector = value;
                UpdateLightIntensity();
            }
        }

        // Lux unity for all light except directional require a distance
        [SerializeField, FormerlySerializedAs("luxAtDistance")]
        float m_LuxAtDistance = 1.0f;
        /// <summary>
        /// Set/Get the distance for spot lights where the emission intensity is matches the value set in the intensity property.
        /// </summary>
        public float luxAtDistance
        {
            get => m_LuxAtDistance;
            set
            {
                if (m_LuxAtDistance == value)
                    return;

                m_LuxAtDistance = Mathf.Clamp(value, 0, float.MaxValue);
                UpdateLightIntensity();
            }
        }

        [Range(k_MinSpotInnerPercent, k_MaxSpotInnerPercent)]
        [SerializeField]
        float m_InnerSpotPercent; // To display this field in the UI this need to be public
        /// <summary>
        /// Get/Set the inner spot radius in percent.
        /// </summary>
        public float innerSpotPercent
        {
            get => m_InnerSpotPercent;
            set
            {
                if (m_InnerSpotPercent == value)
                    return;

                m_InnerSpotPercent = Mathf.Clamp(value, k_MinSpotInnerPercent, k_MaxSpotInnerPercent);
                UpdateLightIntensity();
            }
        }

        /// <summary>
        /// Get the inner spot radius between 0 and 1.
        /// </summary>
        public float innerSpotPercent01 => innerSpotPercent / 100f;


        [Range(k_MinSpotInnerPercent, k_MaxSpotInnerPercent)]
        [SerializeField]
        float m_SpotIESCutoffPercent = 100.0f; // To display this field in the UI this need to be public
        /// <summary>
        /// Get/Set the spot ies cutoff.
        /// </summary>
        public float spotIESCutoffPercent
        {
            get => m_SpotIESCutoffPercent;
            set
            {
                if (m_SpotIESCutoffPercent == value)
                    return;

                m_SpotIESCutoffPercent = Mathf.Clamp(value, k_MinSpotInnerPercent, k_MaxSpotInnerPercent);
            }
        }

        /// <summary>
        /// Get the inner spot radius between 0 and 1.
        /// </summary>
        public float spotIESCutoffPercent01 => spotIESCutoffPercent/100f;

        [Range(0.0f, 16.0f)]
        [SerializeField, FormerlySerializedAs("lightDimmer")]
        float m_LightDimmer = 1.0f;
        /// <summary>
        /// Get/Set the light dimmer / multiplier, between 0 and 16.
        /// </summary>
        public float lightDimmer
        {
            get => m_LightDimmer;
            set
            {
                if (m_LightDimmer == value)
                    return;

                m_LightDimmer = Mathf.Clamp(value, 0.0f, 16.0f);
            }
        }

        [Range(0.0f, 16.0f), SerializeField, FormerlySerializedAs("volumetricDimmer")]
        float m_VolumetricDimmer = 1.0f;
        /// <summary>
        /// Get/Set the light dimmer / multiplier on volumetric effects, between 0 and 16.
        /// </summary>
        public float volumetricDimmer
        {
            get => useVolumetric ? m_VolumetricDimmer : 0.0f;
            set
            {
                if (m_VolumetricDimmer == value)
                    return;

                m_VolumetricDimmer = Mathf.Clamp(value, 0.0f, 16.0f);
            }
        }

        // Used internally to convert any light unit input into light intensity
        [SerializeField, FormerlySerializedAs("lightUnit")]
        LightUnit m_LightUnit = LightUnit.Lumen;
        /// <summary>
        /// Get/Set the light unit. When changing the light unit, the intensity will be converted to match the previous intensity in the new unit.
        /// </summary>
        public LightUnit lightUnit
        {
            get => m_LightUnit;
            set
            {
                if (m_LightUnit == value)
                    return;

                if (!IsValidLightUnitForType(type, m_SpotLightShape, value))
                {
                    var supportedTypes = String.Join(", ", GetSupportedLightUnits(type, m_SpotLightShape));
                    Debug.LogError($"Set Light Unit '{value}' to a {GetLightTypeName()} is not allowed, only {supportedTypes} are supported.");
                    return;
                }

                LightUtils.ConvertLightIntensity(m_LightUnit, value, this, legacyLight);

                m_LightUnit = value;
                UpdateLightIntensity();
            }
        }

        // Not used for directional lights.
        [SerializeField, FormerlySerializedAs("fadeDistance")]
        float m_FadeDistance = 10000.0f;
        /// <summary>
        /// Get/Set the light fade distance.
        /// </summary>
        public float fadeDistance
        {
            get => m_FadeDistance;
            set
            {
                if (m_FadeDistance == value)
                    return;

                m_FadeDistance = Mathf.Clamp(value, 0, float.MaxValue);
            }
        }

        // Not used for directional lights.
        [SerializeField]
        float m_VolumetricFadeDistance = 10000.0f;
        /// <summary>
        /// Get/Set the light fade distance for volumetrics.
        /// </summary>
        public float volumetricFadeDistance
        {
            get => m_VolumetricFadeDistance;
            set
            {
                if (m_VolumetricFadeDistance == value)
                    return;

                m_VolumetricFadeDistance = Mathf.Clamp(value, 0, float.MaxValue);
            }
        }

        [SerializeField, FormerlySerializedAs("affectDiffuse")]
        bool m_AffectDiffuse = true;
        /// <summary>
        /// Controls whether the light affects the diffuse or not
        /// </summary>
        public bool affectDiffuse
        {
            get => m_AffectDiffuse;
            set
            {
                if (m_AffectDiffuse == value)
                    return;

                m_AffectDiffuse = value;
            }
        }

        [SerializeField, FormerlySerializedAs("affectSpecular")]
        bool m_AffectSpecular = true;
        /// <summary>
        /// Controls whether the light affects the specular or not
        /// </summary>
        public bool affectSpecular
        {
            get => m_AffectSpecular;
            set
            {
                if (m_AffectSpecular == value)
                    return;

                m_AffectSpecular = value;
            }
        }

        // This property work only with shadow mask and allow to say we don't render any lightMapped object in the shadow map
        [SerializeField, FormerlySerializedAs("nonLightmappedOnly")]
        bool m_NonLightmappedOnly = false;
        /// <summary>
        /// Only used when the shadow masks are enabled, control if the we use ShadowMask or DistanceShadowmask for this light.
        /// </summary>
        public bool nonLightmappedOnly
        {
            get => m_NonLightmappedOnly;
            set
            {
                if (m_NonLightmappedOnly == value)
                    return;

                m_NonLightmappedOnly = value;
                legacyLight.lightShadowCasterMode = value ? LightShadowCasterMode.NonLightmappedOnly : LightShadowCasterMode.Everything;
            }
        }

        // Only for Rectangle/Line/box projector lights.
        [SerializeField, FormerlySerializedAs("shapeWidth")]
        float m_ShapeWidth = 0.5f;
        /// <summary>
        /// Control the width of an area, a box spot light or a directional light cookie.
        /// </summary>
        public float shapeWidth
        {
            get => m_ShapeWidth;
            set
            {
                if (m_ShapeWidth == value)
                    return;

                if (type == HDLightType.Area)
                    m_ShapeWidth = Mathf.Clamp(value, k_MinAreaWidth, float.MaxValue);
                else
                    m_ShapeWidth = Mathf.Clamp(value, 0, float.MaxValue);
                UpdateAllLightValues();
            }
        }

        // Only for Rectangle/box projector and rectangle area lights
        [SerializeField, FormerlySerializedAs("shapeHeight")]
        float m_ShapeHeight = 0.5f;
        /// <summary>
        /// Control the height of an area, a box spot light or a directional light cookie.
        /// </summary>
        public float shapeHeight
        {
            get => m_ShapeHeight;
            set
            {
                if (m_ShapeHeight == value)
                    return;

                if (type == HDLightType.Area)
                    m_ShapeHeight = Mathf.Clamp(value, k_MinAreaWidth, float.MaxValue);
                else
                    m_ShapeHeight = Mathf.Clamp(value, 0, float.MaxValue);
                UpdateAllLightValues();
            }
        }

        // Only for pyramid projector
        [SerializeField, FormerlySerializedAs("aspectRatio")]
        float m_AspectRatio = 1.0f;
        /// <summary>
        /// Get/Set the aspect ratio of a pyramid light
        /// </summary>
        public float aspectRatio
        {
            get => m_AspectRatio;
            set
            {
                if (m_AspectRatio == value)
                    return;

                m_AspectRatio = Mathf.Clamp(value, k_MinAspectRatio, k_MaxAspectRatio);
                UpdateAllLightValues();
            }
        }

        // Only for Punctual/Sphere/Disc. Default shape radius is not 0 so that specular highlight is visible by default, it matches the previous default of 0.99 for MaxSmoothness.
        [SerializeField, FormerlySerializedAs("shapeRadius")]
        float m_ShapeRadius = 0.025f;
        /// <summary>
        /// Get/Set the radius of a light
        /// </summary>
        public float shapeRadius
        {
            get => m_ShapeRadius;
            set
            {
                if (m_ShapeRadius == value)
                    return;

                m_ShapeRadius = Mathf.Clamp(value, 0, float.MaxValue);
                UpdateAllLightValues();
            }
        }

        [SerializeField]
        float m_SoftnessScale = 1.0f;
        /// <summary>
        /// Get/Set the scale factor applied to shape radius or angular diameter for the softness calculation.
        /// </summary>
        public float softnessScale
        {
            get => m_SoftnessScale;
            set
            {
                if (m_SoftnessScale == value)
                    return;

                m_SoftnessScale = Mathf.Clamp(value, 0, float.MaxValue);
                UpdateAllLightValues();
            }
        }

        [SerializeField, FormerlySerializedAs("useCustomSpotLightShadowCone")]
        bool m_UseCustomSpotLightShadowCone = false;
        // Custom spot angle for spotlight shadows
        /// <summary>
        /// Toggle the custom spot light shadow cone.
        /// </summary>
        public bool useCustomSpotLightShadowCone
        {
            get => m_UseCustomSpotLightShadowCone;
            set
            {
                if (m_UseCustomSpotLightShadowCone == value)
                    return;

                m_UseCustomSpotLightShadowCone = value;
            }
        }

        [SerializeField, FormerlySerializedAs("customSpotLightShadowCone")]
        float m_CustomSpotLightShadowCone = 30.0f;
        /// <summary>
        /// Get/Set the custom spot shadow cone value.
        /// </summary>
        /// <value></value>
        public float customSpotLightShadowCone
        {
            get => m_CustomSpotLightShadowCone;
            set
            {
                if (m_CustomSpotLightShadowCone == value)
                    return;

                m_CustomSpotLightShadowCone = value;
            }
        }

        // Only for Spot/Point/Directional - use to cheaply fake specular spherical area light
        // It is not 1 to make sure the highlight does not disappear.
        [Range(0.0f, 1.0f)]
        [SerializeField, FormerlySerializedAs("maxSmoothness")]
        float m_MaxSmoothness = 0.99f;
        /// <summary>
        /// Get/Set the maximum smoothness of a punctual or directional light.
        /// </summary>
        public float maxSmoothness
        {
            get => m_MaxSmoothness;
            set
            {
                if (m_MaxSmoothness == value)
                    return;

                m_MaxSmoothness = Mathf.Clamp01(value);
            }
        }

        // If true, we apply the smooth attenuation factor on the range attenuation to get 0 value, else the attenuation is just inverse square and never reach 0
        [SerializeField, FormerlySerializedAs("applyRangeAttenuation")]
        bool m_ApplyRangeAttenuation = true;
        /// <summary>
        /// If enabled, apply a smooth attenuation factor so at the end of the range, the attenuation is 0.
        /// Otherwise the inverse-square attenuation is used and the value never reaches 0.
        /// </summary>
        public bool applyRangeAttenuation
        {
            get => m_ApplyRangeAttenuation;
            set
            {
                if (m_ApplyRangeAttenuation == value)
                    return;

                m_ApplyRangeAttenuation = value;
                UpdateAllLightValues();
            }
        }

        // When true, a mesh will be display to represent the area light (Can only be change in editor, component is added in Editor)
        [SerializeField, FormerlySerializedAs("displayAreaLightEmissiveMesh")]
        bool m_DisplayAreaLightEmissiveMesh = false;
        /// <summary>
        /// If enabled, display an emissive mesh rect synchronized with the intensity and color of the light.
        /// </summary>
        public bool displayAreaLightEmissiveMesh
        {
            get => m_DisplayAreaLightEmissiveMesh;
            set
            {
                if (m_DisplayAreaLightEmissiveMesh == value)
                    return;

                m_DisplayAreaLightEmissiveMesh = value;

                UpdateAllLightValues();
            }
        }

        // Optional cookie for rectangular area lights
        [SerializeField, FormerlySerializedAs("areaLightCookie")]
        Texture m_AreaLightCookie = null;
        /// <summary>
        /// Get/Set cookie texture for area lights.
        /// </summary>
        public Texture areaLightCookie
        {
            get => m_AreaLightCookie;
            set
            {
                if (m_AreaLightCookie == value)
                    return;

                m_AreaLightCookie = value;
                UpdateAllLightValues();
            }
        }


        // Optional IES (Cubemap for PointLight)
        [SerializeField]
        internal Texture m_IESPoint;
        // Optional IES (2D Square texture for Spot or rectangular light)
        [SerializeField]
        internal Texture m_IESSpot;

        /// <summary>
        /// Get/Set IES texture for Point
        /// </summary>
        internal Texture IESPoint
        {
            get => m_IESPoint;
            set
            {
                if (value.dimension == TextureDimension.Cube)
                {
                    m_IESPoint = value;
                    UpdateAllLightValues();
                }
                else
                {
                    Debug.LogError("Texture dimension " + value.dimension + " is not supported for point lights.");
                    m_IESPoint = null;
                }
            }
        }

        /// <summary>
        /// Get/Set IES texture for Spot or rectangular light.
        /// </summary>
        internal Texture IESSpot
        {
            get => m_IESSpot;
            set
            {
                if (value.dimension == TextureDimension.Tex2D && value.width == value.height)
                {
                    m_IESSpot = value;
                    UpdateAllLightValues();
                }
                else
                {
                    Debug.LogError("Texture dimension " + value.dimension + " is not supported for spot lights or rectangular light (only square images).");
                    m_IESSpot = null;
                }
            }
        }

        [SerializeField]
        bool m_IncludeForRayTracing = true;
        /// <summary>
        /// Controls if the light is enabled when the camera has the RayTracing frame setting enabled.
        /// </summary>
        public bool includeForRayTracing
        {
            get => m_IncludeForRayTracing;
            set
            {
                if (m_IncludeForRayTracing == value)
                    return;

                UpdateAllLightValues();
            }
        }

        [Range(k_MinAreaLightShadowCone, k_MaxAreaLightShadowCone)]
        [SerializeField, FormerlySerializedAs("areaLightShadowCone")]
        float m_AreaLightShadowCone = 120.0f;
        /// <summary>
        /// Get/Set area light shadow cone value.
        /// </summary>
        public float areaLightShadowCone
        {
            get => m_AreaLightShadowCone;
            set
            {
                if (m_AreaLightShadowCone == value)
                    return;

                m_AreaLightShadowCone = Mathf.Clamp(value, k_MinAreaLightShadowCone, k_MaxAreaLightShadowCone);
                UpdateAllLightValues();
            }
        }

        // Flag that tells us if the shadow should be screen space
        [SerializeField, FormerlySerializedAs("useScreenSpaceShadows")]
        bool m_UseScreenSpaceShadows = false;
        /// <summary>
        /// Controls if we resolve the directional light shadows in screen space (ray tracing only).
        /// </summary>
        public bool useScreenSpaceShadows
        {
            get => m_UseScreenSpaceShadows;
            set
            {
                if (m_UseScreenSpaceShadows == value)
                    return;

                m_UseScreenSpaceShadows = value;
            }
        }

        // Directional lights only.
        [SerializeField, FormerlySerializedAs("interactsWithSky")]
        bool m_InteractsWithSky = true;
        /// <summary>
        /// Controls if the directional light affect the Physically Based sky.
        /// This have no effect on other skies.
        /// </summary>
        public bool interactsWithSky
        {
            get => m_InteractsWithSky;
            set
            {
                if (m_InteractsWithSky == value)
                    return;

                m_InteractsWithSky = value;
            }
        }
        [SerializeField, FormerlySerializedAs("angularDiameter")]
        float m_AngularDiameter = 0.5f;
        /// <summary>
        /// Angular diameter of the emissive celestial body represented by the light as seen from the camera (in degrees).
        /// Used to render the sun/moon disk.
        /// </summary>
        public float angularDiameter
        {
            get => m_AngularDiameter;
            set
            {
                if (m_AngularDiameter == value)
                    return;

                m_AngularDiameter = value; // Serialization code clamps
            }
        }

        [SerializeField, FormerlySerializedAs("flareSize")]
        float m_FlareSize = 2.0f;
        /// <summary>
        /// Size the flare around the celestial body (in degrees).
        /// </summary>
        public float flareSize
        {
            get => m_FlareSize;
            set
            {
                if (m_FlareSize == value)
                    return;

                m_FlareSize = value; // Serialization code clamps
            }
        }

        [SerializeField, FormerlySerializedAs("flareTint")]
        Color m_FlareTint = Color.white;
        /// <summary>
        /// Tints the flare of the celestial body.
        /// </summary>
        public Color flareTint
        {
            get => m_FlareTint;
            set
            {
                if (m_FlareTint == value)
                    return;

                m_FlareTint = value;
            }
        }

        [SerializeField, FormerlySerializedAs("flareFalloff")]
        float m_FlareFalloff = 4.0f;
        /// <summary>
        /// The falloff rate of flare intensity as the angle from the light increases.
        /// </summary>
        public float flareFalloff
        {
            get => m_FlareFalloff;
            set
            {
                if (m_FlareFalloff == value)
                    return;

                m_FlareFalloff = value; // Serialization code clamps
            }
        }

        [SerializeField, FormerlySerializedAs("surfaceTexture")]
        Texture2D m_SurfaceTexture = null;
        /// <summary>
        /// 2D (disk) texture of the surface of the celestial body. Acts like a multiplier.
        /// </summary>
        public Texture2D surfaceTexture
        {
            get => m_SurfaceTexture;
            set
            {
                if (m_SurfaceTexture == value)
                    return;

                m_SurfaceTexture = value;
            }
        }

        [SerializeField, FormerlySerializedAs("surfaceTint")]
        Color m_SurfaceTint = Color.white;
        /// <summary>
        /// Tints the surface of the celestial body.
        /// </summary>
        public Color surfaceTint
        {
            get => m_SurfaceTint;
            set
            {
                if (m_SurfaceTint == value)
                    return;

                m_SurfaceTint = value;
            }
        }

        [SerializeField, FormerlySerializedAs("distance")]
        float m_Distance = 150000000000; // Sun to Earth
        /// <summary>
        /// Distance from the camera to the emissive celestial body represented by the light.
        /// </summary>
        public float distance
        {
            get => m_Distance;
            set
            {
                if (m_Distance == value)
                    return;

                m_Distance = value; // Serialization code clamps
            }
        }

        [SerializeField, FormerlySerializedAs("useRayTracedShadows")]
        bool m_UseRayTracedShadows = false;
        /// <summary>
        /// Controls if we use ray traced shadows.
        /// </summary>
        public bool useRayTracedShadows
        {
            get => m_UseRayTracedShadows;
            set
            {
                if (m_UseRayTracedShadows == value)
                    return;

                m_UseRayTracedShadows = value;
            }
        }

        [Range(1, 32)]
        [SerializeField, FormerlySerializedAs("numRayTracingSamples")]
        int m_NumRayTracingSamples = 4;
        /// <summary>
        /// Controls the number of sample used for the ray traced shadows.
        /// </summary>
        public int numRayTracingSamples
        {
            get => m_NumRayTracingSamples;
            set
            {
                if (m_NumRayTracingSamples == value)
                    return;

                m_NumRayTracingSamples = Mathf.Clamp(value, 1, 32);
            }
        }

        [SerializeField, FormerlySerializedAs("filterTracedShadow")]
        bool m_FilterTracedShadow = true;
        /// <summary>
        /// Toggle the filtering of ray traced shadows.
        /// </summary>
        public bool filterTracedShadow
        {
            get => m_FilterTracedShadow;
            set
            {
                if (m_FilterTracedShadow == value)
                    return;

                m_FilterTracedShadow = value;
            }
        }

        [Range(1, 32)]
        [SerializeField, FormerlySerializedAs("filterSizeTraced")]
        int m_FilterSizeTraced = 16;
        /// <summary>
        /// Control the size of the filter used for ray traced shadows
        /// </summary>
        public int filterSizeTraced
        {
            get => m_FilterSizeTraced;
            set
            {
                if (m_FilterSizeTraced == value)
                    return;

                m_FilterSizeTraced = Mathf.Clamp(value, 1, 32);
            }
        }

        [Range(0.0f, 2.0f)]
        [SerializeField, FormerlySerializedAs("sunLightConeAngle")]
        float m_SunLightConeAngle = 0.5f;
        /// <summary>
        /// Angular size of the sun in degree.
        /// </summary>
        public float sunLightConeAngle
        {
            get => m_SunLightConeAngle;
            set
            {
                if (m_SunLightConeAngle == value)
                    return;

                m_SunLightConeAngle = Mathf.Clamp(value, 0.0f, 2.0f);
            }
        }

        [SerializeField, FormerlySerializedAs("lightShadowRadius")]
        float m_LightShadowRadius = 0.5f;
        /// <summary>
        /// Angular size of the sun in degree.
        /// </summary>
        public float lightShadowRadius
        {
            get => m_LightShadowRadius;
            set
            {
                if (m_LightShadowRadius == value)
                    return;

                m_LightShadowRadius = Mathf.Max(value, 0.001f);
            }
        }

        [SerializeField]
        bool m_SemiTransparentShadow = false;
        /// <summary>
        /// Enable semi-transparent shadows on the light.
        /// </summary>
        public bool semiTransparentShadow
        {
            get => m_SemiTransparentShadow;
            set
            {
                if (m_SemiTransparentShadow == value)
                    return;

                m_SemiTransparentShadow = value;
            }
        }

        [SerializeField]
        bool m_ColorShadow = true;
        /// <summary>
        /// Enable color shadows on the light.
        /// </summary>
        public bool colorShadow
        {
            get => m_ColorShadow;
            set
            {
                if (m_ColorShadow == value)
                    return;

                m_ColorShadow = value;
            }
        }

        [SerializeField]
        bool m_DistanceBasedFiltering = false;
        /// <summary>
        /// Uses the distance to the occluder to improve the shadow denoising.
        /// </summary>
        internal bool distanceBasedFiltering
        {
            get => m_DistanceBasedFiltering;
            set
            {
                if (m_DistanceBasedFiltering == value)
                    return;

                m_DistanceBasedFiltering = value;
            }
        }

        [Range(k_MinEvsmExponent, k_MaxEvsmExponent)]
        [SerializeField, FormerlySerializedAs("evsmExponent")]
        float m_EvsmExponent = 15.0f;
        /// <summary>
        /// Controls the exponent used for EVSM shadows.
        /// </summary>
        public float evsmExponent
        {
            get => m_EvsmExponent;
            set
            {
                if (m_EvsmExponent == value)
                    return;

                m_EvsmExponent = Mathf.Clamp(value, k_MinEvsmExponent, k_MaxEvsmExponent);
            }
        }

        [Range(k_MinEvsmLightLeakBias, k_MaxEvsmLightLeakBias)]
        [SerializeField, FormerlySerializedAs("evsmLightLeakBias")]
        float m_EvsmLightLeakBias = 0.0f;
        /// <summary>
        /// Controls the light leak bias value for EVSM shadows.
        /// </summary>
        public float evsmLightLeakBias
        {
            get => m_EvsmLightLeakBias;
            set
            {
                if (m_EvsmLightLeakBias == value)
                    return;

                m_EvsmLightLeakBias = Mathf.Clamp(value, k_MinEvsmLightLeakBias, k_MaxEvsmLightLeakBias);
            }
        }

        [Range(k_MinEvsmVarianceBias, k_MaxEvsmVarianceBias)]
        [SerializeField, FormerlySerializedAs("evsmVarianceBias")]
        float m_EvsmVarianceBias = 1e-5f;
        /// <summary>
        /// Controls the variance bias used for EVSM shadows.
        /// </summary>
        public float evsmVarianceBias
        {
            get => m_EvsmVarianceBias;
            set
            {
                if (m_EvsmVarianceBias == value)
                    return;

                m_EvsmVarianceBias = Mathf.Clamp(value, k_MinEvsmVarianceBias, k_MaxEvsmVarianceBias);
            }
        }

        [Range(k_MinEvsmBlurPasses, k_MaxEvsmBlurPasses)]
        [SerializeField, FormerlySerializedAs("evsmBlurPasses")]
        int m_EvsmBlurPasses = 0;
        /// <summary>
        /// Controls the number of blur passes used for EVSM shadows.
        /// </summary>
        public int evsmBlurPasses
        {
            get => m_EvsmBlurPasses;
            set
            {
                if (m_EvsmBlurPasses == value)
                    return;

                m_EvsmBlurPasses = Mathf.Clamp(value, k_MinEvsmBlurPasses, k_MaxEvsmBlurPasses);
            }
        }

        // Now the renderingLayerMask is used for shadow layers and not light layers
        [SerializeField, FormerlySerializedAs("lightlayersMask")]
        LightLayerEnum m_LightlayersMask = LightLayerEnum.LightLayerDefault;
        /// <summary>
        /// Controls which layer will be affected by this light
        /// </summary>
        /// <value></value>
        public LightLayerEnum lightlayersMask
        {
            get => linkShadowLayers ? (LightLayerEnum)RenderingLayerMaskToLightLayer(legacyLight.renderingLayerMask) : m_LightlayersMask;
            set
            {
                m_LightlayersMask = value;

                if (linkShadowLayers)
                    legacyLight.renderingLayerMask = LightLayerToRenderingLayerMask((int)m_LightlayersMask, legacyLight.renderingLayerMask);
            }
        }

        [SerializeField, FormerlySerializedAs("linkShadowLayers")]
        bool m_LinkShadowLayers = true;
        /// <summary>
        /// Controls if we want to synchronize shadow map light layers and light layers or not.
        /// </summary>
        public bool linkShadowLayers
        {
            get => m_LinkShadowLayers;
            set => m_LinkShadowLayers = value;
        }

        /// <summary>
        /// Returns a mask of light layers as uint and handle the case of Everything as being 0xFF and not -1
        /// </summary>
        /// <returns></returns>
        public uint GetLightLayers()
        {
            int value = (int)lightlayersMask;
            return value < 0 ? (uint)LightLayerEnum.Everything : (uint)value;
        }

        /// <summary>
        /// Returns a mask of shadow light layers as uint and handle the case of Everything as being 0xFF and not -1
        /// </summary>
        /// <returns></returns>
        public uint GetShadowLayers()
        {
            int value = RenderingLayerMaskToLightLayer(legacyLight.renderingLayerMask);
            return value < 0 ? (uint)LightLayerEnum.Everything : (uint)value;
        }

        // Shadow Settings
        [SerializeField, FormerlySerializedAs("shadowNearPlane")]
        float    m_ShadowNearPlane = 0.1f;
        /// <summary>
        /// Controls the near plane distance of the shadows.
        /// </summary>
        public float shadowNearPlane
        {
            get => m_ShadowNearPlane;
            set
            {
                if (m_ShadowNearPlane == value)
                    return;

                m_ShadowNearPlane = Mathf.Clamp(value, HDShadowUtils.k_MinShadowNearPlane, HDShadowUtils.k_MaxShadowNearPlane);
            }
        }

        // PCSS settings
        [Range(1, 64)]
        [SerializeField, FormerlySerializedAs("blockerSampleCount")]
        int      m_BlockerSampleCount = 24;
        /// <summary>
        /// Controls the number of samples used to detect blockers for PCSS shadows.
        /// </summary>
        public int blockerSampleCount
        {
            get => m_BlockerSampleCount;
            set
            {
                if (m_BlockerSampleCount == value)
                    return;

                m_BlockerSampleCount = Mathf.Clamp(value, 1, 64);
            }
        }

        [Range(1, 64)]
        [SerializeField, FormerlySerializedAs("filterSampleCount")]
        int      m_FilterSampleCount = 16;
        /// <summary>
        /// Controls the number of samples used to filter for PCSS shadows.
        /// </summary>
        public int filterSampleCount
        {
            get => m_FilterSampleCount;
            set
            {
                if (m_FilterSampleCount == value)
                    return;

                m_FilterSampleCount = Mathf.Clamp(value, 1, 64);
            }
        }

        [Range(0, 1.0f)]
        [SerializeField, FormerlySerializedAs("minFilterSize")]
        float m_MinFilterSize = 0.1f;
        /// <summary>
        /// Controls the minimum filter size of PCSS shadows.
        /// </summary>
        public float minFilterSize
        {
            get => m_MinFilterSize;
            set
            {
                if (m_MinFilterSize == value)
                    return;

                m_MinFilterSize = Mathf.Clamp(value, 0.0f, 1.0f);
            }
        }

        // Improved Moment Shadows settings
        [Range(1, 32)]
        [SerializeField, FormerlySerializedAs("kernelSize")]
        int m_KernelSize = 5;
        /// <summary>
        /// Controls the kernel size for IMSM shadows.
        /// </summary>
        public int kernelSize
        {
            get => m_KernelSize;
            set
            {
                if (m_KernelSize == value)
                    return;

                m_KernelSize = Mathf.Clamp(value, 1, 32);
            }
        }

        [Range(0.0f, 9.0f)]
        [SerializeField, FormerlySerializedAs("lightAngle")]
        float m_LightAngle = 1.0f;
        /// <summary>
        /// Controls the light angle for IMSM shadows.
        /// </summary>
        public float lightAngle
        {
            get => m_LightAngle;
            set
            {
                if (m_LightAngle == value)
                    return;

                m_LightAngle = Mathf.Clamp(value, 0.0f, 9.0f);
            }
        }

        [Range(0.0001f, 0.01f)]
        [SerializeField, FormerlySerializedAs("maxDepthBias")]
        float m_MaxDepthBias = 0.001f;
        /// <summary>
        /// Controls the max depth bias for IMSM shadows.
        /// </summary>
        public float maxDepthBias
        {
            get => m_MaxDepthBias;
            set
            {
                if (m_MaxDepthBias == value)
                    return;

                m_MaxDepthBias = Mathf.Clamp(value, 0.0001f, 0.01f);
            }
        }

        /// <summary>
        /// The range of the light.
        /// </summary>
        /// <value></value>
        public float range
        {
            get => legacyLight.range;
            set => legacyLight.range = value;
        }

        /// <summary>
        /// Color of the light.
        /// </summary>
        public Color color
        {
            get => legacyLight.color;
            set
            {
                legacyLight.color = value;

                // Update Area Light Emissive mesh color
                UpdateAreaLightEmissiveMesh();
            }
        }

        #endregion

        #region HDShadow Properties API (from AdditionalShadowData)
        [SerializeField] private IntScalableSettingValue m_ShadowResolution = new IntScalableSettingValue
        {
            @override = k_DefaultShadowResolution,
            useOverride = true,
        };

        /// <summary>
        /// Retrieve the scalable setting for shadow resolution. Use the SetShadowResolution function to set a custom resolution.
        /// </summary>
        public IntScalableSettingValue shadowResolution => m_ShadowResolution;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        float m_ShadowDimmer = 1.0f;
        /// <summary>
        /// Get/Set the shadow dimmer.
        /// </summary>
        public float shadowDimmer
        {
            get => m_ShadowDimmer;
            set
            {
                if (m_ShadowDimmer == value)
                    return;

                m_ShadowDimmer = Mathf.Clamp01(value);
            }
        }

        [Range(0.0f, 1.0f)]
        [SerializeField]
        float m_VolumetricShadowDimmer = 1.0f;
        /// <summary>
        /// Get/Set the volumetric shadow dimmer value, between 0 and 1.
        /// </summary>
        public float volumetricShadowDimmer
        {
            get => useVolumetric ? m_VolumetricShadowDimmer : 0.0f;
            set
            {
                if (m_VolumetricShadowDimmer == value)
                    return;

                m_VolumetricShadowDimmer = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        float m_ShadowFadeDistance = 10000.0f;
        /// <summary>
        /// Get/Set the shadow fade distance.
        /// </summary>
        public float shadowFadeDistance
        {
            get => m_ShadowFadeDistance;
            set
            {
                if (m_ShadowFadeDistance == value)
                    return;

                m_ShadowFadeDistance = Mathf.Clamp(value, 0, float.MaxValue);
            }
        }

        [SerializeField]
        BoolScalableSettingValue m_UseContactShadow = new BoolScalableSettingValue { useOverride = true };

        /// <summary>
        /// Retrieve the scalable setting to use/ignore contact shadows. Toggle the use contact shadow using @override property of the ScalableSetting.
        /// </summary>
        public BoolScalableSettingValue useContactShadow => m_UseContactShadow;

        [SerializeField]
        bool m_RayTracedContactShadow = false;
        /// <summary>
        /// Controls if we want to ray trace the contact shadow.
        /// </summary>
        public bool rayTraceContactShadow
        {
            get => m_RayTracedContactShadow;
            set
            {
                if (m_RayTracedContactShadow == value)
                    return;

                m_RayTracedContactShadow = value;
            }
        }

        [SerializeField]
        Color m_ShadowTint = Color.black;
        /// <summary>
        /// Controls the tint of the shadows.
        /// </summary>
        /// <value></value>
        public Color shadowTint
        {
            get => m_ShadowTint;
            set
            {
                if (m_ShadowTint == value)
                    return;

                m_ShadowTint = value;
            }
        }

        [SerializeField]
        bool m_PenumbraTint = false;
        /// <summary>
        /// Controls if we want to ray trace the contact shadow.
        /// </summary>
        public bool penumbraTint
        {
            get => m_PenumbraTint;
            set
            {
                if (m_PenumbraTint == value)
                    return;

                m_PenumbraTint = value;
            }
        }

        [SerializeField]
        float m_NormalBias = 0.75f;
        /// <summary>
        /// Get/Set the normal bias of the shadow maps.
        /// </summary>
        /// <value></value>
        public float normalBias
        {
            get => m_NormalBias;
            set
            {
                if (m_NormalBias == value)
                    return;

                m_NormalBias = value;
            }
        }

        [SerializeField]
        float m_SlopeBias = 0.5f;
        /// <summary>
        /// Get/Set the slope bias of the shadow maps.
        /// </summary>
        /// <value></value>
        public float slopeBias
        {
            get => m_SlopeBias;
            set
            {
                if (m_SlopeBias == value)
                    return;

                m_SlopeBias = value;
            }
        }

        [SerializeField]
        ShadowUpdateMode m_ShadowUpdateMode = ShadowUpdateMode.EveryFrame;
        /// <summary>
        /// Get/Set the shadow update mode.
        /// </summary>
        /// <value></value>
        public ShadowUpdateMode shadowUpdateMode
        {
            get => m_ShadowUpdateMode;
            set
            {
                if (m_ShadowUpdateMode == value)
                    return;

                if (m_ShadowUpdateMode != ShadowUpdateMode.EveryFrame && value == ShadowUpdateMode.EveryFrame)
                {
                    if(!preserveCachedShadow)
                    {
                        HDShadowManager.cachedShadowManager.EvictLight(this);
                    }
                }
                else if(legacyLight.shadows != LightShadows.None && m_ShadowUpdateMode == ShadowUpdateMode.EveryFrame && value != ShadowUpdateMode.EveryFrame)
                {
                    HDShadowManager.cachedShadowManager.RegisterLight(this);
                }

                m_ShadowUpdateMode = value;
            }
        }

        [SerializeField]
        bool m_AlwaysDrawDynamicShadows = false;

        /// <summary>
        /// Whether cached shadows will always draw dynamic shadow casters.
        /// </summary>
        /// <value></value>
        public bool alwaysDrawDynamicShadows
        {
            get => m_AlwaysDrawDynamicShadows;
            set { m_AlwaysDrawDynamicShadows = value; }
        }

        [SerializeField]
        bool m_UpdateShadowOnLightMovement = false;
        /// <summary>
        /// Whether a cached shadow map will be automatically updated when the light transform changes (more than a given threshold set via cachedShadowTranslationUpdateThreshold
        /// and cachedShadowAngleUpdateThreshold).
        /// </summary>
        /// <value></value>
        public bool updateUponLightMovement
        {
            get => m_UpdateShadowOnLightMovement;
            set
            {
                if(m_UpdateShadowOnLightMovement != value)
                {
                    if (m_UpdateShadowOnLightMovement)
                        HDShadowManager.cachedShadowManager.RegisterTransformToCache(this);
                    else
                        HDShadowManager.cachedShadowManager.RegisterTransformToCache(this);

                    m_UpdateShadowOnLightMovement = value;
                }
            }
        }

        [SerializeField]
        float m_CachedShadowTranslationThreshold = 0.01f;
        /// <summary>
        /// Controls the position threshold over which a cached shadow which is set to update upon light movement
        /// (updateUponLightMovement from script or Update on Light Movement in UI) triggers an update.
        /// </summary>
        public float cachedShadowTranslationUpdateThreshold
        {
            get => m_CachedShadowTranslationThreshold;
            set
            {
                if (m_CachedShadowTranslationThreshold == value)
                    return;

                m_CachedShadowTranslationThreshold = value;
            }
        }

        [SerializeField]
        float m_CachedShadowAngularThreshold = 0.5f;
        /// <summary>
        /// If any transform angle of the light is over this threshold (in degrees) since last update, a cached shadow which is set to update upon light movement
        /// (updateUponLightMovement from script or Update on Light Movement in UI) is updated.
        /// </summary>
        public float cachedShadowAngleUpdateThreshold
        {
            get => m_CachedShadowAngularThreshold;
            set
            {
                if (m_CachedShadowAngularThreshold == value)
                    return;

                m_CachedShadowAngularThreshold = value;
            }
        }

        // Only for Rectangle area lights.
        [Range(0.0f, 90.0f)]
        [SerializeField]
        float m_BarnDoorAngle = 90.0f;
        /// <summary>
        /// Get/Set the angle so that it behaves like a barn door.
        /// </summary>
        public float barnDoorAngle
        {
            get => m_BarnDoorAngle;
            set
            {
                if (m_BarnDoorAngle == value)
                    return;

                m_BarnDoorAngle = Mathf.Clamp(value, 0.0f, 90.0f);
                UpdateAllLightValues();
            }
        }

        // Only for Rectangle area lights
        [SerializeField]
        float m_BarnDoorLength = 0.05f;
        /// <summary>
        /// Get/Set the length for the barn door sides.
        /// </summary>
        public float barnDoorLength
        {
            get => m_BarnDoorLength;
            set
            {
                if (m_BarnDoorLength == value)
                    return;

                m_BarnDoorLength = Mathf.Clamp(value, 0.0f, float.MaxValue);
                UpdateAllLightValues();
            }
        }

        [SerializeField]
        bool m_preserveCachedShadow = false;
        /// <summary>
        /// Controls whether the cached shadow maps for this light is preserved upon disabling the light.
        /// If this field is set to true, then the light will maintain its space in the cached shadow atlas until it is destroyed.
        /// </summary>
        public bool preserveCachedShadow
        {
            get => m_preserveCachedShadow;
            set
            {
                if (m_preserveCachedShadow == value)
                    return;

                m_preserveCachedShadow = value;
            }
        }

        /// <summary>
        /// True if the light affects volumetric fog, false otherwise
        /// </summary>
        public bool affectsVolumetric
        {
            get => useVolumetric;
            set => useVolumetric = value;
        }

#endregion

#region Internal API for moving shadow datas from AdditionalShadowData to HDAdditionalLightData

        [SerializeField]
        float[] m_ShadowCascadeRatios = new float[3] { 0.05f, 0.2f, 0.3f };
        internal float[] shadowCascadeRatios
        {
            get => m_ShadowCascadeRatios;
            set => m_ShadowCascadeRatios = value;
        }

        [SerializeField]
        float[] m_ShadowCascadeBorders = new float[4] { 0.2f, 0.2f, 0.2f, 0.2f };
        internal float[] shadowCascadeBorders
        {
            get => m_ShadowCascadeBorders;
            set => m_ShadowCascadeBorders = value;
        }

        [SerializeField]
        int m_ShadowAlgorithm = 0;
        internal int shadowAlgorithm
        {
            get => m_ShadowAlgorithm;
            set => m_ShadowAlgorithm = value;
        }

        [SerializeField]
        int m_ShadowVariant = 0;
        internal int shadowVariant
        {
            get => m_ShadowVariant;
            set => m_ShadowVariant = value;
        }

        [SerializeField]
        int m_ShadowPrecision = 0;
        internal int shadowPrecision
        {
            get => m_ShadowPrecision;
            set => m_ShadowPrecision = value;
        }

#endregion

#pragma warning disable 0414 // The field '...' is assigned but its value is never used, these fields are used by the inspector
        // This is specific for the LightEditor GUI and not use at runtime
        [SerializeField, FormerlySerializedAs("useOldInspector")]
        bool useOldInspector = false;
        [SerializeField, FormerlySerializedAs("useVolumetric")]
        bool useVolumetric = true;
        [SerializeField, FormerlySerializedAs("featuresFoldout")]
        bool featuresFoldout = true;
        [SerializeField, FormerlySerializedAs("showAdditionalSettings")]
        byte showAdditionalSettings = 0;
#pragma warning restore 0414

        HDShadowRequest[]   shadowRequests;
        bool                m_WillRenderShadowMap;
        bool                m_WillRenderScreenSpaceShadow;
        bool                m_WillRenderRayTracedShadow;
        int[]               m_ShadowRequestIndices;


        // Data for cached shadow maps
        [System.NonSerialized]
        internal int lightIdxForCachedShadows = -1;
        Vector3 m_CachedViewPos = new Vector3(0, 0, 0);


        [System.NonSerialized]
        Plane[]             m_ShadowFrustumPlanes = new Plane[6];

        // temporary matrix that stores the previous light data (mainly used to discard history for ray traced screen space shadows)
        [System.NonSerialized] internal Matrix4x4 previousTransform = Matrix4x4.identity;
        // Temporary index that stores the current shadow index for the light
        [System.NonSerialized] internal int shadowIndex = -1;

        // Runtime datas used to compute light intensity
        Light m_Light;
        internal Light legacyLight
        {
            get
            {
                // Calling TryGetComponent only when needed is faster than letting the null check happen inside TryGetComponent
                if (m_Light == null)
                    TryGetComponent<Light>(out m_Light);

                return m_Light;
            }
        }

        const string k_EmissiveMeshViewerName = "EmissiveMeshViewer";

        GameObject m_ChildEmissiveMeshViewer;
        MeshFilter m_EmissiveMeshFilter;
        internal MeshRenderer emissiveMeshRenderer { get; private set; }

#if UNITY_EDITOR
        bool m_NeedsPrefabInstanceCheck = false;
        bool needRefreshPrefabInstanceEmissiveMeshes = false;
#endif
        bool needRefreshEmissiveMeshesFromTimeLineUpdate = false;

        void CreateChildEmissiveMeshViewerIfNeeded()
        {
#if UNITY_EDITOR
            if (PrefabUtility.IsPartOfPrefabAsset(this))
                    return;
#endif
            bool here = m_ChildEmissiveMeshViewer != null && !m_ChildEmissiveMeshViewer.Equals(null);

#if UNITY_EDITOR
            //if not parented anymore, destroy it
            if (here && m_ChildEmissiveMeshViewer.transform.parent != transform)
            {
                if (Application.isPlaying)
                    Destroy(m_ChildEmissiveMeshViewer);
                else
                    DestroyImmediate(m_ChildEmissiveMeshViewer);
                m_ChildEmissiveMeshViewer = null;
                m_EmissiveMeshFilter = null;
                here = false;
            }
#endif

            //if not here, try to find it first
            if (!here)
            {
                foreach (Transform child in transform)
                {
                    var test = child.GetComponents(typeof(Component));
                    if (child.name == k_EmissiveMeshViewerName
                        && child.hideFlags == (HideFlags.NotEditable | HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor)
                        && child.GetComponents(typeof(MeshFilter)).Length == 1
                        && child.GetComponents(typeof(MeshRenderer)).Length == 1
                        && child.GetComponents(typeof(Component)).Length == 3) // Transform + MeshFilter + MeshRenderer
                    {
                        m_ChildEmissiveMeshViewer = child.gameObject;
                        m_ChildEmissiveMeshViewer.transform.localPosition = Vector3.zero;
                        m_ChildEmissiveMeshViewer.transform.localRotation = Quaternion.identity;
                        m_ChildEmissiveMeshViewer.transform.localScale = Vector3.one;
                        m_ChildEmissiveMeshViewer.layer = areaLightEmissiveMeshLayer == -1 ? gameObject.layer : areaLightEmissiveMeshLayer;

                        m_EmissiveMeshFilter = m_ChildEmissiveMeshViewer.GetComponent<MeshFilter>();
                        emissiveMeshRenderer = m_ChildEmissiveMeshViewer.GetComponent<MeshRenderer>();
                        emissiveMeshRenderer.shadowCastingMode = m_AreaLightEmissiveMeshShadowCastingMode;
                        emissiveMeshRenderer.motionVectorGenerationMode = m_AreaLightEmissiveMeshMotionVectorGenerationMode;

                        here = true;
                        break;
                    }
                }
            }

            //if still not here, create it
            if (!here)
            {
                m_ChildEmissiveMeshViewer = new GameObject(k_EmissiveMeshViewerName, typeof(MeshFilter), typeof(MeshRenderer));
                m_ChildEmissiveMeshViewer.hideFlags = HideFlags.NotEditable | HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                m_ChildEmissiveMeshViewer.transform.SetParent(transform);
                m_ChildEmissiveMeshViewer.transform.localPosition = Vector3.zero;
                m_ChildEmissiveMeshViewer.transform.localRotation = Quaternion.identity;
                m_ChildEmissiveMeshViewer.transform.localScale = Vector3.one;
                m_ChildEmissiveMeshViewer.layer = areaLightEmissiveMeshLayer == -1 ? gameObject.layer : areaLightEmissiveMeshLayer;

                m_EmissiveMeshFilter = m_ChildEmissiveMeshViewer.GetComponent<MeshFilter>();
                emissiveMeshRenderer = m_ChildEmissiveMeshViewer.GetComponent<MeshRenderer>();
                emissiveMeshRenderer.shadowCastingMode = m_AreaLightEmissiveMeshShadowCastingMode;
                emissiveMeshRenderer.motionVectorGenerationMode = m_AreaLightEmissiveMeshMotionVectorGenerationMode;
            }
        }

        void DestroyChildEmissiveMeshViewer()
        {
            m_EmissiveMeshFilter = null;

            emissiveMeshRenderer.enabled = false;
            emissiveMeshRenderer = null;

            CoreUtils.Destroy(m_ChildEmissiveMeshViewer);
            m_ChildEmissiveMeshViewer = null;
        }

        [SerializeField]
        ShadowCastingMode m_AreaLightEmissiveMeshShadowCastingMode = ShadowCastingMode.Off;
        [SerializeField]
        MotionVectorGenerationMode m_AreaLightEmissiveMeshMotionVectorGenerationMode;
        [SerializeField]
        int m_AreaLightEmissiveMeshLayer = -1; //Special value that means we need to grab the one in the Light for initialization (for migration purpose)

        /// <summary> Change the Shadow Casting Mode of the generated emissive mesh for Area Light </summary>
        public ShadowCastingMode areaLightEmissiveMeshShadowCastingMode
        {
            get => m_AreaLightEmissiveMeshShadowCastingMode;
            set
            {
                if (m_AreaLightEmissiveMeshShadowCastingMode == value)
                    return;

                m_AreaLightEmissiveMeshShadowCastingMode = value;
                if (emissiveMeshRenderer != null && !emissiveMeshRenderer.Equals(null))
                {
                    emissiveMeshRenderer.shadowCastingMode = m_AreaLightEmissiveMeshShadowCastingMode;
                }
            }
        }

        /// <summary> Change the Motion Vector Generation Mode of the generated emissive mesh for Area Light </summary>
        public MotionVectorGenerationMode areaLightEmissiveMeshMotionVectorGenerationMode
        {
            get => m_AreaLightEmissiveMeshMotionVectorGenerationMode;
            set
            {
                if (m_AreaLightEmissiveMeshMotionVectorGenerationMode == value)
                    return;

                m_AreaLightEmissiveMeshMotionVectorGenerationMode = value;
                if (emissiveMeshRenderer != null && !emissiveMeshRenderer.Equals(null))
                {
                    emissiveMeshRenderer.motionVectorGenerationMode = m_AreaLightEmissiveMeshMotionVectorGenerationMode;
                }
            }
        }

        /// <summary> Change the Layer of the generated emissive mesh for Area Light </summary>
        public int areaLightEmissiveMeshLayer
        {
            get => m_AreaLightEmissiveMeshLayer;
            set
            {
                if (m_AreaLightEmissiveMeshLayer == value)
                    return;

                m_AreaLightEmissiveMeshLayer = value;
                if (emissiveMeshRenderer != null && !emissiveMeshRenderer.Equals(null) && m_AreaLightEmissiveMeshLayer != -1)
                {
                    emissiveMeshRenderer.gameObject.layer = m_AreaLightEmissiveMeshLayer;
                }
            }
        }


        void OnDestroy()
        {
            if(lightIdxForCachedShadows >= 0) // If it is within the cached system we need to evict it.
                HDShadowManager.cachedShadowManager.EvictLight(this);
        }

        void OnDisable()
        {
            // If it is within the cached system we need to evict it, unless user explicitly requires not to.
            if (!preserveCachedShadow && lightIdxForCachedShadows >= 0)
            {
                HDShadowManager.cachedShadowManager.EvictLight(this);
            }

            SetEmissiveMeshRendererEnabled(false);
            s_overlappingHDLights.Remove(this);
        }

        void SetEmissiveMeshRendererEnabled(bool enabled)
        {
            if (displayAreaLightEmissiveMesh && emissiveMeshRenderer)
            {
                emissiveMeshRenderer.enabled = enabled;
            }
        }

        int GetShadowRequestCount(HDShadowSettings shadowSettings, HDLightType lightType)
        {
            return lightType == HDLightType.Point
                ? 6
                : lightType == HDLightType.Directional
                    ? shadowSettings.cascadeShadowSplitCount.value
                    : 1;
        }

        /// <summary>
        /// Request shadow map rendering when Update Mode is set to On Demand.
        /// </summary>
        public void RequestShadowMapRendering()
        {
            if(shadowUpdateMode == ShadowUpdateMode.OnDemand)
                HDShadowManager.cachedShadowManager.ScheduleShadowUpdate(this);
        }

        /// <summary>
        /// Some lights render more than one shadow maps (e.g. cascade shadow maps or point lights). This method is used to request the rendering of specific shadow map
        /// when Update Mode is set to On Demand. For example, to request the update of a second cascade, shadowIndex should be 1.
        /// Note: if shadowIndex is a 0-based index and it must be lower than the number of shadow maps a light renders (i.e. cascade count for directional lights, 6 for point lights).
        /// </summary>
        /// <param name="shadowIndex">The index of the subshadow to update.</param>
        public void RequestSubShadowMapRendering(int shadowIndex)
        {
            if (shadowUpdateMode == ShadowUpdateMode.OnDemand)
                HDShadowManager.cachedShadowManager.ScheduleShadowUpdate(this, shadowIndex);
        }

        internal bool ShadowIsUpdatedEveryFrame()
        {
            return shadowUpdateMode == ShadowUpdateMode.EveryFrame;
        }

        internal ShadowMapUpdateType GetShadowUpdateType(HDLightType lightType)
        {
            if (ShadowIsUpdatedEveryFrame()) return ShadowMapUpdateType.Dynamic;
#if MIXED_CACHED_SHADOW
            // Note: For now directional are not supported as it will require extra memory budget. This will change in a near future.
            if (m_AlwaysDrawDynamicShadows && lightType != HDLightType.Directional) return ShadowMapUpdateType.Mixed;
#endif
            return ShadowMapUpdateType.Cached;
        }

        internal void EvaluateShadowState(HDCamera hdCamera, in ProcessedLightData processedLight, CullingResults cullResults, FrameSettings frameSettings, int lightIndex)
        {
            Bounds bounds;

            m_WillRenderShadowMap = legacyLight.shadows != LightShadows.None && frameSettings.IsEnabled(FrameSettingsField.ShadowMaps);

            m_WillRenderShadowMap &= cullResults.GetShadowCasterBounds(lightIndex, out bounds);
            // When creating a new light, at the first frame, there is no AdditionalShadowData so we can't really render shadows
            m_WillRenderShadowMap &= shadowDimmer > 0;
            // If the shadow is too far away, we don't render it
            m_WillRenderShadowMap &= processedLight.lightType == HDLightType.Directional || processedLight.distanceToCamera < shadowFadeDistance;

            if (processedLight.lightType == HDLightType.Area && areaLightShape != AreaLightShape.Rectangle)
            {
                m_WillRenderShadowMap = false;
            }

            // First we reset the ray tracing and screen space shadow data
            m_WillRenderScreenSpaceShadow = false;
            m_WillRenderRayTracedShadow = false;

            // If this camera does not allow screen space shadows we are done, set the target parameters to false and leave the function
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ScreenSpaceShadows) || !m_WillRenderShadowMap)
                return;

            // Flag the ray tracing only shadows
            if (frameSettings.IsEnabled(FrameSettingsField.RayTracing) && m_UseRayTracedShadows)
            {
                bool validShadow = false;
                if (processedLight.gpuLightType == GPULightType.Point
                        || processedLight.gpuLightType == GPULightType.Rectangle
                        || (processedLight.gpuLightType == GPULightType.Spot && processedLight.lightVolumeType == LightVolumeType.Cone))
                {
                    validShadow = true;
                }

                if (validShadow)
                {
                    m_WillRenderScreenSpaceShadow = true;
                    m_WillRenderRayTracedShadow = true;
                }
            }

            // Flag the directional shadow
            if (useScreenSpaceShadows && processedLight.gpuLightType == GPULightType.Directional)
            {
                m_WillRenderScreenSpaceShadow = true;
                if (frameSettings.IsEnabled(FrameSettingsField.RayTracing) && m_UseRayTracedShadows)
                {
                    m_WillRenderRayTracedShadow = true;
                }
            }
        }

        internal int GetResolutionFromSettings(ShadowMapType shadowMapType, HDShadowInitParameters initParameters)
        {
            switch (shadowMapType)
            {
                case ShadowMapType.CascadedDirectional:
                    return Math.Min(m_ShadowResolution.Value(initParameters.shadowResolutionDirectional), initParameters.maxDirectionalShadowMapResolution);
                case ShadowMapType.PunctualAtlas:
                    return Math.Min(m_ShadowResolution.Value(initParameters.shadowResolutionPunctual), initParameters.maxPunctualShadowMapResolution);
                case ShadowMapType.AreaLightAtlas:
                    return Math.Min(m_ShadowResolution.Value(initParameters.shadowResolutionArea), initParameters.maxAreaShadowMapResolution);
                default:
                    return 0;
            }
        }

        internal void ReserveShadowMap(Camera camera, HDShadowManager shadowManager, HDShadowSettings shadowSettings, HDShadowInitParameters initParameters, VisibleLight visibleLight, HDLightType lightType)
        {
            if (!m_WillRenderShadowMap)
                return;

            // Create shadow requests array using the light type
            if (shadowRequests == null || m_ShadowRequestIndices == null)
            {
                const int maxLightShadowRequestsCount = 6;
                shadowRequests = new HDShadowRequest[maxLightShadowRequestsCount];
                m_ShadowRequestIndices = new int[maxLightShadowRequestsCount];

                for (int i = 0; i < maxLightShadowRequestsCount; i++)
                {
                    shadowRequests[i] = new HDShadowRequest();
                }
            }

            ShadowMapType shadowType = GetShadowMapType(lightType);

            // Reserve wanted resolution in the shadow atlas
            int resolution = GetResolutionFromSettings(shadowType, initParameters);
            Vector2 viewportSize = new Vector2(resolution, resolution);

            bool viewPortRescaling = false;

            // Compute dynamic shadow resolution
            viewPortRescaling |= (shadowType == ShadowMapType.PunctualAtlas && initParameters.punctualLightShadowAtlas.useDynamicViewportRescale);
            viewPortRescaling |= (shadowType == ShadowMapType.AreaLightAtlas && initParameters.areaLightShadowAtlas.useDynamicViewportRescale);

            bool shadowIsInCacheSystem = !ShadowIsUpdatedEveryFrame();

            if (viewPortRescaling && !shadowIsInCacheSystem)
            {
                // Formulas: https://www.desmos.com/calculator/tdodbuysut f(x) is the distance between 0 and 1, g(x) is the screen ratio (oscillating to simulate different light sizes)
                // The idea is to have a lot of resolution when the camera is close to the light OR the screen area is high.

                // linear normalized distance between the light and camera with max shadow distance
                float distance01 = Mathf.Clamp01(Vector3.Distance(camera.transform.position, visibleLight.GetPosition()) / shadowSettings.maxShadowDistance.value);
                // ease out and invert the curve, give more importance to closer distances
                distance01 = 1.0f - Mathf.Pow(distance01, 2);

                // normalized ratio between light range and distance 
                float range01 = Mathf.Clamp01(visibleLight.range / Vector3.Distance(camera.transform.position, visibleLight.GetPosition()));

                float scaleFactor01 = Mathf.Max(distance01, range01);

                // We allow a maximum of 64 rescale between the highest and lowest shadow resolution
                // It prevent having too many resolution changes when the player is moving.
                const float maxRescaleSteps = 64;
                scaleFactor01 = Mathf.RoundToInt(scaleFactor01 * maxRescaleSteps) / maxRescaleSteps;

                // resize viewport size by the normalized size of the light on screen
                viewportSize = Vector2.Lerp(HDShadowManager.k_MinShadowMapResolution * Vector2.one, viewportSize, scaleFactor01);
            }

            viewportSize = Vector2.Max(viewportSize, new Vector2(HDShadowManager.k_MinShadowMapResolution, HDShadowManager.k_MinShadowMapResolution));

            // Update the directional shadow atlas size
            if (lightType == HDLightType.Directional)
                shadowManager.UpdateDirectionalShadowResolution((int)viewportSize.x, shadowSettings.cascadeShadowSplitCount.value);

            int count = GetShadowRequestCount(shadowSettings, lightType);

            var updateType = GetShadowUpdateType(lightType);
            for (int index = 0; index < count; index++)
            {
                m_ShadowRequestIndices[index] = shadowManager.ReserveShadowResolutions(shadowIsInCacheSystem ? new Vector2(resolution, resolution) : viewportSize, shadowMapType, GetInstanceID(), index, updateType);
            }
        }

        internal bool WillRenderShadowMap()
        {
            return m_WillRenderShadowMap;
        }

        internal bool WillRenderScreenSpaceShadow()
        {
            return m_WillRenderScreenSpaceShadow;
        }

        internal bool WillRenderRayTracedShadow()
        {
            return m_WillRenderRayTracedShadow;
        }

        // This offset shift the position of the spotlight used to approximate the area light shadows. The offset is the minimum such that the full
        // area light shape is included in the cone spanned by the spot light.
        internal static float GetAreaLightOffsetForShadows(Vector2 shapeSize, float coneAngle)
        {
            float rectangleDiagonal = shapeSize.magnitude;
            float halfAngle = coneAngle * 0.5f;
            float cotanHalfAngle = 1.0f / Mathf.Tan(halfAngle * Mathf.Deg2Rad);
            float offset = rectangleDiagonal * cotanHalfAngle;

            return -offset;
        }

        private void UpdateDirectionalShadowRequest(HDShadowManager manager, HDShadowSettings shadowSettings, VisibleLight visibleLight, CullingResults cullResults, Vector2 viewportSize, int requestIndex, int lightIndex, Vector3 cameraPos, HDShadowRequest shadowRequest, out Matrix4x4 invViewProjection)
        {
            Vector4 cullingSphere;
            float nearPlaneOffset = QualitySettings.shadowNearPlaneOffset;

            HDShadowUtils.ExtractDirectionalLightData(
                visibleLight, viewportSize, (uint)requestIndex, shadowSettings.cascadeShadowSplitCount.value,
                shadowSettings.cascadeShadowSplits, nearPlaneOffset, cullResults, lightIndex,
                out shadowRequest.view, out invViewProjection, out shadowRequest.projection,
                out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip, out shadowRequest.splitData
            );

            cullingSphere = shadowRequest.splitData.cullingSphere;

            // Camera relative for directional light culling sphere
            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                cullingSphere.x -= cameraPos.x;
                cullingSphere.y -= cameraPos.y;
                cullingSphere.z -= cameraPos.z;
            }

            manager.UpdateCascade(requestIndex, cullingSphere, shadowSettings.cascadeShadowBorders[requestIndex]);
        }

        internal void UpdateShadowRequestData(HDCamera hdCamera, HDShadowManager manager, HDShadowSettings shadowSettings, VisibleLight visibleLight,
                                         CullingResults cullResults, int lightIndex, LightingDebugSettings lightingDebugSettings, HDShadowFilteringQuality filteringQuality,
                                         Vector2 viewportSize, HDLightType lightType, int shadowIndex, ref HDShadowRequest shadowRequest)
        {
            Matrix4x4 invViewProjection = Matrix4x4.identity;
            Vector3 cameraPos = hdCamera.mainViewConstants.worldSpaceCameraPos;

            // Write per light type matrices, splitDatas and culling parameters
            switch (lightType)
            {
                case HDLightType.Point:
                    HDShadowUtils.ExtractPointLightData(
                        visibleLight, viewportSize, shadowNearPlane,
                        normalBias, (uint)shadowIndex, filteringQuality, out shadowRequest.view,
                        out invViewProjection, out shadowRequest.projection,
                        out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip, out shadowRequest.splitData
                    );
                    break;
                case HDLightType.Spot:
                    float spotAngleForShadows = useCustomSpotLightShadowCone ? Math.Min(customSpotLightShadowCone, visibleLight.light.spotAngle) : visibleLight.light.spotAngle;
                    HDShadowUtils.ExtractSpotLightData(
                        spotLightShape, spotAngleForShadows, shadowNearPlane, aspectRatio, shapeWidth,
                        shapeHeight, visibleLight, viewportSize, normalBias, filteringQuality,
                        out shadowRequest.view, out invViewProjection, out shadowRequest.projection,
                        out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip, out shadowRequest.splitData
                    );
                    break;
                case HDLightType.Directional:
                    UpdateDirectionalShadowRequest(manager, shadowSettings, visibleLight, cullResults, viewportSize, shadowIndex, lightIndex, cameraPos, shadowRequest, out invViewProjection);
                    break;
                case HDLightType.Area:
                    switch (areaLightShape)
                    {
                        case AreaLightShape.Rectangle:
                            Vector2 shapeSize = new Vector2(shapeWidth, m_ShapeHeight);
                            float offset = GetAreaLightOffsetForShadows(shapeSize, areaLightShadowCone);
                            Vector3 shadowOffset = offset * visibleLight.GetForward();
                            HDShadowUtils.ExtractRectangleAreaLightData(visibleLight, visibleLight.GetPosition() + shadowOffset, areaLightShadowCone, shadowNearPlane, shapeSize, viewportSize, normalBias, filteringQuality,
                                out shadowRequest.view, out invViewProjection, out shadowRequest.projection, out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip, out shadowRequest.splitData);
                            break;
                        case AreaLightShape.Tube:
                            //Tube do not cast shadow at the moment.
                            //They should not call this method.
                            break;
                    }
                    break;
            }

            // Assign all setting common to every lights
            SetCommonShadowRequestSettings(shadowRequest, visibleLight, cameraPos, invViewProjection, viewportSize, lightIndex, lightType, filteringQuality);
        }

        internal int UpdateShadowRequest(HDCamera hdCamera, HDShadowManager manager, HDShadowSettings shadowSettings, VisibleLight visibleLight,
                                 CullingResults cullResults, int lightIndex, LightingDebugSettings lightingDebugSettings, HDShadowFilteringQuality filteringQuality, out int shadowRequestCount)
        {
            int firstShadowRequestIndex = -1;
            Vector3 cameraPos = hdCamera.mainViewConstants.worldSpaceCameraPos;
            shadowRequestCount = 0;

            HDLightType lightType = type;

            int count = GetShadowRequestCount(shadowSettings, lightType);
            var updateType = GetShadowUpdateType(lightType);
            bool hasCachedComponent = !ShadowIsUpdatedEveryFrame();
            bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);

            bool needsRenderingDueToTransformChange = false;
            // Note if we are in cached system, but if a placement has not been found by this point we bail out shadows
            bool shadowHasAtlasPlacement = true;
            if (hasCachedComponent)
            {
                // If we force evicted the light, it will have lightIdxForCachedShadows == -1
                shadowHasAtlasPlacement = !HDShadowManager.cachedShadowManager.LightIsPendingPlacement(this, shadowMapType) && (lightIdxForCachedShadows != -1);
                needsRenderingDueToTransformChange = HDShadowManager.cachedShadowManager.NeedRenderingDueToTransformChange(this, lightType);
            }

            for (int index = 0; index < count; index++)
            {
                var shadowRequest = shadowRequests[index];

                Matrix4x4 invViewProjection = Matrix4x4.identity;
                int shadowRequestIndex = m_ShadowRequestIndices[index];

                HDShadowResolutionRequest resolutionRequest = manager.GetResolutionRequest(shadowRequestIndex);

                if (resolutionRequest == null)
                    continue;

                int cachedShadowID = lightIdxForCachedShadows + index;
                bool needToUpdateCachedContent = false;
                bool needToUpdateDynamicContent = !isSampledFromCache;
                bool hasUpdatedRequestData = false;

                if (hasCachedComponent && shadowHasAtlasPlacement)
                {
                    needToUpdateCachedContent = needsRenderingDueToTransformChange || HDShadowManager.cachedShadowManager.ShadowIsPendingUpdate(cachedShadowID, shadowMapType);
                    HDShadowManager.cachedShadowManager.UpdateResolutionRequest(ref resolutionRequest, cachedShadowID, shadowMapType);
                }

                shadowRequest.isInCachedAtlas = isSampledFromCache;
                shadowRequest.isMixedCached = updateType == ShadowMapUpdateType.Mixed;
                shadowRequest.shouldUseCachedShadowData = false;

                Vector2 viewportSize = resolutionRequest.resolution;

                if (shadowRequestIndex == -1)
                    continue;

                if (needToUpdateCachedContent)
                {
                    m_CachedViewPos = cameraPos;
                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                    // Write per light type matrices, splitDatas and culling parameters
                    UpdateShadowRequestData(hdCamera, manager, shadowSettings, visibleLight, cullResults, lightIndex, lightingDebugSettings, filteringQuality, viewportSize, lightType, index, ref shadowRequest);

                    hasUpdatedRequestData = true;
                    shadowRequest.shouldUseCachedShadowData = false;
                    shadowRequest.shouldRenderCachedComponent = true;
                }
                else if(hasCachedComponent)
                {
                    shadowRequest.cachedShadowData.cacheTranslationDelta = cameraPos - m_CachedViewPos;
                    shadowRequest.shouldUseCachedShadowData = true;
                    shadowRequest.shouldRenderCachedComponent = false;
                    // If directional we still need to calculate the split data.
                    if (lightType == HDLightType.Directional)
                        UpdateDirectionalShadowRequest(manager, shadowSettings, visibleLight, cullResults, viewportSize, index, lightIndex, cameraPos, shadowRequest, out invViewProjection);

                }

                if(needToUpdateDynamicContent && !hasUpdatedRequestData)
                {
                    shadowRequest.shouldUseCachedShadowData = false;

                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
                    // Write per light type matrices, splitDatas and culling parameters
                    UpdateShadowRequestData(hdCamera, manager, shadowSettings, visibleLight, cullResults, lightIndex, lightingDebugSettings, filteringQuality, viewportSize, lightType, index, ref shadowRequest);
                }

                shadowRequest.dynamicAtlasViewport = resolutionRequest.dynamicAtlasViewport;
                shadowRequest.cachedAtlasViewport = resolutionRequest.cachedAtlasViewport;
                manager.UpdateShadowRequest(shadowRequestIndex, shadowRequest, updateType);

                if (needToUpdateCachedContent)
                {
                    // Handshake with the cached shadow manager to notify about the rendering.
                    // Technically the rendering has not happened yet, but it is scheduled.
                    HDShadowManager.cachedShadowManager.MarkShadowAsRendered(cachedShadowID, shadowMapType);
                }

                // Store the first shadow request id to return it
                if (firstShadowRequestIndex == -1)
                    firstShadowRequestIndex = shadowRequestIndex;

                shadowRequestCount++;
            }

            return shadowHasAtlasPlacement ? firstShadowRequestIndex : -1;
        }

        void SetCommonShadowRequestSettings(HDShadowRequest shadowRequest, VisibleLight visibleLight, Vector3 cameraPos, Matrix4x4 invViewProjection, Vector2 viewportSize, int lightIndex, HDLightType lightType, HDShadowFilteringQuality filteringQuality)
        {
            // zBuffer param to reconstruct depth position (for transmission)
            float f = legacyLight.range;
            float n = shadowNearPlane;
            shadowRequest.zBufferParam = new Vector4((f-n)/n, 1.0f, (f-n)/(n*f), 1.0f/f);
            shadowRequest.worldTexelSize = 2.0f / shadowRequest.deviceProjectionYFlip.m00 / viewportSize.x * Mathf.Sqrt(2.0f);
            shadowRequest.normalBias = normalBias;

            // Make light position camera relative:
            // TODO: think about VR (use different camera position for each eye)
            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                CoreMatrixUtils.MatrixTimesTranslation(ref shadowRequest.view, cameraPos);
                CoreMatrixUtils.TranslationTimesMatrix(ref invViewProjection, -cameraPos);
            }

            bool hasOrthoMatrix = false;
            if (lightType == HDLightType.Directional || lightType == HDLightType.Spot && spotLightShape == SpotLightShape.Box)
            {
                hasOrthoMatrix = true;
                shadowRequest.position = new Vector3(shadowRequest.view.m03, shadowRequest.view.m13, shadowRequest.view.m23);
            }
            else
            {
                var vlPos = visibleLight.GetPosition();
                shadowRequest.position = (ShaderConfig.s_CameraRelativeRendering != 0) ? vlPos - cameraPos : vlPos;
            }

            shadowRequest.shadowToWorld = invViewProjection.transpose;
            shadowRequest.zClip = (lightType != HDLightType.Directional);
            shadowRequest.lightIndex = lightIndex;
            // We don't allow shadow resize for directional cascade shadow
            if (lightType == HDLightType.Directional)
            {
                shadowRequest.shadowMapType = ShadowMapType.CascadedDirectional;
            }
            else if (lightType == HDLightType.Area && areaLightShape == AreaLightShape.Rectangle)
            {
                shadowRequest.shadowMapType = ShadowMapType.AreaLightAtlas;
            }
            else
            {
                shadowRequest.shadowMapType = ShadowMapType.PunctualAtlas;
            }

            // shadow clip planes (used for tessellation clipping)
            GeometryUtility.CalculateFrustumPlanes(CoreMatrixUtils.MultiplyProjectionMatrix(shadowRequest.projection, shadowRequest.view, hasOrthoMatrix), m_ShadowFrustumPlanes);
            if (shadowRequest.frustumPlanes?.Length != 6)
                shadowRequest.frustumPlanes = new Vector4[6];
            // Left, right, top, bottom, near, far.
            for (int i = 0; i < 6; i++)
            {
                shadowRequest.frustumPlanes[i] = new Vector4(
                    m_ShadowFrustumPlanes[i].normal.x,
                    m_ShadowFrustumPlanes[i].normal.y,
                    m_ShadowFrustumPlanes[i].normal.z,
                    m_ShadowFrustumPlanes[i].distance
                );
            }


            float softness = 0.0f;
            if (lightType == HDLightType.Directional)
            {
                var devProj = shadowRequest.deviceProjection;
                float frustumExtentZ = Vector4.Dot(new Vector4(devProj.m32, -devProj.m32, -devProj.m22, devProj.m22), new Vector4(devProj.m22, devProj.m32, devProj.m23, devProj.m33)) /
                        (devProj.m22 * (devProj.m22 - devProj.m32));

                // We use the light view frustum derived from view projection matrix and angular diameter to work out a filter size in
                // shadow map space, essentially figuring out the footprint of the cone subtended by the light on the shadow map
                float halfAngleTan = Mathf.Tan(0.5f * Mathf.Deg2Rad * (softnessScale * m_AngularDiameter) / 2);
                softness = Mathf.Abs(halfAngleTan * frustumExtentZ / (2.0f * shadowRequest.splitData.cullingSphere.w));
                float range = 2.0f * (1.0f / devProj.m22);
                float rangeScale = Mathf.Abs(range)  / 100.0f;
                shadowRequest.zBufferParam.x = rangeScale;
            }
            else
            {
                // This derivation has been fitted with quartic regression checking against raytracing reference and with a resolution of 512
                float x = m_ShapeRadius * softnessScale;
                float x2 = x * x;
                softness = 0.02403461f + 3.452916f * x - 1.362672f * x2 + 0.6700115f * x2 * x + 0.2159474f * x2 * x2;
                softness /= 100.0f;
            }

            var viewportWidth = shadowRequest.isInCachedAtlas ? shadowRequest.cachedAtlasViewport.width : shadowRequest.dynamicAtlasViewport.width;
            softness *= (viewportWidth / 512);  // Make it resolution independent whereas the baseline is 512

            // Bias
            // This base bias is a good value if we expose a [0..1] since values within [0..5] are empirically shown to be sensible for the slope-scale bias with the width of our PCF.
            float baseBias = 5.0f;
            // If we are PCSS, the blur radius can be quite big, hence we need to tweak up the slope bias
            if (filteringQuality == HDShadowFilteringQuality.High)
            {
                if(softness > 0.01f)
                {
                    // maxBaseBias is an empirically set value, also the lerp stops at a shadow softness of 0.05, then is clamped.
                    float maxBaseBias = 18.0f;
                    baseBias = Mathf.Lerp(baseBias, maxBaseBias, Mathf.Min(1.0f, (softness * 100) / 5));
                }
            }

            shadowRequest.slopeBias = HDShadowUtils.GetSlopeBias(baseBias, slopeBias);

            // Shadow algorithm parameters
            shadowRequest.shadowSoftness = softness;
            shadowRequest.blockerSampleCount = blockerSampleCount;
            shadowRequest.filterSampleCount = filterSampleCount;
            shadowRequest.minFilterSize = minFilterSize * 0.001f; // This divide by 1000 is here to have a range [0...1] exposed to user

            shadowRequest.kernelSize = (uint)kernelSize;
            shadowRequest.lightAngle = (lightAngle * Mathf.PI / 180.0f);
            shadowRequest.maxDepthBias = maxDepthBias;
            // We transform it to base two for faster computation.
            // So e^x = 2^y where y = x * log2 (e)
            const float log2e = 1.44269504089f;
            shadowRequest.evsmParams.x = evsmExponent * log2e;
            shadowRequest.evsmParams.y = evsmLightLeakBias;
            shadowRequest.evsmParams.z = m_EvsmVarianceBias;
            shadowRequest.evsmParams.w = evsmBlurPasses;
        }

        // We need these old states to make timeline and the animator record the intensity value and the emissive mesh changes
        [System.NonSerialized]
        TimelineWorkaround timelineWorkaround = new TimelineWorkaround();

#if UNITY_EDITOR

        // Force to retrieve color light's m_UseColorTemperature because it's private
        [System.NonSerialized]
        SerializedProperty m_UseColorTemperatureProperty;
        SerializedProperty useColorTemperatureProperty
        {
            get
            {
                if (m_UseColorTemperatureProperty == null)
                {
                    m_UseColorTemperatureProperty = lightSerializedObject.FindProperty("m_UseColorTemperature");
                }

                return m_UseColorTemperatureProperty;
            }
        }

        [System.NonSerialized]
        SerializedObject m_LightSerializedObject;
        SerializedObject lightSerializedObject
        {
            get
            {
                if (m_LightSerializedObject == null)
                {
                    m_LightSerializedObject = new SerializedObject(legacyLight);
                }

                return m_LightSerializedObject;
            }
        }

#endif

        internal bool useColorTemperature
        {
            get => legacyLight.useColorTemperature;
            set
            {
                if (legacyLight.useColorTemperature == value)
                    return;

                legacyLight.useColorTemperature = value;
            }
        }

        // TODO: we might be able to get rid to that
        [System.NonSerialized]
        bool m_Animated;

        private void Start()
        {
            // If there is an animator attached ot the light, we assume that some of the light properties
            // might be driven by this animator (using timeline or animations) so we force the LateUpdate
            // to sync the animated HDAdditionalLightData properties with the light component.
            m_Animated = GetComponent<Animator>() != null;
        }

        // TODO: There are a lot of old != current checks and assignation in this function, maybe think about using another system ?
        void LateUpdate()
        {
            // We force the animation in the editor and in play mode when there is an animator component attached to the light
#if !UNITY_EDITOR
            if (!m_Animated)
                return;
#endif

#if UNITY_EDITOR

            // If modification are due to change on prefab asset that are non overridden on this prefab instance
            if (m_NeedsPrefabInstanceCheck && PrefabUtility.IsPartOfPrefabInstance(this) && ((PrefabUtility.GetCorrespondingObjectFromOriginalSource(this) as HDAdditionalLightData)?.needRefreshPrefabInstanceEmissiveMeshes ?? false))
            {
                needRefreshPrefabInstanceEmissiveMeshes = true;
            }
            m_NeedsPrefabInstanceCheck = false;

            // Update the list of overlapping lights for the LightOverlap scene view mode
            if (IsOverlapping())
                s_overlappingHDLights.Add(this);
            else
                s_overlappingHDLights.Remove(this);
#endif

#if UNITY_EDITOR

            // If we requested an emissive mesh but for some reason (e.g. Reload scene unchecked in the Enter Playmode options) Awake has not been called,
            // we need to create it manually.
            if (m_DisplayAreaLightEmissiveMesh && (m_ChildEmissiveMeshViewer == null || m_ChildEmissiveMeshViewer.Equals(null)))
            {
                UpdateAreaLightEmissiveMesh();
            }

            //if not parented anymore, refresh it
            if (m_ChildEmissiveMeshViewer != null && !m_ChildEmissiveMeshViewer.Equals(null))
            {
                if (m_ChildEmissiveMeshViewer.transform.parent != transform)
                {
                    CreateChildEmissiveMeshViewerIfNeeded();
                    UpdateAreaLightEmissiveMesh();
                }
                if (m_ChildEmissiveMeshViewer.gameObject.isStatic != gameObject.isStatic)
                    m_ChildEmissiveMeshViewer.gameObject.isStatic = gameObject.isStatic;
                if (GameObjectUtility.GetStaticEditorFlags(m_ChildEmissiveMeshViewer.gameObject) != GameObjectUtility.GetStaticEditorFlags(gameObject))
                    GameObjectUtility.SetStaticEditorFlags(m_ChildEmissiveMeshViewer.gameObject, GameObjectUtility.GetStaticEditorFlags(gameObject));
            }
#endif

            //auto change layer on emissive mesh
            if (areaLightEmissiveMeshLayer == -1
                && m_ChildEmissiveMeshViewer != null && !m_ChildEmissiveMeshViewer.Equals(null)
                && m_ChildEmissiveMeshViewer.gameObject.layer != gameObject.layer)
                m_ChildEmissiveMeshViewer.gameObject.layer = gameObject.layer;

            // Delayed cleanup when removing emissive mesh from timeline
            if (needRefreshEmissiveMeshesFromTimeLineUpdate)
            {
                needRefreshEmissiveMeshesFromTimeLineUpdate = false;
                UpdateAreaLightEmissiveMesh();
            }

#if UNITY_EDITOR
            // Prefab instance child emissive mesh update
            if (needRefreshPrefabInstanceEmissiveMeshes)
            {
                // We must not call the update on Prefab Asset that are already updated or we will enter infinite loop
                if (!PrefabUtility.IsPartOfPrefabAsset(this))
                {
                    UpdateAreaLightEmissiveMesh();
                }
                needRefreshPrefabInstanceEmissiveMeshes = false;
            }
#endif

            Vector3 shape = new Vector3(shapeWidth, m_ShapeHeight, shapeRadius);

            if (legacyLight.enabled != timelineWorkaround.lightEnabled)
            {
                SetEmissiveMeshRendererEnabled(legacyLight.enabled);
                timelineWorkaround.lightEnabled = legacyLight.enabled;
            }

            // Check if the intensity have been changed by the inspector or an animator
            if (timelineWorkaround.oldLossyScale != transform.lossyScale
                || intensity != timelineWorkaround.oldIntensity
                || legacyLight.colorTemperature != timelineWorkaround.oldLightColorTemperature)
            {
                UpdateLightIntensity();
                UpdateAreaLightEmissiveMesh();
                timelineWorkaround.oldLossyScale = transform.lossyScale;
                timelineWorkaround.oldIntensity = intensity;
                timelineWorkaround.oldLightColorTemperature = legacyLight.colorTemperature;
            }

            // Same check for light angle to update intensity using spot angle
            if (type == HDLightType.Spot && (timelineWorkaround.oldSpotAngle != legacyLight.spotAngle))
            {
                UpdateLightIntensity();
                timelineWorkaround.oldSpotAngle = legacyLight.spotAngle;
            }

            if (legacyLight.color != timelineWorkaround.oldLightColor
                || timelineWorkaround.oldLossyScale != transform.lossyScale
                || displayAreaLightEmissiveMesh != timelineWorkaround.oldDisplayAreaLightEmissiveMesh
                || legacyLight.colorTemperature != timelineWorkaround.oldLightColorTemperature)
            {
                UpdateAreaLightEmissiveMesh();
                timelineWorkaround.oldLightColor = legacyLight.color;
                timelineWorkaround.oldLossyScale = transform.lossyScale;
                timelineWorkaround.oldDisplayAreaLightEmissiveMesh = displayAreaLightEmissiveMesh;
                timelineWorkaround.oldLightColorTemperature = legacyLight.colorTemperature;
            }
        }

        void OnDidApplyAnimationProperties()
        {
            UpdateAllLightValues(fromTimeLine: true);
        }

        /// <summary>
        /// Copy all field from this to an additional light data
        /// </summary>
        /// <param name="data">Destination component</param>
        public void CopyTo(HDAdditionalLightData data)
        {
            data.enableSpotReflector = enableSpotReflector;
            data.luxAtDistance = luxAtDistance;
            data.m_InnerSpotPercent = m_InnerSpotPercent;
            data.lightDimmer = lightDimmer;
            data.volumetricDimmer = volumetricDimmer;
            data.lightUnit = lightUnit;
            data.m_FadeDistance = m_FadeDistance;
            data.affectDiffuse = affectDiffuse;
            data.m_AffectSpecular = m_AffectSpecular;
            data.nonLightmappedOnly = nonLightmappedOnly;
            data.m_PointlightHDType = m_PointlightHDType;
            data.spotLightShape = spotLightShape;
            data.shapeWidth = shapeWidth;
            data.m_ShapeHeight = m_ShapeHeight;
            data.aspectRatio = aspectRatio;
            data.shapeRadius = shapeRadius;
            data.m_MaxSmoothness = maxSmoothness;
            data.m_ApplyRangeAttenuation = m_ApplyRangeAttenuation;
            data.useOldInspector = useOldInspector;
            data.featuresFoldout = featuresFoldout;
            data.showAdditionalSettings = showAdditionalSettings;
            data.m_Intensity = m_Intensity;
            data.displayAreaLightEmissiveMesh = displayAreaLightEmissiveMesh;
            data.interactsWithSky = interactsWithSky;
            data.angularDiameter = angularDiameter;
            data.flareSize = flareSize;
            data.flareTint = flareTint;
            data.surfaceTexture = surfaceTexture;
            data.surfaceTint = surfaceTint;
            data.distance = distance;

            shadowResolution.CopyTo(data.shadowResolution);
            data.shadowDimmer = shadowDimmer;
            data.volumetricShadowDimmer = volumetricShadowDimmer;
            data.shadowFadeDistance = shadowFadeDistance;
            useContactShadow.CopyTo(data.useContactShadow);
            data.slopeBias = slopeBias;
            data.normalBias = normalBias;
            data.shadowCascadeRatios = new float[shadowCascadeRatios.Length];
            shadowCascadeRatios.CopyTo(data.shadowCascadeRatios, 0);
            data.shadowCascadeBorders = new float[shadowCascadeBorders.Length];
            shadowCascadeBorders.CopyTo(data.shadowCascadeBorders, 0);
            data.shadowAlgorithm = shadowAlgorithm;
            data.shadowVariant = shadowVariant;
            data.shadowPrecision = shadowPrecision;
            data.shadowUpdateMode = shadowUpdateMode;

            data.m_UseCustomSpotLightShadowCone = useCustomSpotLightShadowCone;
            data.m_CustomSpotLightShadowCone = customSpotLightShadowCone;

#if UNITY_EDITOR
            data.timelineWorkaround = timelineWorkaround;
#endif
        }

        // As we have our own default value, we need to initialize the light intensity correctly
        /// <summary>
        /// Initialize an HDAdditionalLightData that have just beeing created.
        /// </summary>
        /// <param name="lightData"></param>
        public static void InitDefaultHDAdditionalLightData(HDAdditionalLightData lightData)
        {
            // Special treatment for Unity built-in area light. Change it to our rectangle light
            var light = lightData.gameObject.GetComponent<Light>();

            // Set light intensity and unit using its type
            //note: requiring type convert Rectangle and Disc to Area and correctly set areaLight
            switch (lightData.type)
            {
                case HDLightType.Directional:
                    lightData.lightUnit = LightUnit.Lux;
                    lightData.intensity = k_DefaultDirectionalLightIntensity / Mathf.PI * 100000.0f; // Change back to just k_DefaultDirectionalLightIntensity on 11.0.0 (can't change constant as it's a breaking change)
                    break;
                case HDLightType.Area: // Rectangle by default when light is created
                    switch (lightData.areaLightShape)
                    {
                        case AreaLightShape.Rectangle:
                            lightData.lightUnit = LightUnit.Lumen;
                            lightData.intensity = k_DefaultAreaLightIntensity;
                            light.shadows = LightShadows.None;
                            break;
                        case AreaLightShape.Disc:
                            //[TODO: to be defined]
                            break;
                    }
                    break;
                case HDLightType.Point:
                case HDLightType.Spot:
                    lightData.lightUnit = LightUnit.Lumen;
                    lightData.intensity = k_DefaultPunctualLightIntensity;
                    break;
            }

            // We don't use the global settings of shadow mask by default
            light.lightShadowCasterMode = LightShadowCasterMode.Everything;

            lightData.normalBias           = 0.75f;
            lightData.slopeBias            = 0.5f;

            // Enable filter/temperature mode by default for all light types
            lightData.useColorTemperature = true;
        }

        void OnValidate()
        {
            UpdateBounds();

            RefreshCachedShadow();

#if UNITY_EDITOR
            // If modification are due to change on prefab asset, we want to have prefab instances to self-update, but we cannot check in OnValidate if this is part of
            // prefab instance. So we delay the check on next update (and before teh LateUpdate logic)
            m_NeedsPrefabInstanceCheck = true;
#endif
        }

#region Update functions to patch values in the Light component when we change properties inside HDAdditionalLightData

        void SetLightIntensityPunctual(float intensity)
        {
            switch (type)
            {
                case HDLightType.Directional:
                    legacyLight.intensity = intensity; // Always in lux
                    break;
                case HDLightType.Point:
                    if (lightUnit == LightUnit.Candela)
                        legacyLight.intensity = intensity;
                    else
                        legacyLight.intensity = LightUtils.ConvertPointLightLumenToCandela(intensity);
                    break;
                case HDLightType.Spot:
                    if (lightUnit == LightUnit.Candela)
                    {
                        // When using candela, reflector don't have any effect. Our intensity is candela = lumens/steradian and the user
                        // provide desired value for an angle of 1 steradian.
                        legacyLight.intensity = intensity;
                    }
                    else  // lumen
                    {
                        if (enableSpotReflector)
                        {
                            // If reflector is enabled all the lighting from the sphere is focus inside the solid angle of current shape
                            if (spotLightShape == SpotLightShape.Cone)
                            {
                                legacyLight.intensity = LightUtils.ConvertSpotLightLumenToCandela(intensity, legacyLight.spotAngle * Mathf.Deg2Rad, true);
                            }
                            else if (spotLightShape == SpotLightShape.Pyramid)
                            {
                                float angleA, angleB;
                                LightUtils.CalculateAnglesForPyramid(aspectRatio, legacyLight.spotAngle * Mathf.Deg2Rad, out angleA, out angleB);

                                legacyLight.intensity = LightUtils.ConvertFrustrumLightLumenToCandela(intensity, angleA, angleB);
                            }
                            else // Box shape, fallback to punctual light.
                            {
                                legacyLight.intensity = LightUtils.ConvertPointLightLumenToCandela(intensity);
                            }
                        }
                        else
                        {
                            // No reflector, angle act as occlusion of point light.
                            legacyLight.intensity = LightUtils.ConvertPointLightLumenToCandela(intensity);
                        }
                    }
                    break;
            }
        }

        void UpdateLightIntensity()
        {
            if (lightUnit == LightUnit.Lumen)
            {
                if (m_PointlightHDType == PointLightHDType.Punctual)
                    SetLightIntensityPunctual(intensity);
                else
                    legacyLight.intensity = LightUtils.ConvertAreaLightLumenToLuminance(areaLightShape, intensity, shapeWidth, m_ShapeHeight);
            }
            else if (lightUnit == LightUnit.Ev100)
            {
                legacyLight.intensity = LightUtils.ConvertEvToLuminance(m_Intensity);
            }
            else
            {
                HDLightType lightType = type;
                if ((lightType == HDLightType.Spot || lightType == HDLightType.Point) && lightUnit == LightUnit.Lux)
                {
                    // Box are local directional light with lux unity without at distance
                    if ((lightType == HDLightType.Spot) && (spotLightShape == SpotLightShape.Box))
                        legacyLight.intensity = m_Intensity;
                    else
                        legacyLight.intensity = LightUtils.ConvertLuxToCandela(m_Intensity, luxAtDistance);
                }
                else
                    legacyLight.intensity = m_Intensity;
            }

#if UNITY_EDITOR
            legacyLight.SetLightDirty(); // Should be apply only to parameter that's affect GI, but make the code cleaner
#endif
        }

        void Awake()
        {
            Migrate();

            // We need to reconstruct the emissive mesh at Light creation if needed due to not beeing able to change hierarchy in prefab asset.
            // This is especially true at Tuntime as there is no code path that will trigger the rebuild of emissive mesh until one of the property modifying it is changed.
            UpdateAreaLightEmissiveMesh();
        }

        internal void UpdateAreaLightEmissiveMesh(bool fromTimeLine = false)
        {
            bool isAreaLight = type == HDLightType.Area;
            bool displayEmissiveMesh = isAreaLight && displayAreaLightEmissiveMesh;

            // Only show childEmissiveMeshViewer if type is Area and requested
            if (!isAreaLight || !displayEmissiveMesh)
            {
                if (m_ChildEmissiveMeshViewer)
                {
                    if (fromTimeLine)
                    {
                        // Cannot perform destroy in OnDidApplyAnimationProperties
                        // So shut down rendering instead and set up a flag for cleaning later
                        emissiveMeshRenderer.enabled = false;
                        needRefreshEmissiveMeshesFromTimeLineUpdate = true;
                    }
                    else
                        DestroyChildEmissiveMeshViewer();
                }

                // We don't have anything to do left if the dislay emissive mesh option is disabled
                return;
            }
#if UNITY_EDITOR
            else if (PrefabUtility.IsPartOfPrefabAsset(this))
            {
                // Child emissive mesh should not be handled in asset but we must trigger every instance to update themselves. Will be done in OnValidate
                needRefreshPrefabInstanceEmissiveMeshes = true;

                // We don't have anything to do left as the child will never appear while editing the prefab asset
                return;
            }
#endif
            else
            {
                CreateChildEmissiveMeshViewerIfNeeded();

#if UNITY_EDITOR
                // In Prefab Instance, as we can be called from OnValidate due to Prefab Asset modification, we need to refresh modification on child emissive mesh
                if (needRefreshPrefabInstanceEmissiveMeshes && PrefabUtility.IsPartOfPrefabInstance(this))
                {
                    emissiveMeshRenderer.shadowCastingMode = m_AreaLightEmissiveMeshShadowCastingMode;
                    emissiveMeshRenderer.motionVectorGenerationMode = m_AreaLightEmissiveMeshMotionVectorGenerationMode;
                }
#endif
            }

            // Update Mesh
            if (HDRenderPipeline.defaultAsset != null)
            {
                switch (areaLightShape)
                {
                    case AreaLightShape.Tube:
                        if (m_EmissiveMeshFilter.sharedMesh != HDRenderPipeline.defaultAsset.renderPipelineResources.assets.emissiveCylinderMesh)
                            m_EmissiveMeshFilter.sharedMesh = HDRenderPipeline.defaultAsset.renderPipelineResources.assets.emissiveCylinderMesh;
                        break;
                    case AreaLightShape.Rectangle:
                    default:
                        if (m_EmissiveMeshFilter.sharedMesh != HDRenderPipeline.defaultAsset.renderPipelineResources.assets.emissiveQuadMesh)
                            m_EmissiveMeshFilter.sharedMesh = HDRenderPipeline.defaultAsset.renderPipelineResources.assets.emissiveQuadMesh;
                        break;
                }
            }

            // Update light area size with clamping
            Vector3 lightSize = new Vector3(m_ShapeWidth, m_ShapeHeight, 0);
            if (areaLightShape == AreaLightShape.Tube)
                lightSize.y = 0;
            lightSize = Vector3.Max(Vector3.one * k_MinAreaWidth, lightSize);

            switch (areaLightShape)
            {
                case AreaLightShape.Rectangle:
                    m_ShapeWidth = lightSize.x;
                    m_ShapeHeight = lightSize.y;
                    break;
                case AreaLightShape.Tube:
                    m_ShapeWidth = lightSize.x;
                    break;
                default:
                    break;
            }

#if UNITY_EDITOR
            legacyLight.areaSize = lightSize;
#endif

            // Update child emissive mesh scale
            Vector3 lossyScale = emissiveMeshRenderer.transform.localRotation * transform.lossyScale;
            emissiveMeshRenderer.transform.localScale = new Vector3(lightSize.x / lossyScale.x, lightSize.y / lossyScale.y, k_MinAreaWidth / lossyScale.z);

            // NOTE: When the user duplicates a light in the editor, the material is not duplicated and when changing the properties of one of them (source or duplication)
            // It either overrides both or is overriden. Given that when we duplicate an object the name changes, this approach works. When the name of the game object is then changed again
            // the material is not re-created until one of the light properties is changed again.
            if (emissiveMeshRenderer.sharedMaterial == null || emissiveMeshRenderer.sharedMaterial.name != gameObject.name)
            {
                emissiveMeshRenderer.sharedMaterial = new Material(Shader.Find("HDRP/Unlit"));
                emissiveMeshRenderer.sharedMaterial.SetFloat("_IncludeIndirectLighting", 0.0f);
                emissiveMeshRenderer.sharedMaterial.name = gameObject.name;
            }

            // Update Mesh emissive properties
            emissiveMeshRenderer.sharedMaterial.SetColor("_UnlitColor", Color.black);

            // m_Light.intensity is in luminance which is the value we need for emissive color
            Color value = legacyLight.color.linear * legacyLight.intensity;

// We don't have access to the color temperature in the player because it's a private member of the Light component
#if UNITY_EDITOR
            if (useColorTemperature)
                value *= Mathf.CorrelatedColorTemperatureToRGB(legacyLight.colorTemperature);
#endif

            value *= lightDimmer;

            emissiveMeshRenderer.sharedMaterial.SetColor("_EmissiveColor", value);

            bool enableEmissiveColorMap = false;
            // Set the cookie (if there is one) and raise or remove the shader feature
            if (displayEmissiveMesh && areaLightCookie != null && areaLightCookie != Texture2D.whiteTexture)
            {
                emissiveMeshRenderer.sharedMaterial.SetTexture("_EmissiveColorMap", areaLightCookie);
                enableEmissiveColorMap = true;
            }
            else if (displayEmissiveMesh && IESSpot != null && IESSpot != Texture2D.whiteTexture)
            {
                emissiveMeshRenderer.sharedMaterial.SetTexture("_EmissiveColorMap", IESSpot);
                enableEmissiveColorMap = true;
            }
            else
            {
                emissiveMeshRenderer.sharedMaterial.SetTexture("_EmissiveColorMap", Texture2D.whiteTexture);
            }
            CoreUtils.SetKeyword(emissiveMeshRenderer.sharedMaterial, "_EMISSIVE_COLOR_MAP", enableEmissiveColorMap);

            if (m_AreaLightEmissiveMeshLayer != -1)
                emissiveMeshRenderer.gameObject.layer = m_AreaLightEmissiveMeshLayer;
        }

        void UpdateRectangleLightBounds()
        {
            legacyLight.useShadowMatrixOverride = false;
            legacyLight.useBoundingSphereOverride = true;
            float halfWidth = m_ShapeWidth * 0.5f;
            float halfHeight = m_ShapeHeight * 0.5f;
            float diag = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
            legacyLight.boundingSphereOverride = new Vector4(0.0f, 0.0f, 0.0f, Mathf.Max(range, diag));
        }

        void UpdateTubeLightBounds()
        {
            legacyLight.useShadowMatrixOverride = false;
            legacyLight.useBoundingSphereOverride = true;
            legacyLight.boundingSphereOverride = new Vector4(0.0f, 0.0f, 0.0f, Mathf.Max(range, m_ShapeWidth * 0.5f));
        }

        void UpdateBoxLightBounds()
        {
            legacyLight.useShadowMatrixOverride = true;
            legacyLight.useBoundingSphereOverride = true;

            // Need to inverse scale because culling != rendering convention apparently
            Matrix4x4 scaleMatrix = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f));
            legacyLight.shadowMatrixOverride = HDShadowUtils.ExtractBoxLightProjectionMatrix(legacyLight.range, shapeWidth, m_ShapeHeight, shadowNearPlane) * scaleMatrix;

            // Very conservative bounding sphere taking the diagonal of the shape as the radius
            float diag = new Vector3(shapeWidth * 0.5f, m_ShapeHeight * 0.5f, legacyLight.range * 0.5f).magnitude;
            legacyLight.boundingSphereOverride = new Vector4(0.0f, 0.0f, legacyLight.range * 0.5f, diag);
        }

        void UpdatePyramidLightBounds()
        {
            legacyLight.useShadowMatrixOverride = true;
            legacyLight.useBoundingSphereOverride = true;

            // Need to inverse scale because culling != rendering convention apparently
            Matrix4x4 scaleMatrix = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f));
            legacyLight.shadowMatrixOverride = HDShadowUtils.ExtractSpotLightProjectionMatrix(legacyLight.range, legacyLight.spotAngle, shadowNearPlane, aspectRatio, 0.0f) * scaleMatrix;
            legacyLight.boundingSphereOverride = new Vector4(0.0f, 0.0f, 0.0f, legacyLight.range);
        }

        void UpdateBounds()
        {
            switch (type)
            {
                case HDLightType.Spot:
                    switch (spotLightShape)
                    {
                        case SpotLightShape.Box:
                            UpdateBoxLightBounds();
                            break;
                        case SpotLightShape.Pyramid:
                            UpdatePyramidLightBounds();
                            break;
                        default: // Cone
                            legacyLight.useBoundingSphereOverride = false;
                            legacyLight.useShadowMatrixOverride = false;
                            break;
                    }
                    break;
                case HDLightType.Area:
                    switch (areaLightShape)
                    {
                        case AreaLightShape.Rectangle:
                            UpdateRectangleLightBounds();
                            break;
                        case AreaLightShape.Tube:
                            UpdateTubeLightBounds();
                            break;
                    }
                    break;
                default:
                    legacyLight.useBoundingSphereOverride = false;
                    legacyLight.useShadowMatrixOverride = false;
                    break;
            }
        }

        void UpdateShapeSize()
        {
            // Force to clamp the shape if we changed the type of the light
            shapeWidth = m_ShapeWidth;
            shapeHeight = m_ShapeHeight;

#if UNITY_EDITOR
            // We don't want to update the disc area since their shape is largely handled by builtin.
            if (GetLightTypeAndShape() != HDLightTypeAndShape.DiscArea)
                legacyLight.areaSize = new Vector2(shapeWidth, shapeHeight);
#endif
        }

        /// <summary>
        /// Synchronize all the HD Additional Light values with the Light component.
        /// </summary>
        public void UpdateAllLightValues()
        {
            UpdateAllLightValues(false);
        }

        internal void UpdateAllLightValues(bool fromTimeLine)
        {
            UpdateShapeSize();

            // Update light intensity
            UpdateLightIntensity();

            // Patch bounds
            UpdateBounds();

            UpdateAreaLightEmissiveMesh(fromTimeLine: fromTimeLine);
        }

        internal void RefreshCachedShadow()
        {
            bool wentThroughCachedShadowSystem = lightIdxForCachedShadows >= 0;
            if (wentThroughCachedShadowSystem)
                HDShadowManager.cachedShadowManager.EvictLight(this);

            if (!ShadowIsUpdatedEveryFrame() && legacyLight.shadows != LightShadows.None)
            {
                HDShadowManager.cachedShadowManager.RegisterLight(this);
            }
        }

        #endregion

#region User API functions

        /// <summary>
        /// Set the color of the light.
        /// </summary>
        /// <param name="color">Color</param>
        /// <param name="colorTemperature">Optional color temperature</param>
        public void SetColor(Color color, float colorTemperature = -1)
        {
            if (colorTemperature != -1)
            {
                legacyLight.colorTemperature = colorTemperature;
                useColorTemperature = true;
            }

            this.color = color;
        }

        /// <summary>
        /// Toggle the usage of color temperature.
        /// </summary>
        /// <param name="enable"></param>
        public void EnableColorTemperature(bool enable)
        {
            useColorTemperature = enable;
        }

        /// <summary>
        /// Set the intensity of the light using the current unit.
        /// </summary>
        /// <param name="intensity"></param>
        public void SetIntensity(float intensity) => this.intensity = intensity;

        /// <summary>
        /// Set the intensity of the light using unit in parameter.
        /// </summary>
        /// <param name="intensity"></param>
        /// <param name="unit">Unit must be a valid Light Unit for the current light type</param>
        public void SetIntensity(float intensity, LightUnit unit)
        {
            this.lightUnit = unit;
            this.intensity = intensity;
        }

        /// <summary>
        /// For Spot Lights only, set the intensity that the spot should emit at a certain distance in meter
        /// </summary>
        /// <param name="luxIntensity"></param>
        /// <param name="distance"></param>
        public void SetSpotLightLuxAt(float luxIntensity, float distance)
        {
            lightUnit = LightUnit.Lux;
            luxAtDistance = distance;
            intensity = luxIntensity;
        }

        /// <summary>
        /// Set light cookie. Note that the texture must have a power of two size.
        /// </summary>
        /// <param name="cookie">Cookie texture, must be 2D for Directional, Spot and Area light and Cubemap for Point lights</param>
        /// <param name="directionalLightCookieSize">area light </param>
        public void SetCookie(Texture cookie, Vector2 directionalLightCookieSize)
        {
            HDLightType lightType = type;
            if (lightType == HDLightType.Area)
            {
                if (cookie.dimension != TextureDimension.Tex2D)
                {
                    Debug.LogError("Texture dimension " + cookie.dimension + " is not supported for area lights.");
                    return ;
                }
                areaLightCookie = cookie;
            }
            else
            {
                if (lightType == HDLightType.Point && cookie.dimension != TextureDimension.Cube)
                {
                    Debug.LogError("Texture dimension " + cookie.dimension + " is not supported for point lights.");
                    return ;
                }
                else if ((lightType == HDLightType.Directional || lightType == HDLightType.Spot) && cookie.dimension != TextureDimension.Tex2D) // Only 2D cookie are supported for Directional and Spot lights
                {
                    Debug.LogError("Texture dimension " + cookie.dimension + " is not supported for Directional/Spot lights.");
                    return ;
                }
                if (lightType == HDLightType.Directional)
                {
                    shapeWidth = directionalLightCookieSize.x;
                    shapeHeight = directionalLightCookieSize.y;
                }
                legacyLight.cookie = cookie;
            }
        }

        /// <summary>
        /// Set light cookie.
        /// </summary>
        /// <param name="cookie">Cookie texture, must be 2D for Directional, Spot and Area light and Cubemap for Point lights</param>
        public void SetCookie(Texture cookie) => SetCookie(cookie, Vector2.zero);

        /// <summary>
        /// Set the spot light angle and inner spot percent. We don't use Light.innerSpotAngle.
        /// </summary>
        /// <param name="angle">inner spot angle in degree</param>
        /// <param name="innerSpotPercent">inner spot angle in percent</param>
        public void SetSpotAngle(float angle, float innerSpotPercent = 0)
        {
            this.legacyLight.spotAngle = angle;
            this.innerSpotPercent = innerSpotPercent;
        }

        /// <summary>
        /// Set the dimmer for light and volumetric light.
        /// </summary>
        /// <param name="dimmer">Dimmer for the light</param>
        /// <param name="volumetricDimmer">Dimmer for the volumetrics</param>
        public void SetLightDimmer(float dimmer = 1, float volumetricDimmer = 1)
        {
            this.lightDimmer = dimmer;
            this.volumetricDimmer = volumetricDimmer;
        }

        /// <summary>
        /// Set the light unit.
        /// </summary>
        /// <param name="unit">Unit of the light</param>
        public void SetLightUnit(LightUnit unit) => lightUnit = unit;

        /// <summary>
        /// Enable shadows on a light.
        /// </summary>
        /// <param name="enabled"></param>
        public void EnableShadows(bool enabled) => legacyLight.shadows = enabled ? LightShadows.Soft : LightShadows.None;

        /// <summary>
        /// Set the shadow resolution.
        /// </summary>
        /// <param name="resolution">Must be between 16 and 16384</param>
        public void SetShadowResolution(int resolution)
        {
            if (shadowResolution.@override != resolution)
            {
                shadowResolution.@override = resolution;
                RefreshCachedShadow();
            }
        }

        /// <summary>
        /// Set the shadow resolution quality level.
        /// </summary>
        /// <param name="level">The quality level to use</param>
        public void SetShadowResolutionLevel(int level)
        {
            if (shadowResolution.level != level)
            {
                shadowResolution.level = level;
                RefreshCachedShadow();
            }
        }

        /// <summary>
        /// Set whether the shadow resolution use the override value.
        /// </summary>
        /// <param name="useOverride">True to use the override value, false otherwise.</param>
        public void SetShadowResolutionOverride(bool useOverride)
        {
            if (shadowResolution.useOverride != useOverride)
            {
                shadowResolution.useOverride = useOverride;
                RefreshCachedShadow();
            }
        }

        /// <summary>
        /// Set the near plane of the shadow.
        /// </summary>
        /// <param name="nearPlaneDistance"></param>
        public void SetShadowNearPlane(float nearPlaneDistance) => shadowNearPlane = nearPlaneDistance;

        /// <summary>
        /// Set parameters for PCSS shadows.
        /// </summary>
        /// <param name="blockerSampleCount">Number of samples used to detect blockers</param>
        /// <param name="filterSampleCount">Number of samples used to filter the shadow map</param>
        /// <param name="minFilterSize">Minimum filter intensity</param>
        /// <param name="radiusScaleForSoftness">Scale applied to shape radius or angular diameter in the softness calculations.</param>
        public void SetPCSSParams(int blockerSampleCount = 16, int filterSampleCount = 24, float minFilterSize = 0.01f, float radiusScaleForSoftness = 1)
        {
            this.blockerSampleCount = blockerSampleCount;
            this.filterSampleCount = filterSampleCount;
            this.minFilterSize = minFilterSize;
            this.softnessScale = radiusScaleForSoftness;
        }

        /// <summary>
        /// Set the light layer and shadow map light layer masks. The feature must be enabled in the HDRP asset in norder to work.
        /// </summary>
        /// <param name="lightLayerMask">Layer mask for receiving light</param>
        /// <param name="shadowLayerMask">Layer mask for shadow rendering</param>
        public void SetLightLayer(LightLayerEnum lightLayerMask, LightLayerEnum shadowLayerMask)
        {
            // disable the shadow / light layer link
            linkShadowLayers = false;
            legacyLight.renderingLayerMask = LightLayerToRenderingLayerMask((int)shadowLayerMask, (int)legacyLight.renderingLayerMask);
            lightlayersMask = lightLayerMask;
        }

        /// <summary>
        /// Set the shadow dimmer.
        /// </summary>
        /// <param name="shadowDimmer">Dimmer between 0 and 1</param>
        /// <param name="volumetricShadowDimmer">Dimmer between 0 and 1 for volumetrics</param>
        public void SetShadowDimmer(float shadowDimmer = 1, float volumetricShadowDimmer = 1)
        {
            this.shadowDimmer = shadowDimmer;
            this.volumetricShadowDimmer = volumetricShadowDimmer;
        }

        /// <summary>
        /// Shadow fade distance in meter.
        /// </summary>
        /// <param name="distance"></param>
        public void SetShadowFadeDistance(float distance) => shadowFadeDistance = distance;

        /// <summary>
        /// Set the Shadow tint for the directional light.
        /// </summary>
        /// <param name="tint"></param>
        public void SetDirectionalShadowTint(Color tint) => shadowTint = tint;

        /// <summary>
        /// Set the shadow update mode.
        /// </summary>
        /// <param name="updateMode"></param>
        public void SetShadowUpdateMode(ShadowUpdateMode updateMode) => shadowUpdateMode = updateMode;

        // A bunch of function that changes stuff on the legacy light so users don't have to get the
        // light component which would lead to synchronization problem with ou HD datas.

        /// <summary>
        /// Set the range of the light.
        /// </summary>
        /// <param name="range"></param>
        public void SetRange(float range) => legacyLight.range = range;

        /// <summary>
        /// Set the shadow map light layer masks. The feature must be enabled in the HDRP asset in norder to work.
        /// </summary>
        /// <param name="shadowLayerMask"></param>
        public void SetShadowLightLayer(LightLayerEnum shadowLayerMask) => legacyLight.renderingLayerMask = LightLayerToRenderingLayerMask((int)shadowLayerMask, (int)legacyLight.renderingLayerMask);

        /// <summary>
        /// Set the light culling mask.
        /// </summary>
        /// <param name="cullingMask"></param>
        public void SetCullingMask(int cullingMask) => legacyLight.cullingMask = cullingMask;

        /// <summary>
        /// Set the light layer shadow cull distances.
        /// </summary>
        /// <param name="layerShadowCullDistances"></param>
        /// <returns></returns>
        public float[] SetLayerShadowCullDistances(float[] layerShadowCullDistances) => legacyLight.layerShadowCullDistances = layerShadowCullDistances;

        /// <summary>
        /// Get the list of supported light units depending on the current light type.
        /// </summary>
        /// <returns></returns>
        public LightUnit[] GetSupportedLightUnits() => GetSupportedLightUnits(type, m_SpotLightShape);

        /// <summary>
        /// Set the area light size.
        /// </summary>
        /// <param name="size"></param>
        public void SetAreaLightSize(Vector2 size)
        {
            if (type == HDLightType.Area)
            {
                m_ShapeWidth = size.x;
                m_ShapeHeight = size.y;
                UpdateAllLightValues();
            }
        }

        /// <summary>
        /// Set the box spot light size.
        /// </summary>
        /// <param name="size"></param>
        public void SetBoxSpotSize(Vector2 size)
        {
            if (type == HDLightType.Spot)
            {
                shapeWidth = size.x;
                shapeHeight = size.y;
            }
        }

#if UNITY_EDITOR
        /// <summary> [Editor Only] Set the lightmap bake type. </summary>
        public LightmapBakeType lightmapBakeType
        {
            get => legacyLight.lightmapBakeType;
            set => legacyLight.lightmapBakeType = value;
        }
#endif

#endregion

        /// <summary>
        /// Converts a light layer into a rendering layer mask.
        ///
        /// Light layer is stored in the first 8 bit of the rendering layer mask.
        ///
        /// NOTE: light layers are obsolete, use directly renderingLayerMask.
        /// </summary>
        /// <param name="lightLayer">The light layer, only the first 8 bits will be used.</param>
        /// <param name="renderingLayerMask">Current renderingLayerMask, only the last 24 bits will be used.</param>
        /// <returns></returns>
        internal static int LightLayerToRenderingLayerMask(int lightLayer, int renderingLayerMask)
        {
            var renderingLayerMask_u32 = (uint)renderingLayerMask;
            var lightLayer_u8 = (byte)lightLayer;
            return (int)((renderingLayerMask_u32 & 0xFFFFFF00) | lightLayer_u8);
        }

        /// <summary>
        /// Converts a renderingLayerMask into a lightLayer.
        ///
        /// NOTE: light layers are obsolete, use directly renderingLayerMask.
        /// </summary>
        /// <param name="renderingLayerMask"></param>
        /// <returns></returns>
        internal static int RenderingLayerMaskToLightLayer(int renderingLayerMask)
            => (byte)renderingLayerMask;

        ShadowMapType shadowMapType
            => (type == HDLightType.Area && areaLightShape == AreaLightShape.Rectangle)
            ? ShadowMapType.AreaLightAtlas
            : type != HDLightType.Directional
                ? ShadowMapType.PunctualAtlas
                : ShadowMapType.CascadedDirectional;

        void OnEnable()
        {
            if (shadowUpdateMode != ShadowUpdateMode.EveryFrame && legacyLight.shadows != LightShadows.None)
            {
                HDShadowManager.cachedShadowManager.RegisterLight(this);
            }

            SetEmissiveMeshRendererEnabled(true);
        }

        /// <summary>
        /// Deserialization callback
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize() { }

        /// <summary>
        /// Serialization callback
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // When reseting, Light component can be not available (will be called later in Reset)
            if (m_Light == null || m_Light.Equals(null))
                return;

            UpdateBounds();
        }

        void Reset()
            => UpdateBounds();

        // This is faster than the above property if lightType is known given that type does a non-trivial amount of work.
        internal ShadowMapType GetShadowMapType(HDLightType lightType)
        {
            return (lightType == HDLightType.Area && areaLightShape == AreaLightShape.Rectangle) ? ShadowMapType.AreaLightAtlas
                : lightType != HDLightType.Directional
                    ? ShadowMapType.PunctualAtlas
                    : ShadowMapType.CascadedDirectional;
        }

        /// <summary>Tell if the light is overlapping for the light overlap debug mode</summary>
        internal bool IsOverlapping()
        {
            var baking = GetComponent<Light>().bakingOutput;
            bool isOcclusionSeparatelyBaked = baking.occlusionMaskChannel != -1;
            bool isDirectUsingBakedOcclusion = baking.mixedLightingMode == MixedLightingMode.Shadowmask || baking.mixedLightingMode == MixedLightingMode.Subtractive;
            return isDirectUsingBakedOcclusion && !isOcclusionSeparatelyBaked;
        }
    }
}
