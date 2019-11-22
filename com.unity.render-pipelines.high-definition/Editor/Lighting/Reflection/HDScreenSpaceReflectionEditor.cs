using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ScreenSpaceReflection))]
    class HDScreenSpaceReflectionEditor : VolumeComponentEditor
    {
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

        // Tier 1
        SerializedDataParameter m_UpscaleRadius;
        SerializedDataParameter m_FullResolution;
        SerializedDataParameter m_DeferredMode;
        SerializedDataParameter m_RayBinning;

        // Tier 2
        SerializedDataParameter m_SampleCount;
        SerializedDataParameter m_BounceCount;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceReflection>(serializedObject);
            m_RayTracing              = Unpack(o.Find(x => x.rayTracing));

            // Shared data
            m_MinSmoothness = Unpack(o.Find(x => x.minSmoothness));
            m_SmoothnessFadeStart = Unpack(o.Find(x => x.smoothnessFadeStart));
            m_ReflectSky          = Unpack(o.Find(x => x.reflectSky));

            // SSR Data
            m_DepthBufferThickness = Unpack(o.Find(x => x.depthBufferThickness));
            m_RayMaxIterations = Unpack(o.Find(x => x.rayMaxIterations));
            m_ScreenFadeDistance = Unpack(o.Find(x => x.screenFadeDistance));

            // Generic ray tracing
            m_LayerMask                     = Unpack(o.Find(x => x.layerMask));
            m_RayLength                     = Unpack(o.Find(x => x.rayLength));
            m_ClampValue                    = Unpack(o.Find(x => x.clampValue));
            m_Denoise                       = Unpack(o.Find(x => x.denoise));
            m_DenoiserRadius                = Unpack(o.Find(x => x.denoiserRadius));

            // Tier 1
            m_UpscaleRadius                 = Unpack(o.Find(x => x.upscaleRadius));
            m_FullResolution                = Unpack(o.Find(x => x.fullResolution));
            m_DeferredMode                  = Unpack(o.Find(x => x.deferredMode));
            m_RayBinning                    = Unpack(o.Find(x => x.rayBinning));

            // Tier 2
            m_SampleCount                   = Unpack(o.Find(x => x.sampleCount));
            m_BounceCount                   = Unpack(o.Find(x => x.bounceCount));
        }

        public override void OnInspectorGUI()
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportSSR ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Screen Space Reflection.", MessageType.Error, wide: true);
                return;
            }

            // If the current pipeline supports ray tracing, display first the ray tracing checkbox
            bool rayTracingSupported = (RenderPipelineManager.currentPipeline as HDRenderPipeline).rayTracingSupported;
            if (rayTracingSupported)
                PropertyField(m_RayTracing, EditorGUIUtility.TrTextContent("Ray Tracing", "Enable ray traced reflections."));

            // Shared Data
            PropertyField(m_MinSmoothness,        EditorGUIUtility.TrTextContent("Minimum Smoothness", "Controls the smoothness value at which HDRP activates SSR and the smoothness-controlled fade out stops."));
            PropertyField(m_SmoothnessFadeStart,  EditorGUIUtility.TrTextContent("Smoothness Fade Start", "Controls the smoothness value at which the smoothness-controlled fade out starts. The fade is in the range [Min Smoothness, Smoothness Fade Start]."));
            PropertyField(m_ReflectSky,           EditorGUIUtility.TrTextContent("Reflect Sky", "When enabled, SSR handles sky reflection."));
            m_SmoothnessFadeStart.value.floatValue  = Mathf.Max(m_MinSmoothness.value.floatValue, m_SmoothnessFadeStart.value.floatValue);

            // If ray tracing is supported and it is enabled on this volume, display the ray tracing options.
            if (rayTracingSupported && m_RayTracing.overrideState.boolValue && m_RayTracing.value.boolValue)
            {
                PropertyField(m_LayerMask, EditorGUIUtility.TrTextContent("Layer Mask", "Layer mask used to include the objects for screen space reflection."));
                PropertyField(m_RayLength, EditorGUIUtility.TrTextContent("Ray Length", "Controls the length of reflection rays."));
                PropertyField(m_ClampValue, EditorGUIUtility.TrTextContent("Clamp Value", "Clamps the exposed intensity."));
                RenderPipelineSettings.RaytracingTier currentTier = currentAsset.currentPlatformRenderPipelineSettings.supportedRaytracingTier;
                switch (currentTier)
                {
                    case RenderPipelineSettings.RaytracingTier.Tier1:
                    {
                        PropertyField(m_UpscaleRadius, EditorGUIUtility.TrTextContent("Upscale Radius", "Controls the size of the upscale radius."));
                        PropertyField(m_FullResolution, EditorGUIUtility.TrTextContent("Full Resolution", "Enables full resolution mode."));
                        PropertyField(m_DeferredMode, EditorGUIUtility.TrTextContent("Deferred Mode", "Enables deferred mode."));
                        PropertyField(m_RayBinning, EditorGUIUtility.TrTextContent("Ray Binning", "Enables ray binning."));
                        PropertyField(m_Denoise, EditorGUIUtility.TrTextContent("Denoise", "Enable denoising on the ray traced reflections."));
                        {
                            EditorGUI.indentLevel++;
                            PropertyField(m_DenoiserRadius, EditorGUIUtility.TrTextContent("Denoiser Radius", "Controls the radius of reflection denoiser."));
                            EditorGUI.indentLevel--;
                        }
                    }
                    break;
                    case RenderPipelineSettings.RaytracingTier.Tier2:
                    {
                        PropertyField(m_SampleCount, EditorGUIUtility.TrTextContent("Sample Count", "Number of samples for reflections."));
                        PropertyField(m_BounceCount, EditorGUIUtility.TrTextContent("Bounce Count", "Number of bounces for reflection rays."));
                        PropertyField(m_Denoise, EditorGUIUtility.TrTextContent("Denoise", "Enable denoising on the ray traced reflections."));
                        {
                            EditorGUI.indentLevel++;
                            PropertyField(m_DenoiserRadius, EditorGUIUtility.TrTextContent("Denoiser Radius", "Controls the radius of reflection denoiser."));
                            EditorGUI.indentLevel--;
                        }
                    }
                    break;
                }
            }
            else
            {
                PropertyField(m_ScreenFadeDistance,   EditorGUIUtility.TrTextContent("Screen Edge Fade Distance", "Controls the distance at which HDRP fades out SSR near the edge of the screen."));
                PropertyField(m_RayMaxIterations,     EditorGUIUtility.TrTextContent("Max Number of Ray Steps", "Sets the maximum number of steps HDRP uses for raytracing. Affects both correctness and performance."));
                PropertyField(m_DepthBufferThickness, EditorGUIUtility.TrTextContent("Object Thickness", "Controls the typical thickness of objects the reflection rays may pass behind."));

                m_RayMaxIterations.value.intValue       = Mathf.Max(0, m_RayMaxIterations.value.intValue);
                m_DepthBufferThickness.value.floatValue = Mathf.Clamp(m_DepthBufferThickness.value.floatValue, 0.001f, 1.0f);
            }
        }
    }
}
