using System;
using System.Collections.Generic;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        public bool lightEnabled;
    }

    //@TODO: We should continuously move these values
    // into the engine when we can see them being generally useful
    /// <summary>
    /// HDRP Additional light data component. It contains the light API and fields used by HDRP.
    /// </summary>
    [HDRPHelpURLAttribute("Light-Component")]
    [AddComponentMenu("")] // Hide in menu
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [ExecuteAlways]
    public partial class HDAdditionalLightData : MonoBehaviour, ISerializationCallbackReceiver, IAdditionalData
    {
        internal const float k_MinLightSize = 0.01f; // Provide a small size of 1cm for line light

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
        /// Light source used to shade the celestial body.
        /// </summary>
        public enum CelestialBodyShadingSource
        {
            /// <summary>
            /// The celestial body will emit light.
            /// </summary>
            Emission = 1,
            /// <summary>
            /// The celestial body will reflect light from a directional light in the scene.
            /// </summary>
            ReflectSunLight = 0,
            /// <summary>
            /// The celestial body will be illuminated by an artifical light source.
            /// </summary>
            Manual = 2,
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

        [ExcludeCopy]
        internal HDLightRenderEntity lightEntity = HDLightRenderEntity.Invalid;

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

                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).innerSpotPercent = m_InnerSpotPercent;
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

                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).spotIESCutoffPercent = m_SpotIESCutoffPercent;
            }
        }

        /// <summary>
        /// Get the inner spot radius between 0 and 1.
        /// </summary>
        public float spotIESCutoffPercent01 => spotIESCutoffPercent / 100f;

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

                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).lightDimmer = m_LightDimmer;
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

                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).volumetricDimmer = m_VolumetricDimmer;
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

                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).fadeDistance = m_FadeDistance;
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).volumetricFadeDistance = m_VolumetricFadeDistance;
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).affectDiffuse = m_AffectDiffuse;
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).affectSpecular = m_AffectSpecular;
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
                // We need to update the ray traced shadow flag as we don't want ray traced shadows with shadow mask.
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).useRayTracedShadows = m_UseRayTracedShadows && !m_NonLightmappedOnly;

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

                var lightType = legacyLight.type;
                if (lightType.IsArea())
                    m_ShapeWidth = Mathf.Clamp(value, k_MinAreaWidth, float.MaxValue);
                else
                    m_ShapeWidth = Mathf.Clamp(value, 0, float.MaxValue);
                UpdateAllLightValues();
                HDLightRenderDatabase.instance.SetShapeWidth(lightEntity, m_ShapeWidth);
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

                if (legacyLight.type.IsArea())
                    m_ShapeHeight = Mathf.Clamp(value, k_MinAreaWidth, float.MaxValue);
                else
                    m_ShapeHeight = Mathf.Clamp(value, 0, float.MaxValue);
                UpdateAllLightValues();
                HDLightRenderDatabase.instance.SetShapeHeight(lightEntity, m_ShapeHeight);
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
                HDLightRenderDatabase.instance.SetAspectRatio(lightEntity, m_AspectRatio);
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
                HDLightRenderDatabase.instance.SetShapeRadius(lightEntity, m_ShapeRadius);
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).softnessScale = m_SoftnessScale;
                }
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).useCustomSpotLightShadowCone = m_UseCustomSpotLightShadowCone;
                }
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).customSpotLightShadowCone = m_CustomSpotLightShadowCone;
                }
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).applyRangeAttenuation = m_ApplyRangeAttenuation;
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
        /// IES texture for Point lights.
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
        /// IES texture for Spot or Rectangular lights.
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

        /// <summary>
        /// IES texture for Point, Spot or Rectangular lights.
        /// For Point lights, this must be a cubemap.
        /// For Spot or Rectangle lights, this must be a 2D texture
        /// </summary>
        public Texture IESTexture
        {
            get
            {
                var lightType = legacyLight.type;
                if (lightType == LightType.Point)
                    return IESPoint;
                else if (lightType.IsSpot() || lightType == LightType.Rectangle)
                    return IESSpot;
                return null;
            }
            set
            {
                var lightType = legacyLight.type;
                if (lightType == LightType.Point)
                    IESPoint = value;
                else if (lightType.IsSpot() || lightType == LightType.Rectangle)
                    IESSpot = value;
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

                m_IncludeForRayTracing = value;

                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).includeForRayTracing = m_IncludeForRayTracing;
                UpdateAllLightValues();
            }
        }


        [SerializeField]
        bool m_IncludeForPathTracing = true;
        /// <summary>
        /// Controls if the light is enabled when the camera has Path Tracing enabled.
        /// </summary>
        public bool includeForPathTracing
        {
            get => m_IncludeForPathTracing;
            set
            {
                if (m_IncludeForPathTracing == value)
                    return;

                m_IncludeForPathTracing = value;

                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).includeForPathTracing = m_IncludeForPathTracing;
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).areaLightShadowCone = m_AreaLightShadowCone;
                }
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).useScreenSpaceShadows = m_UseScreenSpaceShadows;
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
            // m_InteractWithSky can be true if user changed from directional to point light, so we need to check current type
            get => m_InteractsWithSky && legacyLight.type == LightType.Directional;
            set
            {
                if (m_InteractsWithSky == value)
                    return;

                m_InteractsWithSky = value;
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).interactsWithSky = m_InteractsWithSky;
            }
        }

        [SerializeField, FormerlySerializedAs("angularDiameter")]
        float m_AngularDiameter = 0.5f;
        /// <summary>
        /// Angular diameter of the celestial body represented by the light as seen from the camera (in degrees).
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
                HDLightRenderDatabase.instance.SetAngularDiameter(lightEntity, m_AngularDiameter);
            }
        }

        /// <summary>
        /// Angular diameter mode to use.
        /// </summary>
        [SerializeField, FormerlySerializedAs("m_DiameterMultiplerMode")]
        public bool diameterMultiplerMode = false;

        /// <summary>
        /// Multiplier for the angular diameter of the celestial body used only when rendering the sun disk.
        /// </summary>
        [SerializeField, Min(0.0f), FormerlySerializedAs("m_DiameterMultiplier")]
        public float diameterMultiplier = 1.0f;

        /// <summary>
        /// Override for the angular diameter of the celestial body used only when rendering the sun disk.
        /// </summary>Mode
        [SerializeField, Min(0.0f), FormerlySerializedAs("m_DiameterOverride")]
        public float diameterOverride = 0.5f;

        /// <summary>
        /// Shading source of the celestial body.
        /// </summary>
        [SerializeField, FormerlySerializedAs("m_EmissiveLightSource")]
        public CelestialBodyShadingSource celestialBodyShadingSource = CelestialBodyShadingSource.Emission;

        /// <summary>
        /// The Directional light that should illuminate this celestial body.
        /// </summary>
        [SerializeField]
        public Light sunLightOverride;

        /// <summary>
        /// Color of the light source.
        /// </summary>
        [SerializeField]
        internal Color sunColor = Color.white;

        /// <summary>
        /// Intensity of the light source in Lux.
        /// </summary>
        [SerializeField, Min(0.0f)]
        internal float sunIntensity = 130000.0f;

        /// <summary>
        /// The percentage of moon that receives sunlight.
        /// </summary>
        [SerializeField, Range(0, 1), FormerlySerializedAs("m_MoonPhase")]
        public float moonPhase = 0.2f;

        /// <summary>
        /// The rotation of the moon phase.
        /// </summary>
        [SerializeField, Range(0, 360.0f), FormerlySerializedAs("m_MoonPhaseRotation")]
        public float moonPhaseRotation = 0.0f;

        /// <summary>
        /// The intensity of the sunlight reflected from the planet onto the moon.
        /// </summary>
        [SerializeField, Min(0.0f), FormerlySerializedAs("m_Earthshine")]
        public float earthshine = 1.0f;

        /// <summary>
        /// Size the flare around the celestial body (in degrees).
        /// </summary>
        [SerializeField, Range(0, 90), FormerlySerializedAs("m_FlareSize")]
        public float flareSize = 2.0f;

        /// <summary>
        /// Tints the flare of the celestial body.
        /// </summary>
        [SerializeField, FormerlySerializedAs("m_FlareTint")]
        public Color flareTint = Color.white;

        /// <summary>
        /// The falloff rate of flare intensity as the angle from the light increases.
        /// </summary>
        [SerializeField, Min(0.0f), FormerlySerializedAs("m_FlareFalloff")]
        public float flareFalloff = 4.0f;

        /// <summary>
        /// Intensity of the flare.
        /// </summary>
        [SerializeField, Range(0, 1)]
        public float flareMultiplier = 1.0f;

        /// <summary>
        /// Texture of the surface of the celestial body. Acts like a multiplier.
        /// </summary>
        [SerializeField, FormerlySerializedAs("m_SurfaceTexture")]
        public Texture surfaceTexture = null;

        /// <summary>
        /// Tints the surface of the celestial body.
        /// </summary>
        [SerializeField, FormerlySerializedAs("m_SurfaceTint")]
        public Color surfaceTint = Color.white;

        [SerializeField, Min(0.0f), FormerlySerializedAs("distance")]
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).distance = m_Distance;
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).useRayTracedShadows = m_UseRayTracedShadows;
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).colorShadow = m_ColorShadow;
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).evsmExponent = m_EvsmExponent;
                }
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).evsmLightLeakBias = m_EvsmLightLeakBias;
                }
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).evsmVarianceBias = m_EvsmVarianceBias;
                }
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).evsmBlurPasses = (byte)m_EvsmBlurPasses;
                }
            }
        }

        // Now the renderingLayerMask is used for shadow layers and not light layers
        [SerializeField, FormerlySerializedAs("lightlayersMask")]
        RenderingLayerMask m_LightlayersMask = (RenderingLayerMask) (uint) UnityEngine.RenderingLayerMask.defaultRenderingLayerMask;
        /// <summary>
        /// Controls which layer will be affected by this light
        /// </summary>
        /// <value></value>
        public RenderingLayerMask lightlayersMask
        {
            get => linkShadowLayers ? (RenderingLayerMask)RenderingLayerMaskToLightLayer(legacyLight.renderingLayerMask) : m_LightlayersMask;
            set
            {
                m_LightlayersMask = value;

                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).renderingLayerMask = (uint)m_LightlayersMask;

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
            return value < 0 ? (uint)RenderingLayerMask.Everything : (uint)value;
        }

        /// <summary>
        /// Returns a mask of shadow light layers as uint and handle the case of Everything as being 0xFF and not -1
        /// </summary>
        /// <returns></returns>
        public uint GetShadowLayers()
        {
            int value = RenderingLayerMaskToLightLayer(legacyLight.renderingLayerMask);
            return value < 0 ? (uint)RenderingLayerMask.Everything : (uint)value;
        }

        // Shadow Settings
        [SerializeField, FormerlySerializedAs("shadowNearPlane")]
        float m_ShadowNearPlane = 0.1f;
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

                m_ShadowNearPlane = Mathf.Clamp(value, 0, HDShadowUtils.k_MaxShadowNearPlane);

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).shadowNearPlane = m_ShadowNearPlane;
                }
            }
        }

        // PCSS settings
        [Range(1, 64)]
        [SerializeField, FormerlySerializedAs("blockerSampleCount")]
        int m_BlockerSampleCount = 24;
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).blockerSampleCount = (byte)m_BlockerSampleCount;
                }
            }
        }

        [Range(1, 64)]
        [SerializeField, FormerlySerializedAs("filterSampleCount")]
        int m_FilterSampleCount = 16;
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).filterSampleCount = (byte)m_FilterSampleCount;
                }
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).minFilterSize = m_MinFilterSize;
                }
            }
        }

        [Range(1, 64)]
        [SerializeField] int m_DirLightPCSSBlockerSampleCount = 24;
        /// <summary>
        /// Controls the number of samples used to detect blockers for directional lights PCSS shadows.
        /// </summary>
        // Note: We duplicate this setting so its default value can be different than other light types
        public int dirLightPCSSBlockerSampleCount
        {
            get => m_DirLightPCSSBlockerSampleCount;
            set
            {
                if (m_DirLightPCSSBlockerSampleCount == value)
                    return;

                m_DirLightPCSSBlockerSampleCount = Mathf.Clamp(value, 1, 64);

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).dirLightPCSSBlockerSampleCount = (byte)m_DirLightPCSSBlockerSampleCount;
                }
            }
        }

        [Range(1, 64)]
        [SerializeField] int m_DirLightPCSSFilterSampleCount = 16;
        /// <summary>
        /// Controls the number of samples used to filter for directional lights PCSS shadows.
        /// </summary>
        // Note: We duplicate this setting so its default value can be different than other light types
        public int dirLightPCSSFilterSampleCount
        {
            get => m_DirLightPCSSFilterSampleCount;
            set
            {
                if (m_DirLightPCSSFilterSampleCount == value)
                    return;

                m_DirLightPCSSFilterSampleCount = Mathf.Clamp(value, 1, 64);

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).dirLightPCSSFilterSampleCount = (byte)m_DirLightPCSSFilterSampleCount;
                }
            }
        }

        [SerializeField] float m_DirLightPCSSMaxPenumbraSize = 0.56f; // Default matching previous API max blocker distance at 64m for a light angular diameter of 0.5
        /// <summary>
        /// Maximum penumbra size (in world space), limiting blur filter kernel size
        /// Measured against a receiving surface perpendicular to light direction (penumbra may get wider for different angles)
        /// Very large kernels may affect GPU performance and/or produce undesirable artifacts close to caster
        /// </summary>
        public float dirLightPCSSMaxPenumbraSize
        {
            get => m_DirLightPCSSMaxPenumbraSize;
            set
            {
                m_DirLightPCSSMaxPenumbraSize = Math.Max(value, 0.0f);
                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).dirLightPCSSMaxPenumbraSize = m_DirLightPCSSMaxPenumbraSize;
                }
            }
        }

        [SerializeField] float m_DirLightPCSSMaxSamplingDistance = 0.5f;
        /// <summary>
        /// Maximum distance from the receiver PCSS shadow sampling occurs, this is to avoid light bleeding due to distant
        /// blockers hiding the cone apex and leading to missing occlusion, the lower the least light bleeding but too low will cause self-shadowing
        /// Note that the algorithm will also clamp the sampling distance in function of the blocker distance, to avoid light bleeding with very close blockers
        /// </summary>
        public float dirLightPCSSMaxSamplingDistance
        {
            get => m_DirLightPCSSMaxSamplingDistance;
            set
            {
                m_DirLightPCSSMaxSamplingDistance = Math.Max(value, 0.0f);
                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).dirLightPCSSMaxSamplingDistance = m_DirLightPCSSMaxSamplingDistance;
                }
            }
        }
        [SerializeField] float m_DirLightPCSSMinFilterSizeTexels = 1.5f;
        /// <summary>
        /// Minimum PCSS filter size (in shadowmap texels) to avoid aliasing
        /// </summary>
        public float dirLightPCSSMinFilterSizeTexels
        {
            get => m_DirLightPCSSMinFilterSizeTexels;
            set
            {
                m_DirLightPCSSMinFilterSizeTexels = Math.Max(value, 0.0f);
                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).dirLightPCSSMinFilterSizeTexels = m_DirLightPCSSMinFilterSizeTexels;
                }
            }
        }

        [SerializeField] float m_DirLightPCSSMinFilterMaxAngularDiameter = 10.0f;
        /// <summary>
        /// Maximum angular diameter to use to reach minimum filter size, this makes a wider cone at the apex
        /// So that we quickly reach minimum filter size while avoiding self-shadowing
        /// </summary>
        public float dirLightPCSSMinFilterMaxAngularDiameter
        {
            get => m_DirLightPCSSMinFilterMaxAngularDiameter;
            set
            {
                m_DirLightPCSSMinFilterMaxAngularDiameter = Math.Max(value, 0.0f);
                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).dirLightPCSSMinFilterMaxAngularDiameter = m_DirLightPCSSMinFilterMaxAngularDiameter;
                }
            }
        }

        [SerializeField] float m_DirLightPCSSBlockerSearchAngularDiameter = 12.0f;
        /// <summary>
        /// Angular diameter to use for blocker search, will include blockers outside of the light cone
        /// when greater than m_AngularDiameter to reduce light bleeding.  Increasing this value too much may
        /// result in self-shadowing artifacts.  A value below m_AngularDiameter will get clamped to m_AngularDiameter
        /// </summary>
        public float dirLightPCSSBlockerSearchAngularDiameter
        {
            get => m_DirLightPCSSBlockerSearchAngularDiameter;
            set
            {
                m_DirLightPCSSBlockerSearchAngularDiameter = Math.Max(value, 0.0f);
                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).dirLightPCSSBlockerSearchAngularDiameter = m_DirLightPCSSBlockerSearchAngularDiameter;
                }
            }
        }

        [Range(1, 6)]
        [SerializeField] float m_DirLightPCSSBlockerSamplingClumpExponent = 2.0f;
        /// <summary>
        /// Affects how blocker search samples are distributed.  Samples distance to center is elevated to this power.
        /// A clump exponent of 1 means uniform distribution on the sampling disk.
        /// A clump exponent of 2 means distance from center of the uniform distribution are squared (clumped toward center)
        /// </summary>
        public float dirLightPCSSBlockerSamplingClumpExponent
        {
            get => m_DirLightPCSSBlockerSamplingClumpExponent;
            set
            {
                m_DirLightPCSSBlockerSamplingClumpExponent = Math.Max(value, 0.0f);
                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).dirLightPCSSBlockerSamplingClumpExponent = m_DirLightPCSSBlockerSamplingClumpExponent;
                }
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).kernelSize = (byte)m_KernelSize;
                }
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).lightAngle = m_LightAngle;
                }
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).maxDepthBias = m_MaxDepthBias;
                }
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
        [ValueCopy] //we want separate object with same values
        [SerializeField]
        private IntScalableSettingValue m_ShadowResolution = new IntScalableSettingValue
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).shadowDimmer = m_ShadowDimmer;
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).volumetricShadowDimmer = m_VolumetricShadowDimmer;
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).shadowFadeDistance = m_ShadowFadeDistance;
            }
        }

        [SerializeField]
        [ValueCopy] //we want separate object with same values
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).shadowTint = m_ShadowTint;
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).penumbraTint = m_PenumbraTint;
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).normalBias = value;
                }
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).slopeBias = m_SlopeBias;
                }
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
                m_ShadowUpdateMode = value;

                RegisterCachedShadowLightOptional();
                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).shadowUpdateMode = value;
                }
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
            set
            {
                m_AlwaysDrawDynamicShadows = value;

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).alwaysDrawDynamicShadows = value;
                }
            }
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
                if (m_UpdateShadowOnLightMovement != value)
                {
                    if (m_UpdateShadowOnLightMovement)
                        HDShadowManager.cachedShadowManager.RegisterTransformToCache(this);
                    else
                        HDShadowManager.cachedShadowManager.RegisterTransformToCache(this);

                    m_UpdateShadowOnLightMovement = value;

                    if (lightEntity.valid)
                    {
                        HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).updateUponLightMovement = value;
                    }
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).cachedShadowTranslationUpdateThreshold = value;
                }
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

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).cachedShadowAngleUpdateThreshold = value;
                }
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).barnDoorAngle = m_BarnDoorAngle;
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
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).barnDoorLength = m_BarnDoorLength;
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

        [SerializeField]
        bool m_OnDemandShadowRenderOnPlacement = true;
        /// <summary>
        /// If the shadow update mode is set to OnDemand, this parameter controls whether the shadows are rendered the first time without needing an explicit render request. If this properties is false,
        /// the OnDemand shadows will never be rendered unless a render request is performed explicitly.
        /// </summary>
        public bool onDemandShadowRenderOnPlacement
        {
            get => m_OnDemandShadowRenderOnPlacement;
            set
            {
                if (m_OnDemandShadowRenderOnPlacement == value)
                    return;

                m_OnDemandShadowRenderOnPlacement = value;
            }
        }

        // This is a bit confusing, but it is an override to ignore the onDemandShadowRenderOnPlacement field when a light is registered for the first time as a consequence of a request for shadow update.
        internal bool forceRenderOnPlacement = false;

        /// <summary>
        /// True if the light affects volumetric fog, false otherwise
        /// </summary>
        public bool affectsVolumetric
        {
            get => useVolumetric;
            set
            {
                useVolumetric = value;
                if (lightEntity.valid)
                    HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity).affectVolumetric = useVolumetric;
            }
        }

        #endregion

        #region Internal API for moving shadow datas from AdditionalShadowData to HDAdditionalLightData

        [SerializeField]
        [ValueCopy] //we want separate object with same values
        float[] m_ShadowCascadeRatios = new float[3] { 0.05f, 0.2f, 0.3f };
        internal float[] shadowCascadeRatios
        {
            get => m_ShadowCascadeRatios;
            set => m_ShadowCascadeRatios = value;
        }

        [SerializeField]
        [ValueCopy] //we want separate object with same values
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
#pragma warning restore 0414

        internal unsafe UnsafeList<HDShadowRequest> shadowRequests
        {
            get
            {
                UnsafeList<HDShadowRequest> retValue = default;

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase lightRenderDatabase = HDLightRenderDatabase.instance;
                    HDShadowRequestDatabase shadowRequestDatabase = HDShadowRequestDatabase.instance;
                    NativeList<HDShadowRequest> hdShadowRequestStorage = shadowRequestDatabase.hdShadowRequestStorage;
                    int dataStartIndex = lightRenderDatabase.GetShadowRequestSetHandle(lightEntity).storageIndexForShadowRequests;
                    Assert.IsTrue(dataStartIndex >= 0 && dataStartIndex < hdShadowRequestStorage.Length);
                    UnsafeList<HDShadowRequest>* unsafeListPtr = hdShadowRequestStorage.GetUnsafeList();
                    retValue = new UnsafeList<HDShadowRequest>(unsafeListPtr->Ptr + dataStartIndex, HDShadowRequest.maxLightShadowRequestsCount);
                }
                return retValue;
            }
        }

        unsafe UnsafeList<int> shadowRequestIndices
        {
            get
            {
                UnsafeList<int> retValue = default;

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase lightRenderDatabase = HDLightRenderDatabase.instance;
                    HDShadowRequestDatabase shadowRequestDatabase = HDShadowRequestDatabase.instance;
                    NativeList<int> hdShadowIndicesStorage = shadowRequestDatabase.hdShadowRequestIndicesStorage;
                    int dataStartIndex = lightRenderDatabase.GetShadowRequestSetHandle(lightEntity).storageIndexForRequestIndices;
                    Assert.IsTrue(dataStartIndex >= 0 && dataStartIndex < hdShadowIndicesStorage.Length);
                    UnsafeList<int>* unsafeListPtr = hdShadowIndicesStorage.GetUnsafeList();
                    retValue = new UnsafeList<int>(unsafeListPtr->Ptr + dataStartIndex, HDShadowRequest.maxLightShadowRequestsCount);
                }
                return retValue;
            }
        }

        // Data for cached shadow maps
        [System.NonSerialized, ExcludeCopy]
        int m_LightIdxForCachedShadows = -1;

        internal int lightIdxForCachedShadows
        {
            get => m_LightIdxForCachedShadows;
            set
            {
                m_LightIdxForCachedShadows = value;

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).lightIdxForCachedShadows = value;
                }
            }
        }

        internal bool hasShadowCache { get { return lightIdxForCachedShadows != -1; } }

        unsafe Vector3* m_CachedViewPositions
        {
            get
            {
                Vector3* ptr = null;

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase lightRenderDatabase = HDLightRenderDatabase.instance;
                    HDShadowRequestDatabase shadowRequestDatabase = HDShadowRequestDatabase.instance;
                    NativeList<Vector3> cachedViewPositionsStorage = shadowRequestDatabase.cachedViewPositionsStorage;
                    int dataStartIndex = lightRenderDatabase.GetShadowRequestSetHandle(lightEntity).storageIndexForCachedViewPositions;
                    Assert.IsTrue(dataStartIndex >= 0 && dataStartIndex < cachedViewPositionsStorage.Length);
                    UnsafeList<Vector3>* unsafeListPtr = cachedViewPositionsStorage.GetUnsafeList();
                    ptr = unsafeListPtr->Ptr + dataStartIndex;
                }
                return ptr;
            }
        }

        // Temporary matrix that stores the previous light data (mainly used to discard history for ray traced screen space shadows)
        [System.NonSerialized, ExcludeCopy]
        internal Matrix4x4 previousTransform = Matrix4x4.identity;
        // Temporary index that stores the current shadow index for the light
        [System.NonSerialized, ExcludeCopy]
        internal int shadowIndex = -1;
        // Temporary information if the shadow was cached
        [System.NonSerialized, ExcludeCopy]
        internal bool wasReallyVisibleLastFrame = true;

        [System.NonSerialized, ExcludeCopy]
        internal bool fallbackToCachedShadows = false;

        // Runtime datas used to compute light intensity
        [ExcludeCopy]
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

        [ExcludeCopy]
        private LightType? cachedLightType;

        const string k_EmissiveMeshViewerName = "EmissiveMeshViewer";

        [ExcludeCopy]
        GameObject m_ChildEmissiveMeshViewer;
        [ExcludeCopy]
        internal MeshFilter m_EmissiveMeshFilter;

        [field: ExcludeCopy]
        internal MeshRenderer emissiveMeshRenderer { get; private set; }

