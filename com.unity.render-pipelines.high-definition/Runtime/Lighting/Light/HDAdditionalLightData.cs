using System;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEditor.Experimental.Rendering.HDPipeline;
#endif
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // This enum extent the original LightType enum with new light type from HD
    public enum LightTypeExtent
    {
        Punctual, // Fallback on LightShape type
        Rectangle,
        Tube,
        // Sphere,
        // Disc,
    };

    public enum SpotLightShape { Cone, Pyramid, Box };

    public enum LightUnit
    {
        Lumen,      // lm = total power/flux emitted by the light
        Candela,    // lm/sr = flux per steradian
        Lux,        // lm/m² = flux per unit area
        Luminance,  // lm/m²/sr = flux per unit area and per steradian
        Ev100,      // ISO 100 Exposure Value (https://en.wikipedia.org/wiki/Exposure_value)
    }

    // Light layering
    public enum LightLayerEnum
    {
        Nothing = 0,   // Custom name for "Nothing" option
        LightLayerDefault = 1 << 0,
        LightLayer1 = 1 << 1,
        LightLayer2 = 1 << 2,
        LightLayer3 = 1 << 3,
        LightLayer4 = 1 << 4,
        LightLayer5 = 1 << 5,
        LightLayer6 = 1 << 6,
        LightLayer7 = 1 << 7,
        Everything = 0xFF, // Custom name for "Everything" option
    }

    // This structure contains all the old values for every recordable fields from the HD light editor
    // so we can force timeline to record changes on other fields from the LateUpdate function (editor only)
    struct TimelineWorkaround
    {
        public float oldDisplayLightIntensity;
        public float oldLuxAtDistance;
        public float oldSpotAngle;
        public bool oldEnableSpotReflector;
        public Color oldLightColor;
        public Vector3 oldLocalScale;
        public bool oldDisplayAreaLightEmissiveMesh;
        public LightTypeExtent oldLightTypeExtent;
        public float oldLightColorTemperature;
        public Vector3 oldShape;
        public float lightDimmer;
    }

    //@TODO: We should continuously move these values
    // into the engine when we can see them being generally useful
    [RequireComponent(typeof(Light))]
    [ExecuteAlways]
    public class HDAdditionalLightData : MonoBehaviour, ISerializationCallbackReceiver
    {
        // TODO: Use proper migration toolkit
        // 3. Added ShadowNearPlane to HDRP additional light data, we don't use Light.shadowNearPlane anymore
        // 4. Migrate HDAdditionalLightData.lightLayer to Light.renderingLayerMask
        private const int currentVersion = 4;

        [HideInInspector, SerializeField]
        [FormerlySerializedAs("m_Version")]
        [System.Obsolete("version is deprecated, use m_Version instead")]
        private float version = currentVersion;
        [SerializeField]
        private int m_Version = currentVersion;

        // To be able to have correct default values for our lights and to also control the conversion of intensity from the light editor (so it is compatible with GI)
        // we add intensity (for each type of light we want to manage).
        [System.Obsolete("directionalIntensity is deprecated, use intensity and lightUnit instead")]
        public float directionalIntensity = k_DefaultDirectionalLightIntensity;
        [System.Obsolete("punctualIntensity is deprecated, use intensity and lightUnit instead")]
        public float punctualIntensity = k_DefaultPunctualLightIntensity;
        [System.Obsolete("areaIntensity is deprecated, use intensity and lightUnit instead")]
        public float areaIntensity = k_DefaultAreaLightIntensity;

        public const float k_DefaultDirectionalLightIntensity = Mathf.PI; // In lux
        public const float k_DefaultPunctualLightIntensity = 600.0f;      // Light default to 600 lumen, i.e ~48 candela
        public const float k_DefaultAreaLightIntensity = 200.0f;          // Light default to 200 lumen to better match point light

        public float intensity
        {
            get { return displayLightIntensity; }
            set { SetLightIntensity(value); }
        }

        // Only for Spotlight, should be hide for other light
        public bool enableSpotReflector = false;
        // Lux unity for all light except directional require a distance
        public float luxAtDistance = 1.0f;

        [Range(0.0f, 100.0f)]
        public float m_InnerSpotPercent; // To display this field in the UI this need to be public

        public float GetInnerSpotPercent01()
        {
            return Mathf.Clamp(m_InnerSpotPercent, 0.0f, 100.0f) / 100.0f;
        }

        [Range(0.0f, 1.0f)]
        public float lightDimmer = 1.0f;

        [Range(0.0f, 1.0f), SerializeField, FormerlySerializedAs("volumetricDimmer")]
        private float m_VolumetricDimmer = 1.0f;

        public float volumetricDimmer
        {
            get { return useVolumetric ? m_VolumetricDimmer : 0f; }
            set {  m_VolumetricDimmer = value; }
        }

        // Used internally to convert any light unit input into light intensity
        public LightUnit lightUnit = LightUnit.Lumen;

        // Directional light only.
        public float sunDiskSize = 1.0f;
        public float sunHaloSize = 0.1f;

        // Not used for directional lights.
        public float fadeDistance = 10000.0f;

        public bool affectDiffuse = true;
        public bool affectSpecular = true;

        // This property work only with shadow mask and allow to say we don't render any lightMapped object in the shadow map
        public bool nonLightmappedOnly = false;

        public LightTypeExtent lightTypeExtent = LightTypeExtent.Punctual;

        // Only for Spotlight, should be hide for other light
        public SpotLightShape spotLightShape { get { return m_SpotLightShape; } set { SetSpotLightShape(value); } }
        [SerializeField, FormerlySerializedAs("spotLightShape")]
        SpotLightShape m_SpotLightShape = SpotLightShape.Cone;

        // Only for Rectangle/Line/box projector lights
        public float shapeWidth = 0.5f;

        // Only for Rectangle/box projector lights
        public float shapeHeight = 0.5f;

        // Only for pyramid projector
        public float aspectRatio = 1.0f;

        // Only for Punctual/Sphere/Disc
        public float shapeRadius = 0.0f;

        // Only for Spot/Point - use to cheaply fake specular spherical area light
        // It is not 1 to make sure the highlight does not disappear.
        [Range(0.0f, 1.0f)]
        public float maxSmoothness = 0.99f;

        // If true, we apply the smooth attenuation factor on the range attenuation to get 0 value, else the attenuation is just inverse square and never reach 0
        public bool applyRangeAttenuation = true;

        // This is specific for the LightEditor GUI and not use at runtime
        public bool useOldInspector = false;
        public bool useVolumetric = true;
        public bool featuresFoldout = true;
        public byte showAdditionalSettings = 0;
        public float displayLightIntensity;

        // When true, a mesh will be display to represent the area light (Can only be change in editor, component is added in Editor)
        public bool displayAreaLightEmissiveMesh = false;

        // Optional cookie for rectangular area lights
        public Texture areaLightCookie = null;

        [Range(0.0f, 179.0f)]
        public float areaLightShadowCone = 120.0f;

#if ENABLE_RAYTRACING
        public bool useRayTracedShadows = false;
#endif

        [Range(0.0f, 42.0f)]
        public float evsmExponent = 15.0f;
        [Range(0.0f, 1.0f)]
        public float evsmLightLeakBias = 0.0f;
        [Range(0.0f, 0.001f)]
        public float evsmVarianceBias = 1e-5f;
        [Range(0, 8)]
        public int evsmBlurPasses = 0;

        // Duplication of HDLightEditor.k_MinAreaWidth, maybe do something about that
        const float k_MinAreaWidth = 0.01f; // Provide a small size of 1cm for line light

        [Obsolete("Use Light.renderingLayerMask instead")]
        public LightLayerEnum lightLayers = LightLayerEnum.LightLayerDefault;

        // This function return a mask of light layers as uint and handle the case of Everything as being 0xFF and not -1
        public uint GetLightLayers()
        {
            int value = m_Light.renderingLayerMask;
            return value < 0 ? (uint)LightLayerEnum.Everything : (uint)value;
        }

        // Shadow Settings
        public float    shadowNearPlane = 0.1f;

        // PCSS settings
        [Range(0, 1.0f)]
        public float    shadowSoftness = .5f;
        [Range(1, 64)]
        public int      blockerSampleCount = 24;
        [Range(1, 64)]
        public int      filterSampleCount = 16;
        [Range(0, 0.001f)]
        public float minFilterSize = 0.00001f;

        // Improved Moment Shadows settings
        [Range(1, 32)]
        public int kernelSize = 5;
        [Range(0.0f, 9.0f)]
        public float lightAngle = 1.0f;
        [Range(0.0001f, 0.01f)]
        public float maxDepthBias = 0.001f;

        HDShadowRequest[]   shadowRequests;
        bool                m_WillRenderShadows;
        int[]               m_ShadowRequestIndices;


        #if ENABLE_RAYTRACING
        // Temporary index that stores the current shadow index for the light
        [System.NonSerialized] public int shadowIndex;
        #endif

        [System.NonSerialized] HDShadowSettings    _ShadowSettings = null;
        HDShadowSettings    m_ShadowSettings
        {
            get
            {
                if (_ShadowSettings == null)
                    _ShadowSettings = VolumeManager.instance.stack.GetComponent<HDShadowSettings>();
                return _ShadowSettings;
            }
        }

        AdditionalShadowData _ShadowData;
        AdditionalShadowData m_ShadowData
        {
            get
            {
                if (_ShadowData == null)
                    _ShadowData = GetComponent<AdditionalShadowData>();
                return _ShadowData;
            }
        }

        int GetShadowRequestCount()
        {
            return (m_Light.type == LightType.Point && lightTypeExtent == LightTypeExtent.Punctual) ? 6 : (m_Light.type == LightType.Directional) ? m_ShadowSettings.cascadeShadowSplitCount : 1;
        }

        public void ReserveShadows(Camera camera, HDShadowManager shadowManager, HDShadowInitParameters initParameters, CullingResults cullResults, FrameSettings frameSettings, int lightIndex)
        {
            Bounds bounds;
            float cameraDistance = Vector3.Distance(camera.transform.position, transform.position);

            m_WillRenderShadows = m_Light.shadows != LightShadows.None && frameSettings.IsEnabled(FrameSettingsField.Shadow);

            m_WillRenderShadows &= cullResults.GetShadowCasterBounds(lightIndex, out bounds);
            // When creating a new light, at the first frame, there is no AdditionalShadowData so we can't really render shadows
            m_WillRenderShadows &= m_ShadowData != null && m_ShadowData.shadowDimmer > 0;
            // If the shadow is too far away, we don't render it
            if (m_ShadowData != null)
                m_WillRenderShadows &= m_Light.type == LightType.Directional || cameraDistance < (m_ShadowData.shadowFadeDistance);

#if ENABLE_RAYTRACING
            m_WillRenderShadows &= !(lightTypeExtent == LightTypeExtent.Rectangle && useRayTracedShadows);
#endif

            if (!m_WillRenderShadows)
                return;

            // Create shadow requests array using the light type
            if (shadowRequests == null || m_ShadowRequestIndices == null)
            {
                const int maxLightShadowRequestsCount = 6;
                shadowRequests = new HDShadowRequest[maxLightShadowRequestsCount];
                m_ShadowRequestIndices = new int[maxLightShadowRequestsCount];

                for (int i = 0; i < maxLightShadowRequestsCount; i++)
                    shadowRequests[i] = new HDShadowRequest();
            }

            Vector2 viewportSize = new Vector2(m_ShadowData.shadowResolution, m_ShadowData.shadowResolution);

            // Compute dynamic shadow resolution
            if (initParameters.useDynamicViewportRescale && m_Light.type != LightType.Directional)
            {
                // resize viewport size by the normalized size of the light on screen
                // When we will have access to the non screen clamped bounding sphere light size, we could use it to scale the shadow map resolution
                // For the moment, this will be enough
                viewportSize *= Mathf.Lerp(64f / viewportSize.x, 1f, m_Light.range / (camera.transform.position - transform.position).magnitude);
                viewportSize = Vector2.Max(new Vector2(64f, 64f) / viewportSize, viewportSize);

                // Prevent flickering caused by the floating size of the viewport
                viewportSize.x = Mathf.Round(viewportSize.x);
                viewportSize.y = Mathf.Round(viewportSize.y);
            }

            viewportSize = Vector2.Max(viewportSize, new Vector2(16, 16));

            // Update the directional shadow atlas size
            if (m_Light.type == LightType.Directional)
                shadowManager.UpdateDirectionalShadowResolution((int)viewportSize.x, m_ShadowSettings.cascadeShadowSplitCount);

            // Reserver wanted resolution in the shadow atlas
            ShadowMapType shadowMapType = (lightTypeExtent == LightTypeExtent.Rectangle) ? ShadowMapType.AreaLightAtlas :
                                          (m_Light.type != LightType.Directional) ? ShadowMapType.PunctualAtlas : ShadowMapType.CascadedDirectional;

            int count = GetShadowRequestCount();
            for (int index = 0; index < count; index++)
                m_ShadowRequestIndices[index] = shadowManager.ReserveShadowResolutions(viewportSize, shadowMapType);
        }

        public bool WillRenderShadows()
        {
            return m_WillRenderShadows;
        }

        // This offset shift the position of the spotlight used to approximate the area light shadows. The offset is the minimum such that the full
        // area light shape is included in the cone spanned by the spot light. 
        public static float GetAreaLightOffsetForShadows(Vector2 shapeSize, float coneAngle)
        {
            float rectangleDiagonal = shapeSize.magnitude;
            float halfAngle = coneAngle * 0.5f;
            float cotanHalfAngle = 1.0f / Mathf.Tan(halfAngle * Mathf.Deg2Rad);
            float offset = rectangleDiagonal * cotanHalfAngle;

            return -offset;
        }

        // Must return the first executed shadow request
        public int UpdateShadowRequest(HDCamera hdCamera, HDShadowManager manager, VisibleLight visibleLight, CullingResults cullResults, int lightIndex, out int shadowRequestCount)
        {
            int                 firstShadowRequestIndex = -1;
            Vector3             cameraPos = hdCamera.camera.transform.position;
            shadowRequestCount = 0;

            int count = GetShadowRequestCount();
            for (int index = 0; index < count; index++)
            {
                var         shadowRequest = shadowRequests[index];
                Matrix4x4   invViewProjection = Matrix4x4.identity;
                int         shadowRequestIndex = m_ShadowRequestIndices[index];
                Vector2     viewportSize = manager.GetReservedResolution(shadowRequestIndex);

                if (shadowRequestIndex == -1)
                    continue;

                if (lightTypeExtent == LightTypeExtent.Rectangle)
                {
                    Vector2 shapeSize = new Vector2(shapeWidth, shapeHeight);
                    float offset = GetAreaLightOffsetForShadows(shapeSize, areaLightShadowCone);
                    Vector3 shadowOffset = offset * visibleLight.GetForward();
                    HDShadowUtils.ExtractAreaLightData(hdCamera, visibleLight, lightTypeExtent, visibleLight.GetPosition() + shadowOffset, areaLightShadowCone, shadowNearPlane - offset, shapeSize, viewportSize, m_ShadowData.normalBiasMax, out shadowRequest.view, out invViewProjection, out shadowRequest.projection, out shadowRequest.deviceProjection, out shadowRequest.splitData);
                }
                else
                {
                    // Write per light type matrices, splitDatas and culling parameters
                    switch (m_Light.type)
                    {
                        case LightType.Point:
                            HDShadowUtils.ExtractPointLightData(
                                hdCamera, m_Light.type, visibleLight, viewportSize, shadowNearPlane,
                                m_ShadowData.normalBiasMax, (uint)index, out shadowRequest.view,
                                out invViewProjection, out shadowRequest.projection,
                                out shadowRequest.deviceProjection, out shadowRequest.splitData
                            );
                            break;
                        case LightType.Spot:
                            HDShadowUtils.ExtractSpotLightData(
                                hdCamera, m_Light.type, spotLightShape, shadowNearPlane, aspectRatio, shapeWidth,
                                shapeHeight, visibleLight, viewportSize, m_ShadowData.normalBiasMax,
                                out shadowRequest.view, out invViewProjection, out shadowRequest.projection,
                                out shadowRequest.deviceProjection, out shadowRequest.splitData
                            );
                            break;
                        case LightType.Directional:
                            Vector4 cullingSphere;
                            float nearPlaneOffset = QualitySettings.shadowNearPlaneOffset;

                            HDShadowUtils.ExtractDirectionalLightData(
                                visibleLight, viewportSize, (uint)index, m_ShadowSettings.cascadeShadowSplitCount,
                                m_ShadowSettings.cascadeShadowSplits, nearPlaneOffset, cullResults, lightIndex,
                                out shadowRequest.view, out invViewProjection, out shadowRequest.projection,
                                out shadowRequest.deviceProjection, out shadowRequest.splitData
                            );

                            cullingSphere = shadowRequest.splitData.cullingSphere;

                            // Camera relative for directional light culling sphere
                            if (ShaderConfig.s_CameraRelativeRendering != 0)
                            {
                                cullingSphere.x -= cameraPos.x;
                                cullingSphere.y -= cameraPos.y;
                                cullingSphere.z -= cameraPos.z;
                            }
                            manager.UpdateCascade(index, cullingSphere, m_ShadowSettings.cascadeShadowBorders[index]);
                            break;
                    }
                }

                // Assign all setting common to every lights
                SetCommonShadowRequestSettings(shadowRequest, cameraPos, invViewProjection, viewportSize, lightIndex);

                manager.UpdateShadowRequest(shadowRequestIndex, shadowRequest);

                // Store the first shadow request id to return it
                if (firstShadowRequestIndex == -1)
                    firstShadowRequestIndex = shadowRequestIndex;

                shadowRequestCount++;
            }

            return firstShadowRequestIndex;
        }

        void SetCommonShadowRequestSettings(HDShadowRequest shadowRequest, Vector3 cameraPos, Matrix4x4 invViewProjection, Vector2 viewportSize, int lightIndex)
        {
            // zBuffer param to reconstruct depth position (for transmission)
            float f = m_Light.range;
            float n = shadowNearPlane;
            shadowRequest.zBufferParam = new Vector4((f-n)/n, 1.0f, (f-n)/n*f, 1.0f/f);
            shadowRequest.viewBias = new Vector4(m_ShadowData.viewBiasMin, m_ShadowData.viewBiasMax, m_ShadowData.viewBiasScale, 2.0f / shadowRequest.projection.m00 / viewportSize.x * 1.4142135623730950488016887242097f);
            shadowRequest.normalBias = new Vector3(m_ShadowData.normalBiasMin, m_ShadowData.normalBiasMax, m_ShadowData.normalBiasScale);
            shadowRequest.flags = 0;
            shadowRequest.flags |= m_ShadowData.sampleBiasScale     ? (int)HDShadowFlag.SampleBiasScale : 0;
            shadowRequest.flags |= m_ShadowData.edgeLeakFixup       ? (int)HDShadowFlag.EdgeLeakFixup : 0;
            shadowRequest.flags |= m_ShadowData.edgeToleranceNormal ? (int)HDShadowFlag.EdgeToleranceNormal : 0;
            shadowRequest.edgeTolerance = m_ShadowData.edgeTolerance;

            // Make light position camera relative:
            // TODO: think about VR (use different camera position for each eye)
            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                var translation = Matrix4x4.Translate(cameraPos);
                shadowRequest.view *= translation;
                translation.SetColumn(3, -cameraPos);
                translation[15] = 1.0f;
                invViewProjection = translation * invViewProjection;
            }

            if (m_Light.type == LightType.Directional || (m_Light.type == LightType.Spot && spotLightShape == SpotLightShape.Box))
                shadowRequest.position = new Vector3(shadowRequest.view.m03, shadowRequest.view.m13, shadowRequest.view.m23);
            else
                shadowRequest.position = (ShaderConfig.s_CameraRelativeRendering != 0) ? transform.position - cameraPos : transform.position;

            shadowRequest.shadowToWorld = invViewProjection.transpose;
            shadowRequest.zClip = (m_Light.type != LightType.Directional);
            shadowRequest.lightIndex = lightIndex;
            // We don't allow shadow resize for directional cascade shadow
            if (m_Light.type == LightType.Directional)
            {
                shadowRequest.shadowMapType = ShadowMapType.CascadedDirectional;
            }
            else if (lightTypeExtent == LightTypeExtent.Rectangle)
            {
                shadowRequest.shadowMapType = ShadowMapType.AreaLightAtlas;
            }
            else
            {
                shadowRequest.shadowMapType = ShadowMapType.PunctualAtlas;
            }

            shadowRequest.lightType = (int) m_Light.type;

            // Shadow algorithm parameters
            shadowRequest.shadowSoftness = shadowSoftness / 100f;
            shadowRequest.blockerSampleCount = blockerSampleCount;
            shadowRequest.filterSampleCount = filterSampleCount;
            shadowRequest.minFilterSize = minFilterSize;

            shadowRequest.kernelSize = (uint)kernelSize;
            shadowRequest.lightAngle = (lightAngle * Mathf.PI / 180.0f);
            shadowRequest.maxDepthBias = maxDepthBias;
            // We transform it to base two for faster computation.
            // So e^x = 2^y where y = x * log2 (e)
            const float log2e = 1.44269504089f;
            shadowRequest.evsmParams.x = evsmExponent * log2e;
            shadowRequest.evsmParams.y = evsmLightLeakBias;
            shadowRequest.evsmParams.z = evsmVarianceBias;
            shadowRequest.evsmParams.w = evsmBlurPasses;
        }

