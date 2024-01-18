using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class SerializedHDLight : ISerializedLight
    {
        // HDRP specific properties
        public SerializedProperty spotInnerPercent;
        public SerializedProperty spotIESCutoffPercent;
        public SerializedProperty lightDimmer;
        public SerializedProperty fadeDistance;
        public SerializedProperty affectDiffuse;
        public SerializedProperty affectSpecular;
        public SerializedProperty nonLightmappedOnly;
        public SerializedProperty shapeWidth;
        public SerializedProperty shapeHeight;
        public SerializedProperty barnDoorAngle;
        public SerializedProperty barnDoorLength;
        public SerializedProperty aspectRatio;
        public SerializedProperty shapeRadius;
        public SerializedProperty maxSmoothness;
        public SerializedProperty applyRangeAttenuation;
        public SerializedProperty volumetricDimmer;
        public SerializedProperty volumetricFadeDistance;
        public SerializedProperty displayAreaLightEmissiveMesh;
        public SerializedProperty areaLightEmissiveMeshCastShadow;
        public SerializedProperty deportedAreaLightEmissiveMeshCastShadow;
        public SerializedProperty areaLightEmissiveMeshMotionVector;
        public SerializedProperty deportedAreaLightEmissiveMeshMotionVector;
        public SerializedProperty areaLightEmissiveMeshLayer;
        public SerializedProperty deportedAreaLightEmissiveMeshLayer;
        public SerializedProperty renderingLayerMask;
        public SerializedProperty shadowNearPlane;
        public SerializedProperty blockerSampleCount;
        public SerializedProperty filterSampleCount;
        public SerializedProperty minFilterSize;
        public SerializedProperty scaleForSoftness;
        public SerializedProperty areaLightCookie; // We can't use default light cookies because the cookie gets reset by some safety measure on C++ side... :/
        public SerializedProperty iesPoint;
        public SerializedProperty iesSpot;
        public SerializedProperty includeForRayTracing;
        public SerializedProperty includeForPathTracing;
        public SerializedProperty areaLightShadowCone;
        public SerializedProperty useCustomSpotLightShadowCone;
        public SerializedProperty customSpotLightShadowCone;
        public SerializedProperty useScreenSpaceShadows;
        public SerializedProperty interactsWithSky;
        public SerializedProperty angularDiameter;
        public SerializedProperty useRayTracedShadows;
        public SerializedProperty numRayTracingSamples;
        public SerializedProperty filterTracedShadow;
        public SerializedProperty filterSizeTraced;
        public SerializedProperty sunLightConeAngle;
        public SerializedProperty lightShadowRadius;
        public SerializedProperty semiTransparentShadow;
        public SerializedProperty colorShadow;
        public SerializedProperty distanceBasedFiltering;
        public SerializedProperty evsmExponent;
        public SerializedProperty evsmLightLeakBias;
        public SerializedProperty evsmVarianceBias;
        public SerializedProperty evsmBlurPasses;
        public SerializedProperty dirLightPCSSMaxPenumbraSize;
        public SerializedProperty dirLightPCSSMaxSamplingDistance;
        public SerializedProperty dirLightPCSSMinFilterSizeTexels;
        public SerializedProperty dirLightPCSSMinFilterMaxAngularDiameter;
        public SerializedProperty dirLightPCSSBlockerSearchAngularDiameter;
        public SerializedProperty dirLightPCSSBlockerSamplingClumpExponent;
        public SerializedProperty dirLightPCSSBlockerSampleCount;
        public SerializedProperty dirLightPCSSFilterSampleCount;

        // Celestial Body
        public SerializedProperty diameterOverride;
        public SerializedProperty diameterMultiplier;
        public SerializedProperty diameterMultiplerMode;
        public SerializedProperty distance;
        public SerializedProperty surfaceTexture;
        public SerializedProperty surfaceTint;
        public SerializedProperty shadingSource;
        public SerializedProperty sunLightOverride;
        public SerializedProperty sunColor;
        public SerializedProperty sunIntensity;
        public SerializedProperty phase;
        public SerializedProperty phaseRotation;
        public SerializedProperty earthshine;
        public SerializedProperty flareSize;
        public SerializedProperty flareFalloff;
        public SerializedProperty flareTint;
        public SerializedProperty flareMultiplier;

        // Improved moment shadows data
        public SerializedProperty lightAngle;
        public SerializedProperty kernelSize;
        public SerializedProperty maxDepthBias;

        // Editor stuff
        public SerializedProperty useOldInspector;
        public SerializedProperty showFeatures;
        public SerializedProperty useVolumetric;

        // Layers
        public SerializedProperty linkShadowLayers;
        public SerializedProperty lightlayersMask;

        // Shadow datas
        public SerializedProperty shadowDimmer;
        public SerializedProperty volumetricShadowDimmer;
        public SerializedProperty shadowFadeDistance;
        public SerializedScalableSettingValue contactShadows;
        public SerializedProperty rayTracedContactShadow;
        public SerializedProperty shadowTint;
        public SerializedProperty penumbraTint;
        public SerializedProperty shadowUpdateMode;
        public SerializedProperty shadowAlwaysDrawDynamic;
        public SerializedProperty shadowUpdateUponTransformChange;
        public SerializedScalableSettingValue shadowResolution;

        // Bias control
        public SerializedProperty slopeBias;
        public SerializedProperty normalBias;

        [Obsolete("This property has been deprecated. Use SerializedHDLight.settings.intensity instead.")]
        public SerializedProperty intensity => settings.intensity;

        private GameObject[] emissiveMeshes;

        public bool needUpdateAreaLightEmissiveMeshComponents = false;

        public SerializedObject serializedObject { get; }
        public SerializedObject serializedAdditionalDataObject { get; }

        public SerializedProperty lightLayer;
        private SerializedObject lightGameObject;

        //contain serialized property that are mainly used to draw inspector
        public LightEditor.Settings settings { get; }

        struct AreaLightEmissiveMeshEditionScope : System.IDisposable
        {
            SerializedHDLight m_Serialized;
            public AreaLightEmissiveMeshEditionScope(SerializedHDLight serialized)
            {
                m_Serialized = serialized;
                foreach (GameObject emissiveMesh in m_Serialized.emissiveMeshes)
                {
                    emissiveMesh.hideFlags &= ~HideFlags.NotEditable;
                }
                m_Serialized.areaLightEmissiveMeshCastShadow.serializedObject.Update();
            }

            void System.IDisposable.Dispose()
            {
                m_Serialized.areaLightEmissiveMeshCastShadow.serializedObject.ApplyModifiedProperties();
                foreach (GameObject emissiveMesh in m_Serialized.emissiveMeshes)
                {
                    emissiveMesh.hideFlags |= HideFlags.NotEditable;
                }
                m_Serialized.areaLightEmissiveMeshCastShadow.serializedObject.Update();
            }
        }

        struct AreaLightEmissiveMeshObjectEditionScope : System.IDisposable
        {
            SerializedHDLight m_Serialized;
            public AreaLightEmissiveMeshObjectEditionScope(SerializedHDLight serialized)
            {
                m_Serialized = serialized;
                foreach (GameObject emissiveMesh in m_Serialized.emissiveMeshes)
                {
                    emissiveMesh.hideFlags &= ~HideFlags.NotEditable;
                }
                m_Serialized.areaLightEmissiveMeshLayer.serializedObject.Update();
            }

            void System.IDisposable.Dispose()
            {
                m_Serialized.areaLightEmissiveMeshLayer.serializedObject.ApplyModifiedProperties();
                foreach (GameObject emissiveMesh in m_Serialized.emissiveMeshes)
                {
                    emissiveMesh.hideFlags |= HideFlags.NotEditable;
                }
                m_Serialized.areaLightEmissiveMeshLayer.serializedObject.Update();
            }
        }

        public struct AreaLightEmissiveMeshDrawScope : System.IDisposable
        {
            SerializedProperty[] m_Properties;
            bool m_OldEnableState;
            public AreaLightEmissiveMeshDrawScope(Rect rect, GUIContent label, bool enabler, params SerializedProperty[] properties)
            {
                m_Properties = properties;
                foreach (var property in m_Properties)
                    if (property != null)
                        EditorGUI.BeginProperty(rect, label, property);
                m_OldEnableState = GUI.enabled;
                GUI.enabled = enabler;
            }

            void System.IDisposable.Dispose()
            {
                GUI.enabled = m_OldEnableState;
                foreach (var property in m_Properties)
                    if (property != null)
                        EditorGUI.EndProperty();
            }
        }

        public void UpdateAreaLightEmissiveMeshCastShadow(UnityEngine.Rendering.ShadowCastingMode shadowCastingMode)
        {
            using (new AreaLightEmissiveMeshEditionScope(this))
            {
                areaLightEmissiveMeshCastShadow.intValue = (int)shadowCastingMode;
                if (deportedAreaLightEmissiveMeshCastShadow != null) //only possible while editing from prefab
                    deportedAreaLightEmissiveMeshCastShadow.intValue = (int)shadowCastingMode;
            }
        }

        public enum MotionVector
        {
            CameraMotionOnly = MotionVectorGenerationMode.Camera,
            PerObjectMotion = MotionVectorGenerationMode.Object,
            ForceNoMotion = MotionVectorGenerationMode.ForceNoMotion
        }

        public void UpdateAreaLightEmissiveMeshMotionVectorGeneration(MotionVector motionVectorGenerationMode)
        {
            using (new AreaLightEmissiveMeshEditionScope(this))
            {
                areaLightEmissiveMeshMotionVector.intValue = (int)motionVectorGenerationMode;
                if (deportedAreaLightEmissiveMeshMotionVector != null) //only possible while editing from prefab
                    deportedAreaLightEmissiveMeshMotionVector.intValue = (int)motionVectorGenerationMode;
            }
        }

        public void UpdateAreaLightEmissiveMeshLayer(int layer)
        {
            using (new AreaLightEmissiveMeshObjectEditionScope(this))
            {
                areaLightEmissiveMeshLayer.intValue = layer;
                if (deportedAreaLightEmissiveMeshLayer != null) //only possible while editing from prefab
                    deportedAreaLightEmissiveMeshLayer.intValue = layer;
            }
        }

        public SerializedHDLight(HDAdditionalLightData[] lightDatas, LightEditor.Settings settings)
        {
            serializedObject = new SerializedObject(lightDatas);
            this.settings = settings;

            using (var o = new PropertyFetcher<HDAdditionalLightData>(serializedObject))
            {
                spotInnerPercent = o.Find("m_InnerSpotPercent");
                spotIESCutoffPercent = o.Find("m_SpotIESCutoffPercent");
                lightDimmer = o.Find("m_LightDimmer");
                volumetricDimmer = o.Find("m_VolumetricDimmer");
                volumetricFadeDistance = o.Find("m_VolumetricFadeDistance");
                displayAreaLightEmissiveMesh = o.Find("m_DisplayAreaLightEmissiveMesh");
                fadeDistance = o.Find("m_FadeDistance");
                affectDiffuse = o.Find("m_AffectDiffuse");
                affectSpecular = o.Find("m_AffectSpecular");
                nonLightmappedOnly = o.Find("m_NonLightmappedOnly");
                shapeWidth = o.Find("m_ShapeWidth");
                shapeHeight = o.Find("m_ShapeHeight");
                barnDoorAngle = o.Find("m_BarnDoorAngle");
                barnDoorLength = o.Find("m_BarnDoorLength");
                aspectRatio = o.Find("m_AspectRatio");
                shapeRadius = o.Find("m_ShapeRadius");
                maxSmoothness = o.Find("m_MaxSmoothness");
                applyRangeAttenuation = o.Find("m_ApplyRangeAttenuation");
                shadowNearPlane = o.Find("m_ShadowNearPlane");
                blockerSampleCount = o.Find("m_BlockerSampleCount");
                filterSampleCount = o.Find("m_FilterSampleCount");
                minFilterSize = o.Find("m_MinFilterSize");
                scaleForSoftness = o.Find("m_SoftnessScale");
                areaLightCookie = o.Find("m_AreaLightCookie");
                iesPoint = o.Find("m_IESPoint");
                iesSpot = o.Find("m_IESSpot");
                includeForRayTracing = o.Find("m_IncludeForRayTracing");
                includeForPathTracing = o.Find("m_IncludeForPathTracing");
                areaLightShadowCone = o.Find("m_AreaLightShadowCone");
                useCustomSpotLightShadowCone = o.Find("m_UseCustomSpotLightShadowCone");
                customSpotLightShadowCone = o.Find("m_CustomSpotLightShadowCone");
                useScreenSpaceShadows = o.Find("m_UseScreenSpaceShadows");
                interactsWithSky = o.Find("m_InteractsWithSky");
                angularDiameter = o.Find("m_AngularDiameter");
                useRayTracedShadows = o.Find("m_UseRayTracedShadows");
                numRayTracingSamples = o.Find("m_NumRayTracingSamples");
                filterTracedShadow = o.Find("m_FilterTracedShadow");
                filterSizeTraced = o.Find("m_FilterSizeTraced");
                sunLightConeAngle = o.Find("m_SunLightConeAngle");
                lightShadowRadius = o.Find("m_LightShadowRadius");
                semiTransparentShadow = o.Find("m_SemiTransparentShadow");
                colorShadow = o.Find("m_ColorShadow");
                distanceBasedFiltering = o.Find("m_DistanceBasedFiltering");
                evsmExponent = o.Find("m_EvsmExponent");
                evsmVarianceBias = o.Find("m_EvsmVarianceBias");
                evsmLightLeakBias = o.Find("m_EvsmLightLeakBias");
                evsmBlurPasses = o.Find("m_EvsmBlurPasses");
                dirLightPCSSMaxPenumbraSize = o.Find("m_DirLightPCSSMaxPenumbraSize");
                dirLightPCSSMaxSamplingDistance = o.Find("m_DirLightPCSSMaxSamplingDistance");
                dirLightPCSSMinFilterSizeTexels = o.Find("m_DirLightPCSSMinFilterSizeTexels");
                dirLightPCSSMinFilterMaxAngularDiameter = o.Find("m_DirLightPCSSMinFilterMaxAngularDiameter");
                dirLightPCSSBlockerSearchAngularDiameter = o.Find("m_DirLightPCSSBlockerSearchAngularDiameter");
                dirLightPCSSBlockerSamplingClumpExponent = o.Find("m_DirLightPCSSBlockerSamplingClumpExponent");
                dirLightPCSSBlockerSampleCount = o.Find("m_DirLightPCSSBlockerSampleCount");
                dirLightPCSSFilterSampleCount = o.Find("m_DirLightPCSSFilterSampleCount");

                // Celestial Body
                diameterOverride = o.Find(x => x.diameterOverride);
                diameterMultiplier = o.Find(x => x.diameterMultiplier);
                diameterMultiplerMode = o.Find(x => x.diameterMultiplerMode);

                distance = o.Find("m_Distance");

                surfaceTexture = o.Find(x => x.surfaceTexture);
                surfaceTint = o.Find(x => x.surfaceTint);

                shadingSource = o.Find(x => x.celestialBodyShadingSource);
                sunLightOverride = o.Find(x => x.sunLightOverride);

                sunColor = o.Find(x => x.sunColor);
                sunIntensity = o.Find(x => x.sunIntensity);
                phase = o.Find(x => x.moonPhase);
                phaseRotation = o.Find(x => x.moonPhaseRotation);
                earthshine = o.Find(x => x.earthshine);

                flareSize = o.Find(x => x.flareSize);
                flareFalloff = o.Find(x => x.flareFalloff);
                flareTint = o.Find(x => x.flareTint);
                flareMultiplier = o.Find(x => x.flareMultiplier);

                // Moment light
                lightAngle = o.Find("m_LightAngle");
                kernelSize = o.Find("m_KernelSize");
                maxDepthBias = o.Find("m_MaxDepthBias");

                // Editor stuff
                useOldInspector = o.Find("useOldInspector");
                showFeatures = o.Find("featuresFoldout");
                useVolumetric = o.Find("useVolumetric");
                renderingLayerMask = settings.renderingLayerMask;

                // Layers
                linkShadowLayers = o.Find("m_LinkShadowLayers");
                lightlayersMask = o.Find("m_LightlayersMask");

                // Shadow datas:
                shadowDimmer = o.Find("m_ShadowDimmer");
                volumetricShadowDimmer = o.Find("m_VolumetricShadowDimmer");
                shadowFadeDistance = o.Find("m_ShadowFadeDistance");
                contactShadows = new SerializedScalableSettingValue(o.Find((HDAdditionalLightData l) => l.useContactShadow));
                rayTracedContactShadow = o.Find("m_RayTracedContactShadow");
                shadowTint = o.Find("m_ShadowTint");
                penumbraTint = o.Find("m_PenumbraTint");
                shadowUpdateMode = o.Find("m_ShadowUpdateMode");
                shadowAlwaysDrawDynamic = o.Find("m_AlwaysDrawDynamicShadows");
                shadowUpdateUponTransformChange = o.Find("m_UpdateShadowOnLightMovement");
                shadowResolution = new SerializedScalableSettingValue(o.Find((HDAdditionalLightData l) => l.shadowResolution));

                slopeBias = o.Find("m_SlopeBias");
                normalBias = o.Find("m_NormalBias");

                // emission mesh
                areaLightEmissiveMeshCastShadow = o.Find("m_AreaLightEmissiveMeshShadowCastingMode");
                areaLightEmissiveMeshMotionVector = o.Find("m_AreaLightEmissiveMeshMotionVectorGenerationMode");
                areaLightEmissiveMeshLayer = o.Find("m_AreaLightEmissiveMeshLayer");
            }

            RefreshEmissiveMeshReference();

            lightGameObject = new SerializedObject(serializedObject.targetObjects.Select(ld => ((HDAdditionalLightData)ld).gameObject).ToArray());
            lightLayer = lightGameObject.FindProperty("m_Layer");
        }

        void RefreshEmissiveMeshReference()
        {
            IEnumerable<MeshRenderer> meshRenderers = serializedObject.targetObjects.Select(ld => ((HDAdditionalLightData)ld).emissiveMeshRenderer).Where(mr => mr != null);
            emissiveMeshes = meshRenderers.Select(mr => mr.gameObject).ToArray();
            if (meshRenderers.Count() > 0)
            {
                SerializedObject meshRendererSerializedObject = new SerializedObject(meshRenderers.ToArray());
                deportedAreaLightEmissiveMeshCastShadow = meshRendererSerializedObject.FindProperty("m_CastShadows");
                deportedAreaLightEmissiveMeshMotionVector = meshRendererSerializedObject.FindProperty("m_MotionVectors");
                SerializedObject gameObjectSerializedObject = new SerializedObject(emissiveMeshes);
                deportedAreaLightEmissiveMeshLayer = gameObjectSerializedObject.FindProperty("m_Layer");
            }
            else
                deportedAreaLightEmissiveMeshCastShadow = deportedAreaLightEmissiveMeshMotionVector = deportedAreaLightEmissiveMeshLayer = null;
        }

        public void FetchAreaLightEmissiveMeshComponents()
        {
            // Only apply display emissive mesh changes or type change as only ones that can happens
            // Plus perhaps if we update deportedAreaLightEmissiveMeshMotionVector.serializedObject,
            // it can no longuer have target here as refreshed only below
            ApplyInternal(withDeportedEmissiveMeshData: false);

            foreach (HDAdditionalLightData target in serializedObject.targetObjects)
                target.UpdateAreaLightEmissiveMesh();

            RefreshEmissiveMeshReference();
            Update();
        }

        public void Update()
        {
            // Case 1182968
            // For some reasons, the is different cache is not updated while we actually have different
            // values for shadowResolution.level
            // So we force the update here as a workaround
            serializedObject.SetIsDifferentCacheDirty();

            serializedObject.Update();
            settings.Update();

            lightGameObject.Update();
            if (deportedAreaLightEmissiveMeshMotionVector.IsTargetAlive())
                deportedAreaLightEmissiveMeshMotionVector?.serializedObject.Update();
            if (deportedAreaLightEmissiveMeshLayer.IsTargetAlive())
                deportedAreaLightEmissiveMeshLayer?.serializedObject.Update();
        }

        void ApplyInternal(bool withDeportedEmissiveMeshData)
        {
            serializedObject.ApplyModifiedProperties();
            settings.ApplyModifiedProperties();
            if (withDeportedEmissiveMeshData)
            {
                if (deportedAreaLightEmissiveMeshMotionVector.IsTargetAlive())
                    deportedAreaLightEmissiveMeshMotionVector?.serializedObject.ApplyModifiedProperties();
                if (deportedAreaLightEmissiveMeshLayer.IsTargetAlive())
                    deportedAreaLightEmissiveMeshLayer?.serializedObject.ApplyModifiedProperties();
            }
        }

        public void Apply() => ApplyInternal(withDeportedEmissiveMeshData: true);

        public bool HasMultipleLightTypes(Editor owner)
        {
            return owner.serializedObject.FindProperty("m_Type").hasMultipleDifferentValues;
        }
    }
}