#if UNITY_EDITOR
        [ExcludeCopy]
        bool m_NeedsPrefabInstanceCheck = false;
        [ExcludeCopy]
        bool needRefreshPrefabInstanceEmissiveMeshes = false;
#endif
        [ExcludeCopy]
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

        /// <summary> A callback allowing the creation of a new Matrix4x4 based on the lightLocalToWorld matrix </summary>
        public delegate Matrix4x4 CustomViewCallback(Matrix4x4 lightLocalToWorldMatrix);

        CustomViewCallback m_CustomViewCallbackEvent;

        /// <summary> Change the View matrix for Spot Light </summary>
        public CustomViewCallback CustomViewCallbackEvent
        {
            get { return m_CustomViewCallbackEvent; }
            set
            {
                m_CustomViewCallbackEvent = value;

                if (lightEntity.valid)
                {
                    HDLightRenderDatabase.instance.SetCustomCallback(lightEntity, value);
                }
            }
        }

        void OnDestroy()
        {
            if (lightIdxForCachedShadows >= 0) // If it is within the cached system we need to evict it.
                HDShadowManager.cachedShadowManager.EvictLight(this, legacyLight.type);

            DestroyHDLightRenderEntity();
        }

        internal void DestroyHDLightRenderEntity()
        {
            if (!lightEntity.valid)
                return;

            HDLightRenderDatabase.instance.DestroyEntity(lightEntity);
            lightEntity = HDLightRenderEntity.Invalid;
        }

        void OnDisable()
        {
            // If it is within the cached system we need to evict it, unless user explicitly requires not to.
            // If the shadow was pending placement in the atlas, we also evict it, even if the user wants to preserve it.
            if ((!preserveCachedShadow || HDShadowManager.cachedShadowManager.LightIsPendingPlacement(lightIdxForCachedShadows, shadowMapType)) && hasShadowCache)
            {
                HDShadowManager.cachedShadowManager.EvictLight(this, legacyLight.type);
            }

            SetEmissiveMeshRendererEnabled(false);
            s_overlappingHDLights.Remove(this);
            DestroyHDLightRenderEntity();
        }

        void SetEmissiveMeshRendererEnabled(bool enabled)
        {
            if (displayAreaLightEmissiveMesh && emissiveMeshRenderer)
            {
                emissiveMeshRenderer.enabled = enabled;
            }
        }

        internal static int GetShadowRequestCount(int shadowSettingsCascadeShadowSplitCount, LightType lightType)
        {
            return lightType == LightType.Point
                ? 6
                : lightType == LightType.Directional
                    ? shadowSettingsCascadeShadowSplitCount
                    : 1;
        }

        /// <summary>
        /// Request shadow map rendering when Update Mode is set to On Demand.
        /// </summary>
        public void RequestShadowMapRendering()
        {
            if (shadowUpdateMode == ShadowUpdateMode.OnDemand)
            {
                HDShadowManager.cachedShadowManager.ScheduleShadowUpdate(this);
            }
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
            {
                HDShadowManager.cachedShadowManager.ScheduleShadowUpdate(this, shadowIndex);
            }
        }

        internal bool ShadowIsUpdatedEveryFrame()
        {
            return shadowUpdateMode == ShadowUpdateMode.EveryFrame;
        }

        // TODO: This is used to avoid compilation errors due to unreachable code
        static bool s_EnableFallbackToCachedShadows = false;

        internal void RegisterCachedShadowLightOptional()
        {
            // TODO Enable fall back to cached shadows for relevant systems
            fallbackToCachedShadows = (shadowUpdateMode == ShadowUpdateMode.EveryFrame)
                && (legacyLight.type != LightType.Directional)
                && s_EnableFallbackToCachedShadows;

            bool wantsShadowCache = shadowUpdateMode != ShadowUpdateMode.EveryFrame || fallbackToCachedShadows;

            wantsShadowCache = wantsShadowCache && (legacyLight.shadows != LightShadows.None);

            if (!wantsShadowCache && hasShadowCache && !preserveCachedShadow)
            {
                HDShadowManager.cachedShadowManager.EvictLight(this, this.legacyLight.type);
            }

            bool onDemand = shadowUpdateMode == ShadowUpdateMode.OnDemand && !onDemandShadowRenderOnPlacement;

            if (wantsShadowCache && !hasShadowCache && !onDemand && lightEntity.valid)
            {
                HDShadowManager.cachedShadowManager.RegisterLight(this);
            }
        }

        internal ShadowMapUpdateType GetShadowUpdateType(LightType lightType)
        {
            return GetShadowUpdateType(lightType, shadowUpdateMode, alwaysDrawDynamicShadows, HDCachedShadowManager.instance.DirectionalHasCachedAtlas());
        }

        internal static ShadowMapUpdateType GetShadowUpdateType(LightType lightType, ShadowUpdateMode shadowUpdateMode, bool alwaysDrawDynamicShadows, bool directionalHasCachedAtlas)
        {
            if (shadowUpdateMode == ShadowUpdateMode.EveryFrame) return ShadowMapUpdateType.Dynamic;
#if UNITY_2021_1_OR_NEWER
            if (alwaysDrawDynamicShadows)
            {
                if (lightType == LightType.Directional)
                {
                    if (directionalHasCachedAtlas) return ShadowMapUpdateType.Mixed;
                }
                else
                {
                    return ShadowMapUpdateType.Mixed;
                }
            }
#endif
            return ShadowMapUpdateType.Cached;
        }

        internal int GetResolutionFromSettings(ShadowMapType shadowMapType, HDShadowInitParameters initParameters, bool cachedResolution = false)
        {
            if (cachedResolution && fallbackToCachedShadows)
            {
                return HDShadowManager.k_OffscreenShadowMapResolution;
            }

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

        internal int GetResolutionFromSettings(LightType lightType, HDShadowInitParameters initParameters)
        {
            return GetResolutionFromSettings(GetShadowMapType(lightType), initParameters);
        }

        internal void ReserveShadowMap(Camera camera, HDShadowManager shadowManager, HDShadowSettings shadowSettings, in HDShadowInitParameters initParameters, in VisibleLight visibleLight, LightType lightType, bool forcedVisible)
        {
            HDLightRenderDatabase renderDatabase = HDLightRenderDatabase.instance;

            // Create shadow requests array using the light type
            if (!renderDatabase.GetShadowRequestSetHandle(lightEntity).valid)
            {
                renderDatabase.AllocateHDShadowRequests(lightEntity);
            }

            ShadowMapType shadowType = GetShadowMapType(lightType);

            // Reserve wanted resolution in the shadow atlas
            int resolution = GetResolutionFromSettings(shadowType, initParameters);

            //Exit out early if we dont want to render the shadow anyways
            if (resolution == 0)
                return;

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
            if (lightType == LightType.Directional)
                shadowManager.UpdateDirectionalShadowResolution((int)viewportSize.x, shadowSettings.cascadeShadowSplitCount.value);

            if (shadowIsInCacheSystem)
                viewportSize = new Vector2(resolution, resolution);

            int count = GetShadowRequestCount(shadowSettings.cascadeShadowSplitCount.value, lightType);
            var updateType = GetShadowUpdateType(lightType);
            bool hasCachedComponent = !ShadowIsUpdatedEveryFrame();

            if (forcedVisible && !shadowIsInCacheSystem)
            {
                // Limit resolution for offscreen lights with dynamic shadowmap
                viewportSize = Vector2.Min(viewportSize, new Vector2(HDShadowManager.k_OffscreenShadowMapResolution, HDShadowManager.k_OffscreenShadowMapResolution));
                if (lightEntity.valid)
                {
                    // Change this in the database
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).shadowUpdateMode = ShadowUpdateMode.OnDemand;
                }
                HDShadowManager.cachedShadowManager.ScheduleShadowUpdate(this);
                updateType = ShadowMapUpdateType.Cached;
                wasReallyVisibleLastFrame = false;
            }
            else
            {
                if (lightEntity.valid)
                {
                    // Change this in the database
                    HDLightRenderDatabase.instance.EditAdditionalLightUpdateDataAsRef(lightEntity).shadowUpdateMode = m_ShadowUpdateMode;
                }
                wasReallyVisibleLastFrame = true;
            }
            var requestIndices = shadowRequestIndices;
            for (int index = 0; index < count; index++)
            {
                requestIndices[index] = shadowManager.ReserveShadowResolutions(viewportSize, shadowMapType, GetInstanceID(), index, updateType);
            }
        }

        internal bool HasShadowAtlasPlacement()
        {
            // If we force evicted the light, it will have lightIdxForCachedShadows == -1
            return !HDShadowManager.cachedShadowManager.LightIsPendingPlacement(lightIdxForCachedShadows, shadowMapType) && (lightIdxForCachedShadows != -1);
        }

        internal void OverrideShadowResolutionRequestsWithShadowCache(HDShadowManager shadowManager, HDShadowSettings shadowSettings, LightType lightType)
        {
            int shadowRequestCount = GetShadowRequestCount(shadowSettings.cascadeShadowSplitCount.value, lightType);

            for (int i = 0; i < shadowRequestCount; i++)
            {
                int shadowRequestIndex = shadowRequestIndices[i];
                if (shadowRequestIndex < 0 || shadowRequestIndex >= shadowManager.shadowResolutionRequestStorage.Length)
                    continue;

                ref HDShadowResolutionRequest resolutionRequest = ref shadowManager.shadowResolutionRequestStorage.ElementAt(shadowRequestIndex);
                int cachedShadowID = lightIdxForCachedShadows + i;
                HDShadowManager.cachedShadowManager.OverrideShadowResolutionRequestWithCachedData(ref resolutionRequest, cachedShadowID, shadowMapType);
            }
        }

        // This offset shift the position of the spotlight used to approximate the area light shadows. The offset is the minimum such that the full
        // area light shape is included in the cone spanned by the spot light.
        internal static float GetAreaLightOffsetForShadows(Vector2 shapeSize, float coneAngle)
        {
            float halfMinSize = Mathf.Min(shapeSize.x, shapeSize.y) * 0.5f;
            float halfAngle = coneAngle * 0.5f;
            float cotanHalfAngle = 1.0f / Mathf.Tan(halfAngle * Mathf.Deg2Rad);
            float offset = halfMinSize * cotanHalfAngle;

            return -offset;
        }

        // We need these old states to make timeline and the animator record the intensity value and the emissive mesh changes
        [System.NonSerialized]
        TimelineWorkaround timelineWorkaround = new TimelineWorkaround();