#if UNITY_EDITOR
        // We need these old states to make timeline and the animator record the intensity value and the emissive mesh changes (editor-only)
        [System.NonSerialized]
        TimelineWorkaround timelineWorkaround = new TimelineWorkaround();
#endif

        // For light that used the old intensity system we update them
        [System.NonSerialized]
        bool needsIntensityUpdate_1_0 = false;

        // Runtime datas used to compute light intensity
        Light _light;
        Light m_Light
        {
            get
            {
                if (_light == null)
                    _light = GetComponent<Light>();
                return _light;
            }
        }

        void SetLightIntensity(float intensity)
        {
            displayLightIntensity = intensity;

            if (lightUnit == LightUnit.Lumen)
            {
                if (lightTypeExtent == LightTypeExtent.Punctual)
                    SetLightIntensityPunctual(intensity);
                else
                    m_Light.intensity = LightUtils.ConvertAreaLightLumenToLuminance(lightTypeExtent, intensity, shapeWidth, shapeHeight);
            }
            else if (lightUnit == LightUnit.Ev100)
            {
                m_Light.intensity = LightUtils.ConvertEvToLuminance(intensity);
            }
            else if ((m_Light.type == LightType.Spot || m_Light.type == LightType.Point) && lightUnit == LightUnit.Lux)
            {
                // Box are local directional light with lux unity without at distance
                if ((m_Light.type == LightType.Spot) && (spotLightShape == SpotLightShape.Box))
                    m_Light.intensity = intensity;
                else
                    m_Light.intensity = LightUtils.ConvertLuxToCandela(intensity, luxAtDistance);
            }
            else
                m_Light.intensity = intensity;

#if UNITY_EDITOR
            m_Light.SetLightDirty(); // Should be apply only to parameter that's affect GI, but make the code cleaner
#endif
        }

        void SetLightIntensityPunctual(float intensity)
        {
            switch (m_Light.type)
            {
                case LightType.Directional:
                    m_Light.intensity = intensity; // Always in lux
                    break;
                case LightType.Point:
                    if (lightUnit == LightUnit.Candela)
                        m_Light.intensity = intensity;
                    else
                        m_Light.intensity = LightUtils.ConvertPointLightLumenToCandela(intensity);
                    break;
                case LightType.Spot:
                    if (lightUnit == LightUnit.Candela)
                    {
                        // When using candela, reflector don't have any effect. Our intensity is candela = lumens/steradian and the user
                        // provide desired value for an angle of 1 steradian.
                        m_Light.intensity = intensity;
                    }
                    else  // lumen
                    {
                        if (enableSpotReflector)
                        {
                            // If reflector is enabled all the lighting from the sphere is focus inside the solid angle of current shape
                            if (spotLightShape == SpotLightShape.Cone)
                            {
                                m_Light.intensity = LightUtils.ConvertSpotLightLumenToCandela(intensity, m_Light.spotAngle * Mathf.Deg2Rad, true);
                            }
                            else if (spotLightShape == SpotLightShape.Pyramid)
                            {
                                float angleA, angleB;
                                LightUtils.CalculateAnglesForPyramid(aspectRatio, m_Light.spotAngle * Mathf.Deg2Rad, out angleA, out angleB);

                                m_Light.intensity = LightUtils.ConvertFrustrumLightLumenToCandela(intensity, angleA, angleB);
                            }
                            else // Box shape, fallback to punctual light.
                            {
                                m_Light.intensity = LightUtils.ConvertPointLightLumenToCandela(intensity);
                            }
                        }
                        else
                        {
                            // No reflector, angle act as occlusion of point light.
                            m_Light.intensity = LightUtils.ConvertPointLightLumenToCandela(intensity);
                        }
                    }
                    break;
            }
        }

        public static bool IsAreaLight(LightTypeExtent lightType)
        {
            return lightType != LightTypeExtent.Punctual;
        }

