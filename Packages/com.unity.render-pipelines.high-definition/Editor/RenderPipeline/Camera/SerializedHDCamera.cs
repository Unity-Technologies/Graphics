using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedHDCamera : ISerializedCamera
    {
        public SerializedObject serializedObject { get; }
        public SerializedObject serializedAdditionalDataObject { get; }
        public CameraEditor.Settings baseCameraSettings { get; }

        // This one is internal in UnityEditor for whatever reason...
        public SerializedProperty projectionMatrixMode { get; }

        // Common properties
        public SerializedProperty dithering { get; }
        public SerializedProperty stopNaNs { get; }
        public SerializedProperty allowDynamicResolution { get; }
        public SerializedProperty volumeLayerMask { get; }
        public SerializedProperty clearDepth { get; }
        public SerializedProperty antialiasing { get; }

        // HDRP specific properties
        public SerializedProperty exposureTarget;

        public SerializedProperty allowDeepLearningSuperSampling;
        public SerializedProperty deepLearningSuperSamplingUseCustomQualitySettings;
        public SerializedProperty deepLearningSuperSamplingQuality;
        public SerializedProperty deepLearningSuperSamplingUseCustomAttributes;
        public SerializedProperty deepLearningSuperSamplingUseOptimalSettings;
        public SerializedProperty deepLearningSuperSamplingSharpening;

        public SerializedProperty fsrOverrideSharpness;
        public SerializedProperty fsrSharpness;

        public SerializedProperty SMAAQuality;
        public SerializedProperty taaSharpenStrength;
        public SerializedProperty taaHistorySharpening;
        public SerializedProperty taaAntiFlicker;
        public SerializedProperty taaMotionVectorRejection;
        public SerializedProperty taaAntiRinging;
        public SerializedProperty taaBaseBlendFactor;
        public SerializedProperty taaJitterScale;
        public SerializedProperty taaQualityLevel;

        public SerializedProperty clearColorMode;
        public SerializedProperty backgroundColorHDR;
        public SerializedProperty xrRendering;
        public SerializedProperty passThrough;
        public SerializedProperty customRenderingSettings;
        public SerializedProperty volumeAnchorOverride;
        public SerializedFrameSettings frameSettings;
        public SerializedProperty probeLayerMask;

        public SerializedHDCamera(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            projectionMatrixMode = serializedObject.FindProperty("m_projectionMatrixMode");

            var additionals = CoreEditorUtils.GetAdditionalData<HDAdditionalCameraData>(serializedObject.targetObjects, HDAdditionalCameraData.InitDefaultHDAdditionalCameraData);
            serializedAdditionalDataObject = new SerializedObject(additionals);

            var hideFlags = serializedAdditionalDataObject.FindProperty("m_ObjectHideFlags");
            // We don't hide additional camera data anymore on UX team request. To be compatible with already author scene we force to be visible
            if ((hideFlags.intValue & (int)HideFlags.HideInInspector) > 0)
                hideFlags.intValue = (int)HideFlags.None;
            serializedAdditionalDataObject.ApplyModifiedProperties();

            // Common properties
            dithering = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.dithering);
            stopNaNs = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.stopNaNs);
            allowDynamicResolution = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.allowDynamicResolution);
            volumeLayerMask = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.volumeLayerMask);
            clearDepth = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.clearDepth);
            antialiasing = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.antialiasing);

            exposureTarget = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.exposureTarget);

            allowDeepLearningSuperSampling = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.allowDeepLearningSuperSampling);
            deepLearningSuperSamplingUseCustomQualitySettings = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.deepLearningSuperSamplingUseCustomQualitySettings);
            deepLearningSuperSamplingQuality = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.deepLearningSuperSamplingQuality);
            deepLearningSuperSamplingUseCustomAttributes = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.deepLearningSuperSamplingUseCustomAttributes);
            deepLearningSuperSamplingUseOptimalSettings = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.deepLearningSuperSamplingUseOptimalSettings);
            deepLearningSuperSamplingSharpening = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.deepLearningSuperSamplingSharpening);

            fsrOverrideSharpness = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.fsrOverrideSharpness);
            fsrSharpness = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.fsrSharpness);

            SMAAQuality = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.SMAAQuality);
            taaSharpenStrength = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.taaSharpenStrength);
            taaQualityLevel = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.TAAQuality);
            taaHistorySharpening = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.taaHistorySharpening);
            taaAntiFlicker = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.taaAntiFlicker);
            taaMotionVectorRejection = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.taaMotionVectorRejection);
            taaAntiRinging = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.taaAntiHistoryRinging);
            taaQualityLevel = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.TAAQuality);
            taaBaseBlendFactor = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.taaBaseBlendFactor);
            taaJitterScale = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.taaJitterScale);

            clearColorMode = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.clearColorMode);
            backgroundColorHDR = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.backgroundColorHDR);
            xrRendering = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.xrRendering);
            passThrough = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.fullscreenPassthrough);
            customRenderingSettings = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.customRenderingSettings);
            volumeAnchorOverride = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.volumeAnchorOverride);
            probeLayerMask = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.probeLayerMask);
            frameSettings = new SerializedFrameSettings(
                serializedAdditionalDataObject.FindProperty("m_RenderingPathCustomFrameSettings"),
                serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.renderingPathCustomFrameSettingsOverrideMask)
            );

            baseCameraSettings = new CameraEditor.Settings(serializedObject);
            baseCameraSettings.OnEnable();
        }

        /// <summary>
        /// Updates the internal serialized objects
        /// </summary>
        public void Update()
        {
            serializedObject.Update();
            serializedAdditionalDataObject.Update();

            // Be sure legacy HDR option is disable on camera as it cause banding in SceneView. Yes, it is a contradiction, but well, Unity...
            // When HDR option is enabled, Unity render in FP16 then convert to 8bit with a stretch copy (this cause banding as it should be convert to sRGB (or other color appropriate color space)), then do a final shader with sRGB conversion
            // When LDR, unity render in 8bitSRGB, then do a final shader with sRGB conversion
            // What should be done is just in our Post process we convert to sRGB and store in a linear 10bit, but require C++ change...
            baseCameraSettings.HDR.boolValue = false;
        }

        /// <summary>
        /// Applies the modified properties to the serialized objects
        /// </summary>
        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            serializedAdditionalDataObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Refreshes the serialized properties from the serialized objects
        /// </summary>
        public void Refresh()
        {
        }
    }
}
