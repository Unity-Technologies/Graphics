using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class SerializedHDLight
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
        public SerializedProperty spotLightShape;
        public SerializedProperty shapeWidth;
        public SerializedProperty shapeHeight;
        public SerializedProperty barnDoorAngle;
        public SerializedProperty barnDoorLength;
        public SerializedProperty aspectRatio;
        public SerializedProperty shapeRadius;
        public SerializedProperty maxSmoothness;
        public SerializedProperty applyRangeAttenuation;
        public SerializedProperty volumetricDimmer;
        public SerializedProperty lightUnit;
        public SerializedProperty displayAreaLightEmissiveMesh;
        public SerializedProperty areaLightEmissiveMeshCastShadow;
        public SerializedProperty deportedAreaLightEmissiveMeshCastShadow;
        public SerializedProperty areaLightEmissiveMeshMotionVector;
        public SerializedProperty deportedAreaLightEmissiveMeshMotionVector;
        public SerializedProperty renderingLayerMask;
        public SerializedProperty shadowNearPlane;
        public SerializedProperty blockerSampleCount;
        public SerializedProperty filterSampleCount;
        public SerializedProperty minFilterSize;
        public SerializedProperty scaleForSoftness;
        public SerializedProperty areaLightCookie;   // We can't use default light cookies because the cookie gets reset by some safety measure on C++ side... :/
        public SerializedProperty areaLightShadowCone;
        public SerializedProperty useCustomSpotLightShadowCone;
        public SerializedProperty customSpotLightShadowCone;
        public SerializedProperty useScreenSpaceShadows;
        public SerializedProperty interactsWithSky;
        public SerializedProperty angularDiameter;
        public SerializedProperty flareSize;
        public SerializedProperty flareTint;
        public SerializedProperty flareFalloff;
        public SerializedProperty surfaceTexture;
        public SerializedProperty surfaceTint;
        public SerializedProperty distance;
        public SerializedProperty useRayTracedShadows;
        public SerializedProperty numRayTracingSamples;
        public SerializedProperty filterTracedShadow;
        public SerializedProperty filterSizeTraced;
        public SerializedProperty sunLightConeAngle;
        public SerializedProperty lightShadowRadius;
        public SerializedProperty semiTransparentShadow;
        public SerializedProperty colorShadow;
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
        public SerializedProperty rayTracedContactShadow;
        public SerializedProperty shadowTint;
        public SerializedProperty penumbraTint;
        public SerializedProperty shadowUpdateMode;
        public SerializedScalableSettingValue shadowResolution;
        
        // Bias control
        public SerializedProperty slopeBias;
        public SerializedProperty normalBias;

        private SerializedProperty pointLightHDType;
        private SerializedProperty areaLightShapeProperty;

        private IEnumerable<GameObject> emissiveMeshes;

        public bool needUpdateAreaLightEmissiveMeshComponents = false;

        public SerializedObject serializedObject;

        //contain serialized property that are mainly used to draw inspector
        public LightEditor.Settings settings;

        //type is converted on the fly each time so we cannot have SerializedProperty on it
        public HDLightType type
        {
            get => haveMultipleTypeValue
                ? (HDLightType)(-1) //as serialize property on enum when mixed value state happens
                : (serializedObject.targetObjects[0] as HDAdditionalLightData).type;
            set
            {
                //Note: type is split in both component
                var undoObjects = serializedObject.targetObjects.SelectMany((Object x) => new Object[] { x, (x as HDAdditionalLightData).legacyLight }).ToArray();
                Undo.RecordObjects(undoObjects, "Change light type");
                var objects = serializedObject.targetObjects;
                for (int index = 0; index < objects.Length; ++index)
                    (objects[index] as HDAdditionalLightData).type = value;
                serializedObject.Update();
            }
        }

        bool haveMultipleTypeValue
        {
            get
            {
                var objects = serializedObject.targetObjects;
                HDLightType value = (objects[0] as HDAdditionalLightData).type;
                for (int index = 1; index < objects.Length; ++index)
                    if (value != (objects[index] as HDAdditionalLightData).type)
                        return true;
                return false;
            }
        }

        // This scope is here mainly to keep pointLightHDType isolated
        public struct LightTypeEditionScope : System.IDisposable
        {
            public LightTypeEditionScope(Rect rect, GUIContent label, SerializedHDLight serialized)
            {
                EditorGUI.BeginProperty(rect, label, serialized.pointLightHDType);
                EditorGUI.BeginProperty(rect, label, serialized.settings.lightType);
            }

            void System.IDisposable.Dispose()
            {
                EditorGUI.EndProperty();
                EditorGUI.EndProperty();
            }
        }

        //areaLightShape need to be accessed by its property to always report modification in the right way
        public AreaLightShape areaLightShape
        {
            get => haveMultipleAreaLightShapeValue
                ? (AreaLightShape)(-1) //as serialize property on enum when mixed value state happens
                : (serializedObject.targetObjects[0] as HDAdditionalLightData).areaLightShape;
            set
            {
                //Note: Disc is actually changing legacyLight.type to Disc
                var undoObjects = serializedObject.targetObjects.SelectMany((Object x) => new Object[] { x, (x as HDAdditionalLightData).legacyLight }).ToArray();
                Undo.RecordObjects(undoObjects, "Change light area shape");
                var objects = serializedObject.targetObjects;
                for (int index = 0; index < objects.Length; ++index)
                    (objects[index] as HDAdditionalLightData).areaLightShape = value;
                serializedObject.Update();
            }
        }

        bool haveMultipleAreaLightShapeValue
        {
            get
            {
                var objects = serializedObject.targetObjects;
                AreaLightShape value = (objects[0] as HDAdditionalLightData).areaLightShape;
                for (int index = 1; index < objects.Length; ++index)
                    if (value != (objects[index] as HDAdditionalLightData).areaLightShape)
                        return true;
                return false;
            }
        }

        // This scope is here mainly to keep pointLightHDType and areaLightShapeProperty isolated
        public struct AreaLightShapeEditionScope : System.IDisposable
        {
            public AreaLightShapeEditionScope(Rect rect, GUIContent label, SerializedHDLight serialized)
            {
                EditorGUI.BeginProperty(rect, label, serialized.pointLightHDType);
                EditorGUI.BeginProperty(rect, label, serialized.settings.lightType);
                EditorGUI.BeginProperty(rect, label, serialized.areaLightShapeProperty);
            }

            void System.IDisposable.Dispose()
            {
                EditorGUI.EndProperty();
                EditorGUI.EndProperty();
                EditorGUI.EndProperty();
            }
        }

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

        public struct AreaLightEmissiveMeshDrawScope : System.IDisposable
        {
            int propertyCount;
            bool oldEnableState;
            public AreaLightEmissiveMeshDrawScope(Rect rect, GUIContent label, bool enabler, params SerializedProperty[] properties)
            {
                propertyCount = properties.Count(p => p != null);
                foreach (var property in properties)
                    if (property != null)
                        EditorGUI.BeginProperty(rect, label, property);
                oldEnableState = GUI.enabled;
                GUI.enabled = enabler;
            }

            void System.IDisposable.Dispose()
            {
                GUI.enabled = oldEnableState;
                for (int i = 0; i < propertyCount; ++i)
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

        public SerializedHDLight(HDAdditionalLightData[] lightDatas, LightEditor.Settings settings)
        {
            serializedObject = new SerializedObject(lightDatas);
            this.settings = settings;

            using (var o = new PropertyFetcher<HDAdditionalLightData>(serializedObject))
            {
                intensity = o.Find("m_Intensity");
                enableSpotReflector = o.Find("m_EnableSpotReflector");
                luxAtDistance = o.Find("m_LuxAtDistance");
                spotInnerPercent = o.Find("m_InnerSpotPercent");
                lightDimmer = o.Find("m_LightDimmer");
                volumetricDimmer = o.Find("m_VolumetricDimmer");
                lightUnit = o.Find("m_LightUnit");
                displayAreaLightEmissiveMesh = o.Find("m_DisplayAreaLightEmissiveMesh");
                fadeDistance = o.Find("m_FadeDistance");
                affectDiffuse = o.Find("m_AffectDiffuse");
                affectSpecular = o.Find("m_AffectSpecular");
                nonLightmappedOnly = o.Find("m_NonLightmappedOnly");
                spotLightShape = o.Find("m_SpotLightShape");
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
                areaLightShadowCone = o.Find("m_AreaLightShadowCone");
                useCustomSpotLightShadowCone = o.Find("m_UseCustomSpotLightShadowCone");
                customSpotLightShadowCone = o.Find("m_CustomSpotLightShadowCone");
                useScreenSpaceShadows = o.Find("m_UseScreenSpaceShadows");
                interactsWithSky = o.Find("m_InteractsWithSky");
                angularDiameter = o.Find("m_AngularDiameter");
                flareSize = o.Find("m_FlareSize");
                flareFalloff = o.Find("m_FlareFalloff");
                flareTint = o.Find("m_FlareTint");
                surfaceTexture = o.Find("m_SurfaceTexture");
                surfaceTint = o.Find("m_SurfaceTint");
                distance = o.Find("m_Distance");
                useRayTracedShadows = o.Find("m_UseRayTracedShadows");
                numRayTracingSamples = o.Find("m_NumRayTracingSamples");
                filterTracedShadow = o.Find("m_FilterTracedShadow");
                filterSizeTraced = o.Find("m_FilterSizeTraced");
                sunLightConeAngle = o.Find("m_SunLightConeAngle");
                lightShadowRadius = o.Find("m_LightShadowRadius");
                semiTransparentShadow = o.Find("m_SemiTransparentShadow");
                colorShadow = o.Find("m_ColorShadow");
                evsmExponent = o.Find("m_EvsmExponent");
                evsmVarianceBias = o.Find("m_EvsmVarianceBias");
                evsmLightLeakBias = o.Find("m_EvsmLightLeakBias");
                evsmBlurPasses = o.Find("m_EvsmBlurPasses");

                // Moment light
                lightAngle = o.Find("m_LightAngle");
                kernelSize = o.Find("m_KernelSize");
                maxDepthBias = o.Find("m_MaxDepthBias");

                // Editor stuff
                useOldInspector = o.Find("useOldInspector");
                showFeatures = o.Find("featuresFoldout");
                showAdditionalSettings = o.Find("showAdditionalSettings");
                useVolumetric = o.Find("useVolumetric");
                renderingLayerMask = settings.renderingLayerMask;

                // Layers
                linkLightLayers = o.Find("m_LinkShadowLayers");
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
                shadowResolution = new SerializedScalableSettingValue(o.Find((HDAdditionalLightData l) => l.shadowResolution));

				slopeBias = o.Find("m_SlopeBias");
                normalBias = o.Find("m_NormalBias");

                // private references for prefab handling
                pointLightHDType = o.Find("m_PointlightHDType");
                areaLightShapeProperty = o.Find("m_AreaLightShape");

                // emission mesh
                areaLightEmissiveMeshCastShadow = o.Find("m_AreaLightEmissiveMeshShadowCastingMode");
                areaLightEmissiveMeshMotionVector = o.Find("m_AreaLightEmissiveMeshMotionVectorGenerationMode");
            }

            RefreshEmissiveMeshReference();
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
            }
            else
                deportedAreaLightEmissiveMeshCastShadow = deportedAreaLightEmissiveMeshMotionVector = null;
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
        }

        void ApplyInternal(bool withDeportedEmissiveMeshData)
        {
            serializedObject.ApplyModifiedProperties();
            settings.ApplyModifiedProperties();
            if (withDeportedEmissiveMeshData)
                deportedAreaLightEmissiveMeshMotionVector?.serializedObject.ApplyModifiedProperties();
        }

        public void Apply() => ApplyInternal(withDeportedEmissiveMeshData: true);
    }
}