#if UNITY_EDITOR

        // Force to retrieve color light's m_UseColorTemperature because it's private
        [System.NonSerialized]
        SerializedProperty useColorTemperatureProperty;
        [System.NonSerialized]
        SerializedObject lightSerializedObject;
        public bool useColorTemperature
        {
            get
            {
                if (useColorTemperatureProperty == null)
                {
                    lightSerializedObject = new SerializedObject(m_Light);
                    useColorTemperatureProperty = lightSerializedObject.FindProperty("m_UseColorTemperature");
                }

                lightSerializedObject.Update();

                return useColorTemperatureProperty.boolValue;
            }
        }

        // TODO: There are a lot of old != current checks and assignation in this function, maybe think about using another system ?
        void LateUpdate()
        {
            Vector3 shape = new Vector3(shapeWidth, shapeHeight, shapeRadius);

            // Check if the intensity have been changed by the inspector or an animator
            if (displayLightIntensity != timelineWorkaround.oldDisplayLightIntensity
                || luxAtDistance != timelineWorkaround.oldLuxAtDistance
                || lightTypeExtent != timelineWorkaround.oldLightTypeExtent
                || transform.localScale != timelineWorkaround.oldLocalScale
                || shape != timelineWorkaround.oldShape
                || m_Light.colorTemperature != timelineWorkaround.oldLightColorTemperature)
            {
                RefreshLightIntensity();
                UpdateAreaLightEmissiveMesh();
                timelineWorkaround.oldDisplayLightIntensity = displayLightIntensity;
                timelineWorkaround.oldLuxAtDistance = luxAtDistance;
                timelineWorkaround.oldLocalScale = transform.localScale;
                timelineWorkaround.oldLightTypeExtent = lightTypeExtent;
                timelineWorkaround.oldLightColorTemperature = m_Light.colorTemperature;
                timelineWorkaround.oldShape = shape;
            }

            // Same check for light angle to update intensity using spot angle
            if (m_Light.type == LightType.Spot && (timelineWorkaround.oldSpotAngle != m_Light.spotAngle || timelineWorkaround.oldEnableSpotReflector != enableSpotReflector))
            {
                RefreshLightIntensity();
                timelineWorkaround.oldSpotAngle = m_Light.spotAngle;
                timelineWorkaround.oldEnableSpotReflector = enableSpotReflector;
            }

            if (m_Light.color != timelineWorkaround.oldLightColor
                || transform.localScale != timelineWorkaround.oldLocalScale
                || displayAreaLightEmissiveMesh != timelineWorkaround.oldDisplayAreaLightEmissiveMesh
                || lightTypeExtent != timelineWorkaround.oldLightTypeExtent
                || m_Light.colorTemperature != timelineWorkaround.oldLightColorTemperature
                || lightDimmer != timelineWorkaround.lightDimmer)
            {
                UpdateAreaLightEmissiveMesh();
                timelineWorkaround.lightDimmer = lightDimmer;
                timelineWorkaround.oldLightColor = m_Light.color;
                timelineWorkaround.oldLocalScale = transform.localScale;
                timelineWorkaround.oldDisplayAreaLightEmissiveMesh = displayAreaLightEmissiveMesh;
                timelineWorkaround.oldLightTypeExtent = lightTypeExtent;
                timelineWorkaround.oldLightColorTemperature = m_Light.colorTemperature;
            }
        }

        // The editor can only access displayLightIntensity (because of SerializedProperties) so we update the intensity to get the real value
        void RefreshLightIntensity()
        {
            intensity = displayLightIntensity;
        }

        public static bool IsAreaLight(SerializedProperty lightType)
        {
            return IsAreaLight((LightTypeExtent)lightType.enumValueIndex);
        }

        public void UpdateAreaLightEmissiveMesh()
        {
            MeshRenderer emissiveMeshRenderer = GetComponent<MeshRenderer>();
            MeshFilter emissiveMeshFilter = GetComponent<MeshFilter>();

            bool displayEmissiveMesh = IsAreaLight(lightTypeExtent) && displayAreaLightEmissiveMesh;

            // Ensure that the emissive mesh components are here
            if (displayEmissiveMesh)
            {
                if (emissiveMeshRenderer == null)
                    emissiveMeshRenderer = gameObject.AddComponent<MeshRenderer>();
                if (emissiveMeshFilter == null)
                    emissiveMeshFilter = gameObject.AddComponent<MeshFilter>();
            }
            else // Or remove them if the option is disabled
            {
                if (emissiveMeshRenderer != null)
                    DestroyImmediate(emissiveMeshRenderer);
                if (emissiveMeshFilter != null)
                    DestroyImmediate(emissiveMeshFilter);

                // We don't have anything to do left if the dislay emissive mesh option is disabled
                return;
            }

            Vector3 lightSize;

            // Update light area size from GameObject transform scale if the transform have changed
            // else we update the light size from the shape fields
            if (timelineWorkaround.oldLocalScale != transform.localScale)
                lightSize = transform.localScale;
            else
                lightSize = new Vector3(shapeWidth, shapeHeight, transform.localScale.z);

            lightSize = Vector3.Max(Vector3.one * k_MinAreaWidth, lightSize);
            lightSize.z = k_MinAreaWidth;
            m_Light.transform.localScale = lightSize;
            m_Light.areaSize = lightSize;

            switch (lightTypeExtent)
            {
                case LightTypeExtent.Rectangle:
                    shapeWidth = lightSize.x;
                    shapeHeight = lightSize.y;
                    break;
                case LightTypeExtent.Tube:
                    shapeWidth = lightSize.x;
                    break;
                default:
                    break;
            }

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
            Color value = m_Light.color.linear * m_Light.intensity;
            if (useColorTemperature)
                value *= Mathf.CorrelatedColorTemperatureToRGB(m_Light.colorTemperature);

            value *= lightDimmer;

            emissiveMeshRenderer.sharedMaterial.SetColor("_EmissiveColor", value);

            // Set the cookie (if there is one) and raise or remove the shader feature
            emissiveMeshRenderer.sharedMaterial.SetTexture("_EmissiveColorMap", areaLightCookie);
            CoreUtils.SetKeyword(emissiveMeshRenderer.sharedMaterial, "_EMISSIVE_COLOR_MAP", areaLightCookie != null);
        }