#if UNITY_EDITOR

        // Force to retrieve color light's m_UseColorTemperature because it's private
        [System.NonSerialized, ExcludeCopy]
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

        [System.NonSerialized, ExcludeCopy]
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

        internal Color EvaluateLightColor()
        {
            Color finalColor = legacyLight.color.linear * legacyLight.intensity;

            if (legacyLight.useColorTemperature)
                finalColor *= Mathf.CorrelatedColorTemperatureToRGB(legacyLight.colorTemperature);

            return finalColor;
        }

        // TODO: we might be able to get rid to that
        [System.NonSerialized, ExcludeCopy]
        bool m_Animated;

        private void Start()
        {
            // If there is an animator attached ot the light, we assume that some of the light properties
            // might be driven by this animator (using timeline or animations) so we force the LateUpdate
            // to sync the animated HDAdditionalLightData properties with the light component.
            m_Animated = GetComponent<Animator>() != null;
        }

        // TODO: There are a lot of old != current checks and assignation in this function, maybe think about using another system ?
        internal static void TickLateUpdate()
        {
            // Prevent any unwanted sync when not in HDRP (case 1217575)
            if (HDRenderPipeline.currentPipeline == null)
                return;

            DynamicArray<HDAdditionalLightData> allAdditionalLightDatas = HDLightRenderDatabase.instance.hdAdditionalLightData;

            int additionalLightCount = HDLightRenderDatabase.instance.lightCount;

            for (int i = 0; i < additionalLightCount; i++)
            {
                HDAdditionalLightData lightData = allAdditionalLightDatas[i];

                // HDRP manually subcribes to the player loop callback and calls the tick function. This can trigger an
                // edge case during async scene loads where the component is initialized, but the parent GameObject is
                // not. In this case, we simply skip the tick logic. This is in a try block because there's no other
                // way. Simply accessing the .gameObject member calls a getter that will throw a null ref exception in
                // this invalid state.
                try
                {
                    var go = lightData.gameObject;
                }
                catch (Exception)
                {
                    continue;
                }

                if (lightData.cachedLightType != lightData.legacyLight.type)
                {
                    // ^ The light type has changed since the last tick.
                    if (lightData.m_ShadowUpdateMode != ShadowUpdateMode.EveryFrame && lightData.cachedLightType.HasValue)
                    {
                        HDShadowManager.cachedShadowManager.EvictLight(lightData, lightData.cachedLightType.Value);
                    }

                    var directionalLights = HDLightRenderDatabase.instance.directionalLights;
                    if (lightData.cachedLightType == LightType.Directional)
                    {
                        directionalLights.Add(lightData);
                    }
                    else if (lightData.legacyLight.type != LightType.Directional)
                    {
                        // Remove the light from directionalLights, We use a loop to avoid a GC allocation (UUM-69806)
                        for (int k = 0; k < directionalLights.Count; ++k)
                        {
                            if (ReferenceEquals(directionalLights[k], lightData))
                            {
                                directionalLights.RemoveAt(k);
                                break;
                            }
                        }
                    }

#if UNITY_EDITOR
                    switch (lightData.legacyLight.type)
                    {
                        case LightType.Disc:
                            lightData.legacyLight.lightmapBakeType = LightmapBakeType.Baked;
                            break;
                        case LightType.Tube:
                            lightData.legacyLight.lightmapBakeType = LightmapBakeType.Realtime;
                            break;
                    }
#endif

                    lightData.cachedLightType = lightData.legacyLight.type;

                    lightData.RegisterCachedShadowLightOptional();
                }

                bool forceShadowCulling = false;
                // TODO enable for relevant systems
                if (s_EnableFallbackToCachedShadows)
                {
                    // Only force lights that will fall back to cached shadows
                    forceShadowCulling = lightData.fallbackToCachedShadows;

                    // If we have something in the cache, there is no need to force culling
                    // if the light is visible again it will be culled, until then we use
                    // the cached shadow map
                    forceShadowCulling &= lightData.wasReallyVisibleLastFrame;
                }
                lightData.legacyLight.forceVisible = forceShadowCulling;

                // TODO: The rest of this loop only handles animation. Iterate over a separate list in builds,
                // containing only lights with Animator components.

                // We force the animation in the editor and in play mode when there is an animator component attached to the light
#if !UNITY_EDITOR
                if (!lightData.m_Animated)
                    continue;
#endif

#if UNITY_EDITOR

                // If modification are due to change on prefab asset that are non overridden on this prefab instance
                if (lightData.m_NeedsPrefabInstanceCheck && PrefabUtility.IsPartOfPrefabInstance(lightData) && ((PrefabUtility.GetCorrespondingObjectFromOriginalSource(lightData) as HDAdditionalLightData)?.needRefreshPrefabInstanceEmissiveMeshes ?? false))
                {
                    lightData.needRefreshPrefabInstanceEmissiveMeshes = true;
                }
                lightData.m_NeedsPrefabInstanceCheck = false;

                // Update the list of overlapping lights for the LightOverlap scene view mode
                if (lightData.IsOverlapping())
                    s_overlappingHDLights.Add(lightData);
                else
                    s_overlappingHDLights.Remove(lightData);
#endif

#if UNITY_EDITOR

                // If we requested an emissive mesh but for some reason (e.g. Reload scene unchecked in the Enter Playmode options) Awake has not been called,
                // we need to create it manually.
                if (lightData.m_DisplayAreaLightEmissiveMesh && (lightData.m_ChildEmissiveMeshViewer == null || lightData.m_ChildEmissiveMeshViewer.Equals(null)))
                {
                    lightData.UpdateAreaLightEmissiveMesh();
                }

                //if not parented anymore, refresh it
                if (lightData.m_ChildEmissiveMeshViewer != null && !lightData.m_ChildEmissiveMeshViewer.Equals(null))
                {
                    if (lightData.m_ChildEmissiveMeshViewer.transform.parent != lightData.transform)
                    {
                        lightData.CreateChildEmissiveMeshViewerIfNeeded();
                        lightData.UpdateAreaLightEmissiveMesh();
                    }
                    if (lightData.m_ChildEmissiveMeshViewer.isStatic != lightData.gameObject.isStatic)
                        lightData.m_ChildEmissiveMeshViewer.isStatic = lightData.gameObject.isStatic;
                    if (GameObjectUtility.GetStaticEditorFlags(lightData.m_ChildEmissiveMeshViewer) != GameObjectUtility.GetStaticEditorFlags(lightData.gameObject))
                        GameObjectUtility.SetStaticEditorFlags(lightData.m_ChildEmissiveMeshViewer, GameObjectUtility.GetStaticEditorFlags(lightData.gameObject));
                }
#endif

                //auto change layer on emissive mesh
                if (lightData.areaLightEmissiveMeshLayer == -1
                    && lightData.m_ChildEmissiveMeshViewer != null && !lightData.m_ChildEmissiveMeshViewer.Equals(null)
                    && lightData.m_ChildEmissiveMeshViewer.layer != lightData.gameObject.layer)
                    lightData.m_ChildEmissiveMeshViewer.layer = lightData.gameObject.layer;

                // Delayed cleanup when removing emissive mesh from timeline
                if (lightData.needRefreshEmissiveMeshesFromTimeLineUpdate)
                {
                    lightData.needRefreshEmissiveMeshesFromTimeLineUpdate = false;
                    lightData.UpdateAreaLightEmissiveMesh();
                }

#if UNITY_EDITOR
                // Prefab instance child emissive mesh update
                if (lightData.needRefreshPrefabInstanceEmissiveMeshes)
                {
                    // We must not call the update on Prefab Asset that are already updated or we will enter infinite loop
                    if (!PrefabUtility.IsPartOfPrefabAsset(lightData))
                    {
                        lightData.UpdateAreaLightEmissiveMesh();
                    }
                    lightData.needRefreshPrefabInstanceEmissiveMeshes = false;
                }
#endif

                if (lightData.legacyLight.enabled != lightData.timelineWorkaround.lightEnabled)
                {
                    lightData.SetEmissiveMeshRendererEnabled(lightData.legacyLight.enabled);
                    lightData.timelineWorkaround.lightEnabled = lightData.legacyLight.enabled;
                }

                // Check if the intensity have been changed by the inspector or an animator
                if (lightData.timelineWorkaround.oldLossyScale != lightData.transform.lossyScale
                    || lightData.legacyLight.colorTemperature != lightData.timelineWorkaround.oldLightColorTemperature)
                {
                    lightData.UpdateAreaLightEmissiveMesh();
                    lightData.timelineWorkaround.oldLossyScale = lightData.transform.lossyScale;
                    lightData.timelineWorkaround.oldLightColorTemperature = lightData.legacyLight.colorTemperature;
                }

#if !UNITY_EDITOR
                // Same check for light angle to update intensity using spot angle
                if ((lightData.legacyLight.type == LightType.Spot || lightData.legacyLight.type == LightType.Pyramid) &&
                    (lightData.timelineWorkaround.oldSpotAngle != lightData.legacyLight.spotAngle))
                {
                    // If light unit is currently displayed in lumen and 'reflector' is on and the spot angle has changed,
                    // recalculate intensity (candela) so lumen value remains constant
                    if (lightData.legacyLight.lightUnit == LightUnit.Lumen && lightData.legacyLight.enableSpotReflector)
                    {
                        float oldSolidAngle;
                        float newSolidAngle;
                        if (lightData.legacyLight.type == LightType.Spot)
                        {
                            oldSolidAngle = LightUnitUtils.GetSolidAngleFromSpotLight(lightData.timelineWorkaround.oldSpotAngle);
                            newSolidAngle = LightUnitUtils.GetSolidAngleFromSpotLight(lightData.legacyLight.spotAngle);
                        }
                        else // Pyramid
                        {
                            oldSolidAngle = LightUnitUtils.GetSolidAngleFromPyramidLight(
                                lightData.timelineWorkaround.oldSpotAngle,
                                lightData.aspectRatio);
                            newSolidAngle = LightUnitUtils.GetSolidAngleFromPyramidLight(
                                lightData.legacyLight.spotAngle,
                                lightData.aspectRatio);
                        }

                        float oldLumen = LightUnitUtils.CandelaToLumen(lightData.legacyLight.intensity, oldSolidAngle);
                        lightData.legacyLight.intensity = LightUnitUtils.LumenToCandela(oldLumen, newSolidAngle);
                    }
                    lightData.timelineWorkaround.oldSpotAngle = lightData.legacyLight.spotAngle;
                }
#endif

                if (lightData.legacyLight.color != lightData.timelineWorkaround.oldLightColor
                    || lightData.timelineWorkaround.oldLossyScale != lightData.transform.lossyScale
                    || lightData.displayAreaLightEmissiveMesh != lightData.timelineWorkaround.oldDisplayAreaLightEmissiveMesh
                    || lightData.legacyLight.colorTemperature != lightData.timelineWorkaround.oldLightColorTemperature)
                {
                    lightData.UpdateAreaLightEmissiveMesh();
                    lightData.timelineWorkaround.oldLightColor = lightData.legacyLight.color;
                    lightData.timelineWorkaround.oldLossyScale = lightData.transform.lossyScale;
                    lightData.timelineWorkaround.oldDisplayAreaLightEmissiveMesh = lightData.displayAreaLightEmissiveMesh;
                    lightData.timelineWorkaround.oldLightColorTemperature = lightData.legacyLight.colorTemperature;
                }
            }
        }

        void OnDidApplyAnimationProperties()
        {
            UpdateAllLightValues(fromTimeLine: true);
            UpdateRenderEntity();
        }

        /// <summary>
        /// Copy all field from this to an additional light data
        /// </summary>
        /// <param name="data">Destination component</param>
        public void CopyTo(HDAdditionalLightData data)
        {
            data.m_InnerSpotPercent = m_InnerSpotPercent;
            data.m_SpotIESCutoffPercent = m_SpotIESCutoffPercent;
            data.m_LightDimmer = m_LightDimmer;
            data.m_VolumetricDimmer = m_VolumetricDimmer;
            data.m_FadeDistance = m_FadeDistance;
            data.m_VolumetricFadeDistance = m_VolumetricFadeDistance;
            data.m_AffectDiffuse = m_AffectDiffuse;
            data.m_AffectSpecular = m_AffectSpecular;
            data.m_NonLightmappedOnly = m_NonLightmappedOnly;
            data.m_ShapeWidth = m_ShapeWidth;
            data.m_ShapeHeight = m_ShapeHeight;
            data.m_AspectRatio = m_AspectRatio;
            data.m_ShapeRadius = m_ShapeRadius;
            data.m_SoftnessScale = m_SoftnessScale;
            data.m_UseCustomSpotLightShadowCone = m_UseCustomSpotLightShadowCone;
            data.m_CustomSpotLightShadowCone = m_CustomSpotLightShadowCone;
            data.m_MaxSmoothness = m_MaxSmoothness;
            data.m_ApplyRangeAttenuation = m_ApplyRangeAttenuation;
            data.m_DisplayAreaLightEmissiveMesh = m_DisplayAreaLightEmissiveMesh;
            data.m_AreaLightCookie = m_AreaLightCookie;
            data.m_IESPoint = m_IESPoint;
            data.m_IESSpot = m_IESSpot;
            data.m_IncludeForRayTracing = m_IncludeForRayTracing;
            data.m_IncludeForPathTracing = m_IncludeForPathTracing;
            data.m_AreaLightShadowCone = m_AreaLightShadowCone;
            data.m_UseScreenSpaceShadows = m_UseScreenSpaceShadows;
            data.m_InteractsWithSky = m_InteractsWithSky;
            data.m_AngularDiameter = m_AngularDiameter;
            data.diameterMultiplerMode = diameterMultiplerMode;
            data.diameterMultiplier = diameterMultiplier;
            data.diameterOverride = diameterOverride;
            data.celestialBodyShadingSource = celestialBodyShadingSource;
            data.sunLightOverride = sunLightOverride;
            data.sunColor = sunColor;
            data.sunIntensity = sunIntensity;
            data.moonPhase = moonPhase;
            data.moonPhaseRotation = moonPhaseRotation;
            data.earthshine = earthshine;
            data.flareSize = flareSize;
            data.flareTint = flareTint;
            data.flareFalloff = flareFalloff;
            data.flareMultiplier = flareMultiplier;
            data.surfaceTexture = surfaceTexture;
            data.surfaceTint = surfaceTint;
            data.m_Distance = m_Distance;
            data.m_UseRayTracedShadows = m_UseRayTracedShadows;
            data.m_NumRayTracingSamples = m_NumRayTracingSamples;
            data.m_FilterTracedShadow = m_FilterTracedShadow;
            data.m_FilterSizeTraced = m_FilterSizeTraced;
            data.m_SunLightConeAngle = m_SunLightConeAngle;
            data.m_LightShadowRadius = m_LightShadowRadius;
            data.m_SemiTransparentShadow = m_SemiTransparentShadow;
            data.m_ColorShadow = m_ColorShadow;
            data.m_DistanceBasedFiltering = m_DistanceBasedFiltering;
            data.m_EvsmExponent = m_EvsmExponent;
            data.m_EvsmLightLeakBias = m_EvsmLightLeakBias;
            data.m_EvsmVarianceBias = m_EvsmVarianceBias;
            data.m_EvsmBlurPasses = m_EvsmBlurPasses;
            data.m_LightlayersMask = m_LightlayersMask;
            data.m_LinkShadowLayers = m_LinkShadowLayers;
            data.m_ShadowNearPlane = m_ShadowNearPlane;
            data.m_BlockerSampleCount = m_BlockerSampleCount;
            data.m_FilterSampleCount = m_FilterSampleCount;
            data.m_MinFilterSize = m_MinFilterSize;
            data.m_KernelSize = m_KernelSize;
            data.m_LightAngle = m_LightAngle;
            data.m_MaxDepthBias = m_MaxDepthBias;
            m_ShadowResolution.CopyTo(data.m_ShadowResolution);
            data.m_ShadowDimmer = m_ShadowDimmer;
            data.m_VolumetricShadowDimmer = m_VolumetricShadowDimmer;
            data.m_ShadowFadeDistance = m_ShadowFadeDistance;
            m_UseContactShadow.CopyTo(data.m_UseContactShadow);
            data.m_RayTracedContactShadow = m_RayTracedContactShadow;
            data.m_ShadowTint = m_ShadowTint;
            data.m_PenumbraTint = m_PenumbraTint;
            data.m_NormalBias = m_NormalBias;
            data.m_SlopeBias = m_SlopeBias;
            data.m_ShadowUpdateMode = m_ShadowUpdateMode;
            data.m_AlwaysDrawDynamicShadows = m_AlwaysDrawDynamicShadows;
            data.m_UpdateShadowOnLightMovement = m_UpdateShadowOnLightMovement;
            data.m_CachedShadowTranslationThreshold = m_CachedShadowTranslationThreshold;
            data.m_CachedShadowAngularThreshold = m_CachedShadowAngularThreshold;
            data.m_BarnDoorLength = m_BarnDoorLength;
            data.m_BarnDoorAngle = m_BarnDoorAngle;
            data.m_preserveCachedShadow = m_preserveCachedShadow;
            data.m_OnDemandShadowRenderOnPlacement = m_OnDemandShadowRenderOnPlacement;
            data.forceRenderOnPlacement = forceRenderOnPlacement;
            data.m_ShadowCascadeRatios = new float[m_ShadowCascadeRatios.Length];
            m_ShadowCascadeRatios.CopyTo(data.m_ShadowCascadeRatios, 0);
            data.m_ShadowCascadeBorders = new float[m_ShadowCascadeBorders.Length];
            m_ShadowCascadeBorders.CopyTo(data.m_ShadowCascadeBorders, 0);
            data.m_ShadowAlgorithm = m_ShadowAlgorithm;
            data.m_ShadowVariant = m_ShadowVariant;
            data.m_ShadowPrecision = m_ShadowPrecision;
            data.useOldInspector = useOldInspector;
            data.useVolumetric = useVolumetric;
            data.featuresFoldout = featuresFoldout;
            data.m_AreaLightEmissiveMeshShadowCastingMode = m_AreaLightEmissiveMeshShadowCastingMode;
            data.m_AreaLightEmissiveMeshMotionVectorGenerationMode = m_AreaLightEmissiveMeshMotionVectorGenerationMode;
            data.m_AreaLightEmissiveMeshLayer = m_AreaLightEmissiveMeshLayer;
            data.dirLightPCSSMaxPenumbraSize = dirLightPCSSMaxPenumbraSize;
            data.dirLightPCSSMaxSamplingDistance = dirLightPCSSMaxSamplingDistance;
            data.dirLightPCSSMinFilterSizeTexels = dirLightPCSSMinFilterSizeTexels;
            data.dirLightPCSSMinFilterMaxAngularDiameter = dirLightPCSSMinFilterMaxAngularDiameter;
            data.dirLightPCSSBlockerSearchAngularDiameter = dirLightPCSSBlockerSearchAngularDiameter;
            data.dirLightPCSSBlockerSamplingClumpExponent = dirLightPCSSBlockerSamplingClumpExponent;
            data.dirLightPCSSBlockerSampleCount = dirLightPCSSBlockerSampleCount;
            data.dirLightPCSSFilterSampleCount = dirLightPCSSFilterSampleCount;


#if UNITY_EDITOR
            data.timelineWorkaround = timelineWorkaround;
#endif

            data.UpdateAllLightValues();
            data.UpdateRenderEntity();
        }

        // As we have our own default value, we need to initialize the light intensity correctly
        /// <summary>
        /// Initialize an HDAdditionalLightData that have just beeing created.
        /// </summary>
        /// <param name="lightData"></param>
        public static void InitDefaultHDAdditionalLightData(HDAdditionalLightData lightData)
        {
            var light = lightData.legacyLight;

            // Set light intensity and unit using its type
            switch (light.type)
            {
                case LightType.Directional:
                    light.lightUnit = LightUnit.Lux;
                    light.intensity = k_DefaultDirectionalLightIntensity / Mathf.PI * 100000.0f; // Change back to just k_DefaultDirectionalLightIntensity on 11.0.0 (can't change constant as it's a breaking change)
                    break;
                case LightType.Box:
                    light.lightUnit = LightUnit.Lux;
                    light.intensity = LightUnitUtils.LumenToCandela(k_DefaultPunctualLightIntensity, LightUnitUtils.SphereSolidAngle); // Find a proper default for box lights
                    break;
                case LightType.Rectangle:
                case LightType.Disc:
                case LightType.Tube:
                    light.lightUnit = LightUnit.Lumen;
                    light.intensity = LightUnitUtils.ConvertIntensity(light, k_DefaultAreaLightIntensity, LightUnit.Lumen, LightUnit.Nits);
                    lightData.shadowNearPlane = 0;
                    light.shadows = LightShadows.None;
                    break;
                case LightType.Point:
                case LightType.Spot:
                case LightType.Pyramid:
                    light.lightUnit = LightUnit.Lumen;
                    light.intensity = LightUnitUtils.ConvertIntensity(light, k_DefaultPunctualLightIntensity, LightUnit.Lumen, LightUnit.Candela);
                    break;
            }

            // We don't use the global settings of shadow mask by default
            light.lightShadowCasterMode = LightShadowCasterMode.Everything;

            lightData.normalBias = 0.75f;
            lightData.slopeBias = 0.5f;

            // Enable filter/temperature mode by default for all light types
            lightData.useColorTemperature = true;
        }

        void OnValidate()
        {
            UpdateBounds();

            RefreshCachedShadow();

            // Light size must be non-zero, else we get NaNs.
            shapeWidth = Mathf.Max(shapeWidth, k_MinLightSize);
            shapeHeight = Mathf.Max(shapeHeight, k_MinLightSize);
            shapeRadius = Mathf.Max(shapeRadius, 0.0f);

#if UNITY_EDITOR
            // If modification are due to change on prefab asset, we want to have prefab instances to self-update, but we cannot check in OnValidate if this is part of
            // prefab instance. So we delay the check on next update (and before teh LateUpdate logic)
            m_NeedsPrefabInstanceCheck = true;
#endif
        }

        #region Update functions to patch values in the Light component when we change properties inside HDAdditionalLightData

        void Awake()
        {
            Migrate();

            // We need to reconstruct the emissive mesh at Light creation if needed due to not beeing able to change hierarchy in prefab asset.
            // This is especially true at Tuntime as there is no code path that will trigger the rebuild of emissive mesh until one of the property modifying it is changed.
            UpdateAreaLightEmissiveMesh();
        }

        internal void UpdateAreaLightEmissiveMesh(bool fromTimeLine = false)
        {
            var lightType = legacyLight.type;
            bool displayEmissiveMesh = lightType.IsArea() && displayAreaLightEmissiveMesh;

            // Only show childEmissiveMeshViewer if type is Area and requested
            if (!lightType.IsArea() || !displayEmissiveMesh)
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
            if (GraphicsSettings.TryGetRenderPipelineSettings<HDRenderPipelineRuntimeAssets>(out var assets))
            {
                switch (lightType)
                {
                    case LightType.Tube:
                        if (m_EmissiveMeshFilter.sharedMesh != assets.emissiveCylinderMesh)
                            m_EmissiveMeshFilter.sharedMesh = assets.emissiveCylinderMesh;
                        break;
                    default:
                        if (m_EmissiveMeshFilter.sharedMesh != assets.emissiveQuadMesh)
                            m_EmissiveMeshFilter.sharedMesh = assets.emissiveQuadMesh;
                        break;
                }
            }

            // Update light area size with clamping
            Vector3 lightSize = new Vector3(m_ShapeWidth, m_ShapeHeight, 0);
            if (lightType == LightType.Tube)
                lightSize.y = 0;
            lightSize = Vector3.Max(Vector3.one * k_MinAreaWidth, lightSize);

            switch (lightType)
            {
                case LightType.Rectangle:
                    m_ShapeWidth = lightSize.x;
                    m_ShapeHeight = lightSize.y;
                    break;
                case LightType.Tube:
                    m_ShapeWidth = lightSize.x;
                    break;
                case LightType.Disc:
                    m_ShapeWidth = lightSize.x;
                    m_ShapeHeight = lightSize.x;
                    break;
                default:
                    break;
            }

            if (lightEntity.valid)
            {
                ref HDLightRenderData lightRenderData = ref HDLightRenderDatabase.instance.EditLightDataAsRef(lightEntity);
                lightRenderData.shapeWidth = m_ShapeWidth;
                lightRenderData.shapeHeight = m_ShapeHeight;
            }

            legacyLight.areaSize = lightSize;

            // Update child emissive mesh scale
            Vector3 lossyScale = emissiveMeshRenderer.transform.localRotation * transform.lossyScale;
            emissiveMeshRenderer.transform.localScale = new Vector3(lightSize.x / lossyScale.x, lightSize.y / lossyScale.y, k_MinAreaWidth / lossyScale.z);

            // NOTE: When the user duplicates a light in the editor, the material is not duplicated and when changing the properties of one of them (source or duplication)
            // It either overrides both or is overriden. Given that when we duplicate an object the name changes, this approach works. When the name of the game object is then changed again
            // the material is not re-created until one of the light properties is changed again.
            if (emissiveMeshRenderer.sharedMaterial == null || emissiveMeshRenderer.sharedMaterial.name != gameObject.name)
            {
                // Shader.Find works because the Unlit shader is referenced in the HDRP Runtime Resources
                // We can't access the resources though because HDRP isn't initialized during the Awake of this gameobject
                emissiveMeshRenderer.sharedMaterial = new Material(Shader.Find("HDRP/Unlit"));
                emissiveMeshRenderer.sharedMaterial.SetFloat("_IncludeIndirectLighting", 0.0f);
                emissiveMeshRenderer.sharedMaterial.name = gameObject.name;
            }

            // Update Mesh emissive properties
            emissiveMeshRenderer.sharedMaterial.SetColor("_UnlitColor", Color.black);

            // m_Light.intensity is in luminance which is the value we need for emissive color
            Color value = legacyLight.color.linear * legacyLight.intensity;

            if (useColorTemperature)
                value *= Mathf.CorrelatedColorTemperatureToRGB(legacyLight.colorTemperature);

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
            // TODO: Don't use bounding sphere overrides. Support this properly in Unity native instead.
            legacyLight.useBoundingSphereOverride = true;
            float halfWidth = m_ShapeWidth * 0.5f;
            float halfHeight = m_ShapeHeight * 0.5f;
            float diag = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
            legacyLight.boundingSphereOverride = new Vector4(0.0f, 0.0f, 0.0f, range + diag);
        }

        void UpdateDiscLightBounds()
        {
            legacyLight.useShadowMatrixOverride = false;
            // TODO: Don't use bounding sphere overrides. Support this properly in Unity native instead.
            legacyLight.useBoundingSphereOverride = true;
            legacyLight.boundingSphereOverride = new Vector4(0.0f, 0.0f, 0.0f, range + m_ShapeWidth);
        }

        void UpdateTubeLightBounds()
        {
            legacyLight.useShadowMatrixOverride = false;
            // TODO: Don't use bounding sphere overrides. Support this properly in Unity native instead.
            legacyLight.useBoundingSphereOverride = true;
            legacyLight.boundingSphereOverride = new Vector4(0.0f, 0.0f, 0.0f, range + m_ShapeWidth * 0.5f);
        }

        void UpdateBoxLightBounds()
        {
            legacyLight.useShadowMatrixOverride = true;
            // TODO: Don't use bounding sphere overrides. Support this properly in Unity native instead.
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
            // TODO: Don't use bounding sphere overrides. Support this properly in Unity native instead.
            legacyLight.useBoundingSphereOverride = true;

            // Need to inverse scale because culling != rendering convention apparently
            Matrix4x4 scaleMatrix = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f));
            legacyLight.shadowMatrixOverride = HDShadowUtils.ExtractSpotLightProjectionMatrix(legacyLight.range, legacyLight.spotAngle, shadowNearPlane, aspectRatio, 0.0f) * scaleMatrix;
            legacyLight.boundingSphereOverride = new Vector4(0.0f, 0.0f, 0.0f, legacyLight.range);
        }

        void UpdateBounds()
        {
            switch (legacyLight.type)
            {
                case LightType.Spot:
                    legacyLight.useBoundingSphereOverride = false;
                    legacyLight.useShadowMatrixOverride = false;
                    break;
                case LightType.Box:
                    UpdateBoxLightBounds();
                    break;
                case LightType.Pyramid:
                    UpdatePyramidLightBounds();
                    break;
                case LightType.Rectangle:
                    UpdateRectangleLightBounds();
                    break;
                case LightType.Disc:
                    UpdateDiscLightBounds();
                    break;
                case LightType.Tube:
                    UpdateTubeLightBounds();
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

            if (legacyLight.type == LightType.Pyramid)
            {
                // Pyramid lights use areaSize.x for aspect ratio.
                legacyLight.areaSize = new Vector2(aspectRatio, 0);
            }
            else if (legacyLight.type != LightType.Disc)
            {
                // We don't want to update the disc area since their shape is largely handled by builtin.
                legacyLight.areaSize = new Vector2(shapeWidth, shapeHeight);
            }
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

            // Patch bounds
            UpdateBounds();

            UpdateAreaLightEmissiveMesh(fromTimeLine: fromTimeLine);
        }

        internal void RefreshCachedShadow()
        {
            bool wentThroughCachedShadowSystem = lightIdxForCachedShadows >= 0;
            if (wentThroughCachedShadowSystem)
                HDShadowManager.cachedShadowManager.EvictLight(this, legacyLight.type);

            RegisterCachedShadowLightOptional();
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
        /// Set light cookie. Note that the texture must have a power of two size.
        /// </summary>
        /// <param name="cookie">Cookie texture, must be 2D for Directional, Spot and Area light and Cubemap for Point lights</param>
        /// <param name="directionalLightCookieSize">area light </param>
        public void SetCookie(Texture cookie, Vector2 directionalLightCookieSize)
        {
            LightType lightType = legacyLight.type;
            if (lightType.IsArea())
            {
                if (cookie.dimension != TextureDimension.Tex2D)
                {
                    Debug.LogError("Texture dimension " + cookie.dimension + " is not supported for area lights.");
                    return;
                }
                areaLightCookie = cookie;
            }
            else
            {
                if (lightType == LightType.Point && cookie.dimension != TextureDimension.Cube)
                {
                    Debug.LogError("Texture dimension " + cookie.dimension + " is not supported for point lights.");
                    return;
                }
                else if ((lightType == LightType.Directional || lightType.IsSpot()) && cookie.dimension != TextureDimension.Tex2D) // Only 2D cookie are supported for Directional and Spot lights
                {
                    Debug.LogError("Texture dimension " + cookie.dimension + " is not supported for Directional/Spot lights.");
                    return;
                }
                if (lightType == LightType.Directional)
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
        /// Enable shadows on a light.
        /// </summary>
        /// <param name="enabled"></param>
        public void EnableShadows(bool enabled) => legacyLight.shadows = enabled ? LightShadows.Soft : LightShadows.None;

        internal bool ShadowsEnabled()
        {
            return legacyLight.shadows != LightShadows.None;
        }

        /// <summary>
        /// Set the shadow resolution.
        /// </summary>
        /// <param name="resolution">Must be between 16 and 16384 but we will allow 0 to turn off the shadow</param>
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
        public void SetLightLayer(RenderingLayerMask lightLayerMask, RenderingLayerMask shadowLayerMask)
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
        public void SetShadowLightLayer(RenderingLayerMask shadowLayerMask) => legacyLight.renderingLayerMask = LightLayerToRenderingLayerMask((int)shadowLayerMask, (int)legacyLight.renderingLayerMask);

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
        /// Set the area light size.
        /// </summary>
        /// <param name="size"></param>
        public void SetAreaLightSize(Vector2 size)
        {
            if (legacyLight.type.IsArea())
            {
                m_ShapeWidth = size.x;
                m_ShapeHeight = size.y;
                HDLightRenderDatabase.instance.SetShapeWidth(lightEntity, m_ShapeWidth);
                HDLightRenderDatabase.instance.SetShapeHeight(lightEntity, m_ShapeHeight);
                UpdateAllLightValues();
            }
        }

        /// <summary>
        /// Set the box spot light size.
        /// </summary>
        /// <param name="size"></param>
        public void SetBoxSpotSize(Vector2 size)
        {
            if (legacyLight.type == LightType.Box)
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
        {
            get
            {
                var lightType = legacyLight.type;
                return lightType == LightType.Rectangle
                    ? ShadowMapType.AreaLightAtlas
                    : lightType != LightType.Directional
                        ? ShadowMapType.PunctualAtlas
                        : ShadowMapType.CascadedDirectional;
            }
        }

        // TODO: Remove. Use the above property instead.
        internal ShadowMapType GetShadowMapType(LightType lightType)
        {
            return (lightType == LightType.Rectangle) ? ShadowMapType.AreaLightAtlas
                : lightType != LightType.Directional
                ? ShadowMapType.PunctualAtlas
                : ShadowMapType.CascadedDirectional;
        }

        internal void UpdateRenderEntity()
        {
            //NOTE: do not add members into HDLighRenderData unless this data is strictly required by the GPU lightData.
            // HDRP requires an intermediate / structure like represnetation of lights so we can parallelize processing.
            // Because Lights in HDRP are complimented by HDAdditionalLightData, which is a GameObject component, we cant access
            // this component in burst. Thus every new single member added into a HDLightRenderData struct must be updated to reflect the
            // state of the game side. Adding members into HDLightRenderData will incur into CPU cost for ProcessLightsForGPU.

            HDLightRenderDatabase lightEntities = HDLightRenderDatabase.instance;
            if (!lightEntities.IsValid(lightEntity))
                return;

            ref HDLightRenderData lightRenderData = ref lightEntities.EditLightDataAsRef(lightEntity);
            lightRenderData.renderingLayerMask = (uint)m_LightlayersMask;
            lightRenderData.fadeDistance = m_FadeDistance;
            lightRenderData.distance = m_Distance;
            lightRenderData.angularDiameter = m_AngularDiameter;
            lightRenderData.volumetricFadeDistance = m_VolumetricFadeDistance;
            lightRenderData.includeForRayTracing = m_IncludeForRayTracing;
            lightRenderData.includeForPathTracing = m_IncludeForPathTracing;
            lightRenderData.useScreenSpaceShadows = m_UseScreenSpaceShadows;

            // If we are pure shadowmask, we disable raytraced shadows.
#if UNITY_EDITOR
            if (legacyLight.lightmapBakeType == LightmapBakeType.Mixed)
#else
            if (legacyLight.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
#endif
                lightRenderData.useRayTracedShadows = !m_NonLightmappedOnly && m_UseRayTracedShadows;
            else
                lightRenderData.useRayTracedShadows = m_UseRayTracedShadows;

            lightRenderData.colorShadow = m_ColorShadow;
            lightRenderData.lightDimmer = m_LightDimmer;
            lightRenderData.volumetricDimmer = m_VolumetricDimmer;
            lightRenderData.shadowDimmer = m_ShadowDimmer;
            lightRenderData.shadowFadeDistance = m_ShadowFadeDistance;
            lightRenderData.volumetricShadowDimmer = m_VolumetricShadowDimmer;
            lightRenderData.shapeWidth = m_ShapeWidth;
            lightRenderData.shapeHeight = m_ShapeHeight;
            lightRenderData.aspectRatio = m_AspectRatio;
            lightRenderData.innerSpotPercent = m_InnerSpotPercent;
            lightRenderData.spotIESCutoffPercent = m_SpotIESCutoffPercent;
            lightRenderData.shapeRadius = m_ShapeRadius;
            lightRenderData.barnDoorLength = m_BarnDoorLength;
            lightRenderData.affectVolumetric = useVolumetric;
            lightRenderData.affectDiffuse = m_AffectDiffuse;
            lightRenderData.affectSpecular = m_AffectSpecular;
            lightRenderData.applyRangeAttenuation = m_ApplyRangeAttenuation;
            lightRenderData.penumbraTint = m_PenumbraTint;
            lightRenderData.interactsWithSky = m_InteractsWithSky;
            lightRenderData.shadowTint = m_ShadowTint;

            lightEntities.EditAdditionalLightUpdateDataAsRef(lightEntity).Set(this);
        }

        internal void CreateHDLightRenderEntity(bool autoDestroy = false)
        {
            if (!lightEntity.valid)
            {
                HDLightRenderDatabase lightEntities = HDLightRenderDatabase.instance;
                lightEntity = lightEntities.CreateEntity(autoDestroy);
                lightEntities.AttachGameObjectData(lightEntity, legacyLight.GetInstanceID(), this, legacyLight.gameObject);
            }

            UpdateRenderEntity();
        }

        void OnEnable()
        {
            CreateHDLightRenderEntity();

            RegisterCachedShadowLightOptional();

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

        /// <summary>Tell if the light is overlapping for the light overlap debug mode</summary>
        internal bool IsOverlapping()
        {
            var baking = legacyLight.bakingOutput;
            bool isOcclusionSeparatelyBaked = baking.occlusionMaskChannel != -1;
            bool isDirectUsingBakedOcclusion = baking.mixedLightingMode == MixedLightingMode.Shadowmask || baking.mixedLightingMode == MixedLightingMode.Subtractive;
            return isDirectUsingBakedOcclusion && !isOcclusionSeparatelyBaked;
        }
    }

    // The LateUpdate of HDAdditionalLightData relies on Unity's LateUpdate callback, which comes with significant overhead.
    // By adding a single static callback to Unity's PlayerLoop, we reduced the per-frame per-light CPU overhead considerably.

    /// <summary>
    /// LightLateUpdate.
    /// </summary>
    public static class LightLateUpdate
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        internal static void Init()
        {
            var currentLoopSystem = LowLevel.PlayerLoop.GetCurrentPlayerLoop();

            bool found = AppendToPlayerLoopList(typeof(LightLateUpdate), Tick, ref currentLoopSystem, typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate));
            LowLevel.PlayerLoop.SetPlayerLoop(currentLoopSystem);
        }
        internal static void Tick()
        {
            HDAdditionalLightData.TickLateUpdate();
        }

        internal static bool AppendToPlayerLoopList(Type updateType, PlayerLoopSystem.UpdateFunction updateFunction, ref PlayerLoopSystem playerLoop, Type playerLoopSystemType)
        {
            if (updateType == null || updateFunction == null || playerLoopSystemType == null)
                return false;

            if (playerLoop.type == playerLoopSystemType)
            {
                var oldListLength = playerLoop.subSystemList != null ? playerLoop.subSystemList.Length : 0;
                var newSubsystemList = new PlayerLoopSystem[oldListLength + 1];
                for (var i = 0; i < oldListLength; ++i)
                    newSubsystemList[i] = playerLoop.subSystemList[i];
                newSubsystemList[oldListLength] = new PlayerLoopSystem
                {
                    type = updateType,
                    updateDelegate = updateFunction
                };
                playerLoop.subSystemList = newSubsystemList;
                return true;
            }

            if (playerLoop.subSystemList != null)
            {
                for (var i = 0; i < playerLoop.subSystemList.Length; ++i)
                {
                    if (AppendToPlayerLoopList(updateType, updateFunction, ref playerLoop.subSystemList[i], playerLoopSystemType))
                        return true;
                }
            }
            return false;
        }
    }
}
