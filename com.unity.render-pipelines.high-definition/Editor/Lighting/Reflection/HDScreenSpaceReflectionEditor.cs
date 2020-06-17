using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ScreenSpaceReflection))]
    class HDScreenSpaceReflectionEditor : VolumeComponentWithQualityEditor
    {
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_RayTracing;

        // Shared data
        SerializedDataParameter m_MinSmoothness;
        SerializedDataParameter m_SmoothnessFadeStart;
        SerializedDataParameter m_ReflectSky;

        // SSR Only
        SerializedDataParameter m_ScreenFadeDistance;
        SerializedDataParameter m_RayMaxIterations;
        SerializedDataParameter m_DepthBufferThickness;

        // Ray Tracing
        SerializedDataParameter m_LayerMask;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_ClampValue;
        SerializedDataParameter m_Denoise;
        SerializedDataParameter m_DenoiserRadius;
        SerializedDataParameter m_Mode;

        // Performance
        SerializedDataParameter m_UpscaleRadius;
        SerializedDataParameter m_FullResolution;

        // Quality
        SerializedDataParameter m_SampleCount;
        SerializedDataParameter m_BounceCount;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<ScreenSpaceReflection>(serializedObject);
            m_Enable                        = Unpack(o.Find(x => x.enabled));
            m_RayTracing                    = Unpack(o.Find(x => x.rayTracing));

            // Shared data
            m_MinSmoothness                 = Unpack(o.Find(x => x.minSmoothness));
            m_SmoothnessFadeStart           = Unpack(o.Find(x => x.smoothnessFadeStart));
            m_ReflectSky                    = Unpack(o.Find(x => x.reflectSky));

            // SSR Data
            m_DepthBufferThickness          = Unpack(o.Find(x => x.depthBufferThickness));
            m_RayMaxIterations              = Unpack(o.Find(x => x.rayMaxIterations));
            m_ScreenFadeDistance            = Unpack(o.Find(x => x.screenFadeDistance));

            // Generic ray tracing
            m_LayerMask                     = Unpack(o.Find(x => x.layerMask));
            m_RayLength                     = Unpack(o.Find(x => x.rayLength));
            m_ClampValue                    = Unpack(o.Find(x => x.clampValue));
            m_Denoise                       = Unpack(o.Find(x => x.denoise));
            m_DenoiserRadius                = Unpack(o.Find(x => x.denoiserRadius));
            m_Mode                          = Unpack(o.Find(x => x.mode));

            // Performance
            m_UpscaleRadius                 = Unpack(o.Find(x => x.upscaleRadius));
            m_FullResolution                = Unpack(o.Find(x => x.fullResolution));

            // Quality
            m_SampleCount                   = Unpack(o.Find(x => x.sampleCount));
            m_BounceCount                   = Unpack(o.Find(x => x.bounceCount));
        }

        static public readonly GUIContent k_RayTracingText = EditorGUIUtility.TrTextContent("Ray Tracing (Preview)", "Enable ray traced reflections.");
        static public readonly GUIContent k_ReflectSkyText = EditorGUIUtility.TrTextContent("Reflect Sky", "When enabled, SSR handles sky reflection.");
        static public readonly GUIContent k_LayerMaskText = EditorGUIUtility.TrTextContent("Layer Mask", "Layer mask used to include the objects for screen space reflection.");
        static public readonly GUIContent k_MinimumSmoothnessText = EditorGUIUtility.TrTextContent("Minimum Smoothness", "Controls the smoothness value at which HDRP activates SSR and the smoothness-controlled fade out stops.");
        static public readonly GUIContent k_SmoothnessFadeStartText = EditorGUIUtility.TrTextContent("Smoothness Fade Start", "Controls the smoothness value at which the smoothness-controlled fade out starts. The fade is in the range [Min Smoothness, Smoothness Fade Start].");
        static public readonly GUIContent k_ScreenFaceDistanceText = EditorGUIUtility.TrTextContent("Screen Edge Fade Distance", "Controls the distance at which HDRP fades out SSR near the edge of the screen.");
        static public readonly GUIContent k_DepthBufferThicknessText = EditorGUIUtility.TrTextContent("Object Thickness", "Controls the typical thickness of objects the reflection rays may pass behind.");
        static public readonly GUIContent k_RayMaxIterationsText = EditorGUIUtility.TrTextContent("Max Ray Steps", "Sets the maximum number of steps HDRP uses for raytracing. Affects both correctness and performance.");
        static public readonly GUIContent k_RayLengthText = EditorGUIUtility.TrTextContent("Ray Length", "Controls the length of reflection rays.");
        static public readonly GUIContent k_ClampValueText = EditorGUIUtility.TrTextContent("Clamp Value", "Clamps the exposed intensity.");
        static public readonly GUIContent k_SampleCountText = EditorGUIUtility.TrTextContent("Sample Count", "Number of samples for reflections.");
        static public readonly GUIContent k_BounceCountText = EditorGUIUtility.TrTextContent("Bounce Count", "Number of bounces for reflection rays.");
        static public readonly GUIContent k_ModeText = EditorGUIUtility.TrTextContent("Mode", "Controls which version of the effect should be used.");
        static public readonly GUIContent k_DenoiseText = EditorGUIUtility.TrTextContent("Denoise", "Enable denoising on the ray traced reflections.");
        static public readonly GUIContent k_UpscaleRadiusText = EditorGUIUtility.TrTextContent("Upscale Radius", "Controls the size of the upscale radius.");
        static public readonly GUIContent k_FullResolutionText = EditorGUIUtility.TrTextContent("Full Resolution", "Enables full resolution mode.");
        static public readonly GUIContent k_DenoiseRadiusText = EditorGUIUtility.TrTextContent("Denoiser Radius", "Controls the radius of reflection denoiser.");

        void RayTracingQualityModeGUI()
        {
            PropertyField(m_MinSmoothness, k_MinimumSmoothnessText);
            PropertyField(m_SmoothnessFadeStart, k_SmoothnessFadeStartText);
            m_SmoothnessFadeStart.value.floatValue  = Mathf.Max(m_MinSmoothness.value.floatValue, m_SmoothnessFadeStart.value.floatValue);
            PropertyField(m_RayLength, k_RayLengthText);
            PropertyField(m_ClampValue, k_ClampValueText);
            PropertyField(m_SampleCount, k_SampleCountText);
            PropertyField(m_BounceCount, k_BounceCountText);
            PropertyField(m_Denoise, k_DenoiseText);
            {
                EditorGUI.indentLevel++;
                PropertyField(m_DenoiserRadius, k_DenoiseRadiusText);
                EditorGUI.indentLevel--;
            }
        }


        void RayTracingPerformanceModeGUI()
        {
            base.OnInspectorGUI();
            GUI.enabled = useCustomValue;
            {
                EditorGUI.indentLevel++;
                PropertyField(m_MinSmoothness, k_MinimumSmoothnessText);
                PropertyField(m_SmoothnessFadeStart, k_SmoothnessFadeStartText);
                m_SmoothnessFadeStart.value.floatValue  = Mathf.Max(m_MinSmoothness.value.floatValue, m_SmoothnessFadeStart.value.floatValue);
                PropertyField(m_RayLength, k_RayLengthText);
                PropertyField(m_ClampValue, k_ClampValueText);
                PropertyField(m_UpscaleRadius, k_UpscaleRadiusText);
                PropertyField(m_FullResolution, k_FullResolutionText);
                PropertyField(m_Denoise, k_DenoiseText);
                {
                    EditorGUI.indentLevel++;
                    PropertyField(m_DenoiserRadius, k_DenoiseRadiusText);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            GUI.enabled = true;
        }

        void RayTracedReflectionGUI()
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            PropertyField(m_ReflectSky, k_ReflectSkyText);
            PropertyField(m_LayerMask, k_LayerMaskText);

            if (currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
            {
                PropertyField(m_Mode, k_ModeText);
                EditorGUI.indentLevel++;
                switch (m_Mode.value.GetEnumValue<RayTracingMode>())
                {
                    case RayTracingMode.Performance:
                    {
                        RayTracingPerformanceModeGUI();
                    }
                    break;
                    case RayTracingMode.Quality:
                    {
                        RayTracingQualityModeGUI();
                    }
                    break;
                }
                EditorGUI.indentLevel--;
            }
            else if (currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Quality)
            {
                RayTracingQualityModeGUI();
            }
            else
            {
                RayTracingPerformanceModeGUI();
            }
        }

        public override void OnInspectorGUI()
        {
            // This whole editor has nothing to display if the SSR feature is not supported
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportSSR ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Screen Space Reflection.", MessageType.Error, wide: true);
                return;
            }

            PropertyField(m_Enable, EditorGUIUtility.TrTextContent("Enable"));

            // The ray tracing enabling checkbox is only displayed if the asset supports ray tracing
            bool rayTracingSupported = HDRenderPipeline.pipelineSupportsRayTracing;
            if (rayTracingSupported)
                PropertyField(m_RayTracing, k_RayTracingText);

            // The rest of the ray tracing UI is only displayed if the asset supports ray tracing and the checkbox is checked.
            if (rayTracingSupported && m_RayTracing.overrideState.boolValue && m_RayTracing.value.boolValue)
            {
                RayTracedReflectionGUI();
            }
            else
            {
                // Shared Data
                PropertyField(m_MinSmoothness, k_MinimumSmoothnessText);
                PropertyField(m_SmoothnessFadeStart, k_SmoothnessFadeStartText);
                PropertyField(m_ReflectSky, k_ReflectSkyText);
                m_SmoothnessFadeStart.value.floatValue  = Mathf.Max(m_MinSmoothness.value.floatValue, m_SmoothnessFadeStart.value.floatValue);
                PropertyField(m_ScreenFadeDistance, k_ScreenFaceDistanceText);
                PropertyField(m_DepthBufferThickness, k_DepthBufferThicknessText);

                m_DepthBufferThickness.value.floatValue = Mathf.Clamp(m_DepthBufferThickness.value.floatValue, 0.001f, 1.0f);

                base.OnInspectorGUI();
                GUI.enabled = useCustomValue;
                PropertyField(m_RayMaxIterations, k_RayMaxIterationsText);
                m_RayMaxIterations.value.intValue = Mathf.Max(0, m_RayMaxIterations.value.intValue);
                GUI.enabled = true;
            }
        }
    }
}