#endif

        public void CopyTo(HDAdditionalLightData data)
        {
#pragma warning disable 618
            data.directionalIntensity = directionalIntensity;
            data.punctualIntensity = punctualIntensity;
            data.areaIntensity = areaIntensity;
#pragma warning restore 618
            data.enableSpotReflector = enableSpotReflector;
            data.luxAtDistance = luxAtDistance;
            data.m_InnerSpotPercent = m_InnerSpotPercent;
            data.lightDimmer = lightDimmer;
            data.volumetricDimmer = volumetricDimmer;
            data.lightUnit = lightUnit;
            data.fadeDistance = fadeDistance;
            data.affectDiffuse = affectDiffuse;
            data.affectSpecular = affectSpecular;
            data.nonLightmappedOnly = nonLightmappedOnly;
            data.lightTypeExtent = lightTypeExtent;
            data.spotLightShape = spotLightShape;
            data.shapeWidth = shapeWidth;
            data.shapeHeight = shapeHeight;
            data.aspectRatio = aspectRatio;
            data.shapeRadius = shapeRadius;
            data.maxSmoothness = maxSmoothness;
            data.applyRangeAttenuation = applyRangeAttenuation;
            data.useOldInspector = useOldInspector;
            data.featuresFoldout = featuresFoldout;
            data.showAdditionalSettings = showAdditionalSettings;
            data.displayLightIntensity = displayLightIntensity;
            data.displayAreaLightEmissiveMesh = displayAreaLightEmissiveMesh;
            data.needsIntensityUpdate_1_0 = needsIntensityUpdate_1_0;

#if UNITY_EDITOR
            data.timelineWorkaround = timelineWorkaround;
#endif
        }

        void SetSpotLightShape(SpotLightShape shape)
        {
            m_SpotLightShape = shape;
            UpdateBounds();
        }

        void UpdateAreaLightBounds()
        {
            m_Light.useShadowMatrixOverride = false;
            m_Light.useBoundingSphereOverride = true;
            m_Light.boundingSphereOverride = new Vector4(0.0f, 0.0f, 0.0f, m_Light.range);
        }

        void UpdateBoxLightBounds()
        {
            m_Light.useShadowMatrixOverride = true;
            m_Light.useBoundingSphereOverride = true;

            // Need to inverse scale because culling != rendering convention apparently
            Matrix4x4 scaleMatrix = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f));
            m_Light.shadowMatrixOverride = HDShadowUtils.ExtractBoxLightProjectionMatrix(m_Light.range, shapeWidth, shapeHeight, shadowNearPlane) * scaleMatrix;

            // Very conservative bounding sphere taking the diagonal of the shape as the radius
            float diag = new Vector3(shapeWidth * 0.5f, shapeHeight * 0.5f, m_Light.range * 0.5f).magnitude;
            m_Light.boundingSphereOverride = new Vector4(0.0f, 0.0f, m_Light.range * 0.5f, diag);
        }

        void UpdatePyramidLightBounds()
        {
            m_Light.useShadowMatrixOverride = true;
            m_Light.useBoundingSphereOverride = true;

            // Need to inverse scale because culling != rendering convention apparently
            Matrix4x4 scaleMatrix = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f));
            m_Light.shadowMatrixOverride = HDShadowUtils.ExtractSpotLightProjectionMatrix(m_Light.range, m_Light.spotAngle, shadowNearPlane, aspectRatio, 0.0f) * scaleMatrix;

            // Very conservative bounding sphere taking the diagonal of the shape as the radius
            float diag = new Vector3(shapeWidth * 0.5f, shapeHeight * 0.5f, m_Light.range * 0.5f).magnitude;
            m_Light.boundingSphereOverride = new Vector4(0.0f, 0.0f, m_Light.range * 0.5f, diag);
        }

        void UpdateBounds()
        {
            if (lightTypeExtent == LightTypeExtent.Punctual && m_Light.type == LightType.Spot)
            {
                switch (spotLightShape)
                {
                    case SpotLightShape.Box:
                        UpdateBoxLightBounds();
                        break;
                    case SpotLightShape.Pyramid:
                        UpdatePyramidLightBounds();
                        break;
                    default: // Cone
                        m_Light.useBoundingSphereOverride = false;
                        m_Light.useShadowMatrixOverride = false;
                        break;
                }
            }
            else if (lightTypeExtent == LightTypeExtent.Rectangle || lightTypeExtent == LightTypeExtent.Tube)
            {
                UpdateAreaLightBounds();
            }
            else
            {
                m_Light.useBoundingSphereOverride = false;
                m_Light.useShadowMatrixOverride = false;
            }
        }

        // As we have our own default value, we need to initialize the light intensity correctly
        public static void InitDefaultHDAdditionalLightData(HDAdditionalLightData lightData)
        {
            // Special treatment for Unity built-in area light. Change it to our rectangle light
            var light = lightData.gameObject.GetComponent<Light>();

            // Set light intensity and unit using its type
            switch (light.type)
            {
                case LightType.Directional:
                    lightData.lightUnit = LightUnit.Lux;
                    lightData.intensity = k_DefaultDirectionalLightIntensity;
                    break;
                case LightType.Rectangle: // Rectangle by default when light is created
                    lightData.lightUnit = LightUnit.Lumen;
                    lightData.intensity = k_DefaultAreaLightIntensity;
                    light.shadows = LightShadows.None;
                    break;
                case LightType.Point:
                case LightType.Spot:
                    lightData.lightUnit = LightUnit.Lumen;
                    lightData.intensity = k_DefaultPunctualLightIntensity;
                    break;
            }

            // Sanity check: lightData.lightTypeExtent is init to LightTypeExtent.Punctual (in case for unknow reasons we recreate additional data on an existing line)
            if (light.type == LightType.Rectangle && lightData.lightTypeExtent == LightTypeExtent.Punctual)
            {
                lightData.lightTypeExtent = LightTypeExtent.Rectangle;
                light.type = LightType.Point; // Same as in HDLightEditor
#if UNITY_EDITOR
                light.lightmapBakeType = LightmapBakeType.Realtime;
#endif
            }

            // We don't use the global settings of shadow mask by default
            light.lightShadowCasterMode = LightShadowCasterMode.Everything;
        }

        void OnValidate()
        {
            UpdateBounds();
        }

        public void OnBeforeSerialize()
        {
            UpdateBounds();
        }

        public void OnAfterDeserialize()
        {
            // Note: the field version is deprecated but we keep it for retro-compatibility reasons, you should use m_Version instead
#pragma warning disable 618
            if (version <= 1.0f)
#pragma warning restore 618
            {
                // Note: We can't access to the light component in OnAfterSerialize as it is not init() yet,
                // so instead we use a boolean to do the upgrade in OnEnable().
                // However OnEnable is not call when the light is disabled, so the HDLightEditor also call
                // the UpgradeLight() code in this case
                needsIntensityUpdate_1_0 = true;
            }
        }

        private void OnEnable()
        {
            UpgradeLight();
        }

        public void UpgradeLight()
        {
// Disable the warning generated by deprecated fields (areaIntensity, directionalIntensity, ...)
#pragma warning disable 618

            // If we are deserializing an old version, convert the light intensity to the new system
            if (needsIntensityUpdate_1_0)
            {
                switch (lightTypeExtent)
                {
                    case LightTypeExtent.Punctual:
                        switch (m_Light.type)
                        {
                            case LightType.Directional:
                                lightUnit = LightUnit.Lux;
                                intensity = directionalIntensity;
                                break;
                            case LightType.Spot:
                            case LightType.Point:
                                lightUnit = LightUnit.Lumen;
                                intensity = punctualIntensity;
                                break;
                        }
                        break;
                    case LightTypeExtent.Tube:
                    case LightTypeExtent.Rectangle:
                        lightUnit = LightUnit.Lumen;
                        intensity = areaIntensity;
                        break;
                }
                needsIntensityUpdate_1_0 = false;
            }
            if (m_Version <= 2)
            {
                // ShadowNearPlane have been move to HDRP as default legacy unity clamp it to 0.1 and we need to be able to go below that
                shadowNearPlane = m_Light.shadowNearPlane;
            }
            if (m_Version <= 3)
            {
                m_Light.renderingLayerMask = LightLayerToRenderingLayerMask((int)lightLayers, m_Light.renderingLayerMask);
            }

            m_Version = currentVersion;
            version = currentVersion;

#pragma warning restore 0618
        }

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
    }
}
