using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    using LightShape = HDLightUI.LightShape;
    internal class SerializedHDLight
    {
        public sealed class SerializedLightData
        {
            public SerializedProperty intensity;
            public SerializedProperty enableSpotReflector;
            public SerializedProperty luxAtDistance;
            public SerializedProperty spotInnerPercent;
            public SerializedProperty lightDimmer;
            public SerializedProperty fadeDistance;
            public SerializedProperty affectDiffuse;
            public SerializedProperty affectSpecular;
            public SerializedProperty nonLightmappedOnly;
            public SerializedProperty lightTypeExtent;
            public SerializedProperty spotLightShape;
            public SerializedProperty shapeWidth;
            public SerializedProperty shapeHeight;
            public SerializedProperty aspectRatio;
            public SerializedProperty shapeRadius;
            public SerializedProperty maxSmoothness;
            public SerializedProperty applyRangeAttenuation;
            public SerializedProperty volumetricDimmer;
            public SerializedProperty lightUnit;
            public SerializedProperty displayAreaLightEmissiveMesh;
            public SerializedProperty renderingLayerMask;
            public SerializedProperty shadowNearPlane;
            public SerializedProperty shadowSoftness;
            public SerializedProperty blockerSampleCount;
            public SerializedProperty filterSampleCount;
            public SerializedProperty minFilterSize;
            public SerializedProperty areaLightCookie;   // We can't use default light cookies because the cookie gets reset by some safety measure on C++ side... :/
            public SerializedProperty areaLightShadowCone;
            public SerializedProperty useCustomSpotLightShadowCone;
            public SerializedProperty customSpotLightShadowCone;
            public SerializedProperty useScreenSpaceShadows;
            public SerializedProperty interactsWithSky;
            public SerializedProperty angularDiameter;
            public SerializedProperty distance;
#if ENABLE_RAYTRACING
            public SerializedProperty useRayTracedShadows;
            public SerializedProperty numRayTracingSamples;
            public SerializedProperty filterTracedShadow;
            public SerializedProperty filterSizeTraced;
            public SerializedProperty sunLightConeAngle;
            public SerializedProperty lightShadowRadius;
#endif
            public SerializedProperty evsmExponent;
            public SerializedProperty evsmLightLeakBias;
            public SerializedProperty evsmVarianceBias;
            public SerializedProperty evsmBlurPasses;

            // Improved moment shadows data
            public SerializedProperty lightAngle;
            public SerializedProperty kernelSize;
            public SerializedProperty maxDepthBias;

            // Editor stuff
            public SerializedProperty useOldInspector;
            public SerializedProperty showFeatures;
            public SerializedProperty showAdditionalSettings;
            public SerializedProperty useVolumetric;

            // Layers
            public SerializedProperty linkLightLayers;
            public SerializedProperty lightlayersMask;

            // Shadow datas
            public SerializedProperty shadowDimmer;
            public SerializedProperty volumetricShadowDimmer;
            public SerializedProperty shadowFadeDistance;
            public SerializedScalableSettingValue contactShadows;
            public SerializedProperty shadowTint;
            public SerializedProperty shadowUpdateMode;
            public SerializedScalableSettingValue shadowResolution;

            // Bias control
            public SerializedProperty constantBias;

            public SerializedProperty normalBias;
        }

        public bool needUpdateAreaLightEmissiveMeshComponents = false;

        public SerializedObject serializedLightDatas;

        public SerializedLightData serializedLightData;

        //contain serialized property that are mainly used to draw inspector
        public LightEditor.Settings settings;

        // Used for UI only; the processing code must use LightTypeExtent and LightType
        public LightShape editorLightShape;

        public SerializedHDLight(HDAdditionalLightData[] lightDatas, LightEditor.Settings settings)
        {
            serializedLightDatas = new SerializedObject(lightDatas);
            this.settings = settings;

            using (var o = new PropertyFetcher<HDAdditionalLightData>(serializedLightDatas))
                serializedLightData = new SerializedLightData
                {
                    intensity = o.Find("m_Intensity"),
                    enableSpotReflector = o.Find("m_EnableSpotReflector"),
                    luxAtDistance = o.Find("m_LuxAtDistance"),
                    spotInnerPercent = o.Find("m_InnerSpotPercent"),
                    lightDimmer = o.Find("m_LightDimmer"),
                    volumetricDimmer = o.Find("m_VolumetricDimmer"),
                    lightUnit = o.Find("m_LightUnit"),
                    displayAreaLightEmissiveMesh = o.Find("m_DisplayAreaLightEmissiveMesh"),
                    fadeDistance = o.Find("m_FadeDistance"),
                    affectDiffuse = o.Find("m_AffectDiffuse"),
                    affectSpecular = o.Find("m_AffectSpecular"),
                    nonLightmappedOnly = o.Find("m_NonLightmappedOnly"),
                    lightTypeExtent = o.Find("m_LightTypeExtent"),
                    spotLightShape = o.Find("m_SpotLightShape"), // WTF?
                    shapeWidth = o.Find("m_ShapeWidth"),
                    shapeHeight = o.Find("m_ShapeHeight"),
                    aspectRatio = o.Find("m_AspectRatio"),
                    shapeRadius = o.Find("m_ShapeRadius"),
                    maxSmoothness = o.Find("m_MaxSmoothness"),
                    applyRangeAttenuation = o.Find("m_ApplyRangeAttenuation"),
                    shadowNearPlane = o.Find("m_ShadowNearPlane"),
                    shadowSoftness = o.Find("m_ShadowSoftness"),
                    blockerSampleCount = o.Find("m_BlockerSampleCount"),
                    filterSampleCount = o.Find("m_FilterSampleCount"),
                    minFilterSize = o.Find("m_MinFilterSize"),
                    areaLightCookie = o.Find("m_AreaLightCookie"),
                    areaLightShadowCone = o.Find("m_AreaLightShadowCone"),
                    useCustomSpotLightShadowCone = o.Find("m_UseCustomSpotLightShadowCone"),
                    customSpotLightShadowCone = o.Find("m_CustomSpotLightShadowCone"),
                    useScreenSpaceShadows = o.Find("m_UseScreenSpaceShadows"),
                    interactsWithSky = o.Find("m_InteractsWithSky"),
                    angularDiameter = o.Find("m_AngularDiameter"),
                    distance = o.Find("m_Distance"),
#if ENABLE_RAYTRACING
                    useRayTracedShadows = o.Find("m_UseRayTracedShadows"),
                    numRayTracingSamples = o.Find("m_NumRayTracingSamples"),
                    filterTracedShadow = o.Find("m_FilterTracedShadow"),
                    filterSizeTraced = o.Find("m_FilterSizeTraced"),
                    sunLightConeAngle = o.Find("m_SunLightConeAngle"),
                    lightShadowRadius = o.Find("m_LightShadowRadius"),
#endif
                    evsmExponent = o.Find("m_EvsmExponent"),
                    evsmVarianceBias = o.Find("m_EvsmVarianceBias"),
                    evsmLightLeakBias = o.Find("m_EvsmLightLeakBias"),
                    evsmBlurPasses = o.Find("m_EvsmBlurPasses"),

                    // Moment light
                    lightAngle = o.Find("m_LightAngle"),
                    kernelSize = o.Find("m_KernelSize"),
                    maxDepthBias = o.Find("m_MaxDepthBias"),

                    // Editor stuff
                    useOldInspector = o.Find("useOldInspector"),
                    showFeatures = o.Find("featuresFoldout"),
                    showAdditionalSettings = o.Find("showAdditionalSettings"),
                    useVolumetric = o.Find("useVolumetric"),
                    renderingLayerMask = settings.renderingLayerMask,

                    // Layers
                    linkLightLayers = o.Find("m_LinkShadowLayers"),
                    lightlayersMask = o.Find("m_LightlayersMask"),

                    // Shadow datas:
                    shadowDimmer = o.Find("m_ShadowDimmer"),
                    volumetricShadowDimmer = o.Find("m_VolumetricShadowDimmer"),
                    shadowFadeDistance = o.Find("m_ShadowFadeDistance"),
                    contactShadows = new SerializedScalableSettingValue(o.Find((HDAdditionalLightData l) => l.useContactShadow)),
                    shadowTint = o.Find("m_ShadowTint"),
                    shadowUpdateMode = o.Find("m_ShadowUpdateMode"),
                    shadowResolution = new SerializedScalableSettingValue(o.Find((HDAdditionalLightData l) => l.shadowResolution)),

                    constantBias = o.Find("m_ConstantBias"),
                    normalBias = o.Find("m_NormalBias"),
                };
        }

        public void Update()
        {
            // Case 1182968
            // For some reasons, the is different cache is not updated while we actually have different
            // values for shadowResolution.level
            // So we force the update here as a workaround
            serializedLightDatas.SetIsDifferentCacheDirty();

            serializedLightDatas.Update();
            settings.Update();

            ResolveLightShape();
        }

        public void Apply()
        {
            serializedLightDatas.ApplyModifiedProperties();
            settings.ApplyModifiedProperties();
        }

        void ResolveLightShape()
        {
            var type = settings.lightType;

            // Special case for multi-selection: don't resolve light shape or it'll corrupt lights
            if (type.hasMultipleDifferentValues
                || serializedLightData.lightTypeExtent.hasMultipleDifferentValues)
            {
                editorLightShape = (LightShape)(-1);
                return;
            }

            editorLightShape = ResolveLightShape(
                (LightTypeExtent) serializedLightData.lightTypeExtent.enumValueIndex,
                (LightType)type.enumValueIndex
            );
        }

        internal static LightShape ResolveLightShape(LightTypeExtent typeExtent, LightType type)
        {
            switch (typeExtent)
            {
                case LightTypeExtent.Punctual:
                    switch (type)
                    {
                        case LightType.Directional:
                            return LightShape.Directional;
                        case LightType.Point:
                            return LightShape.Point;
                        case LightType.Spot:
                            return LightShape.Spot;
                    }
                    break;
                case LightTypeExtent.Rectangle:
                    return LightShape.Rectangle;
                case LightTypeExtent.Tube:
                    return LightShape.Tube;
            }
            throw new Exception("Unknown light type");
        }
    }
}
