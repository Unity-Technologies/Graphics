using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedHDCamera
    {
        CameraEditor.Settings m_Settings;
        SerializedProperty m_SerializedExtension;
        SerializedObject m_SerializedObject;
        
        public SerializedProperty iso;
        public SerializedProperty shutterSpeed;
        public SerializedProperty aperture;
        public SerializedProperty bladeCount;
        public SerializedProperty curvature;
        public SerializedProperty barrelClipping;
        public SerializedProperty anamorphism;
        public SerializedProperty exposureTarget;

        public SerializedProperty antialiasing;
        public SerializedProperty SMAAQuality;
        public SerializedProperty taaSharpenStrength;
        public SerializedProperty taaHistorySharpening;
        public SerializedProperty taaAntiFlicker;
        public SerializedProperty taaMotionVectorRejection;
        public SerializedProperty taaAntiRinging;
        public SerializedProperty taaQualityLevel;

        public SerializedProperty dithering;
        public SerializedProperty stopNaNs;
        public SerializedProperty clearColorMode;
        public SerializedProperty backgroundColorHDR;
        public SerializedProperty xrRendering;
        public SerializedProperty passThrough;
        public SerializedProperty customRenderingSettings;
        public SerializedProperty clearDepth;
        public SerializedProperty volumeLayerMask;
        public SerializedProperty volumeAnchorOverride;
        public SerializedProperty allowDynamicResolution;
        public SerializedFrameSettings frameSettings;
        public CameraEditor.Settings baseCameraSettings => m_Settings;

        // This one is internal in UnityEditor for whatever reason...
        public SerializedProperty projectionMatrixMode;

        public SerializedProperty probeLayerMask;

        public SerializedHDCamera(CameraEditor.Settings settings, SerializedProperty serializedExtension)
        {
            m_Settings = settings;
            m_SerializedExtension = serializedExtension;
            m_SerializedObject = m_SerializedExtension.serializedObject;

            projectionMatrixMode = m_SerializedObject.FindProperty("m_projectionMatrixMode");

            var state = m_SerializedExtension.FindPropertyRelative("m_TransferableState");
            var physicalParameters = state.FindPropertyRelative("physicalParameters");

            iso = physicalParameters.FindPropertyRelative("m_Iso");
            shutterSpeed = physicalParameters.FindPropertyRelative("m_ShutterSpeed");
            aperture = physicalParameters.FindPropertyRelative("m_Aperture");
            bladeCount = physicalParameters.FindPropertyRelative("m_BladeCount");
            curvature = physicalParameters.FindPropertyRelative("m_Curvature");
            barrelClipping = physicalParameters.FindPropertyRelative("m_BarrelClipping");
            anamorphism = physicalParameters.FindPropertyRelative("m_Anamorphism");

            exposureTarget = state.FindPropertyRelative("exposureTarget");

            antialiasing = state.FindPropertyRelative("antialiasing");
            SMAAQuality = state.FindPropertyRelative("SMAAQuality");
            taaSharpenStrength = state.FindPropertyRelative("taaSharpenStrength");
            taaQualityLevel = state.FindPropertyRelative("TAAQuality");
            taaHistorySharpening = state.FindPropertyRelative("taaHistorySharpening");
            taaAntiFlicker = state.FindPropertyRelative("taaAntiFlicker");
            taaMotionVectorRejection = state.FindPropertyRelative("taaMotionVectorRejection");
            taaAntiRinging = state.FindPropertyRelative("taaAntiHistoryRinging");
            taaQualityLevel = state.FindPropertyRelative("TAAQuality");

            dithering = state.FindPropertyRelative("dithering");
            stopNaNs = state.FindPropertyRelative("stopNaNs");
            clearColorMode = state.FindPropertyRelative("clearColorMode");
            backgroundColorHDR = state.FindPropertyRelative("backgroundColorHDR");
            xrRendering = state.FindPropertyRelative("xrRendering");
            passThrough = state.FindPropertyRelative("fullscreenPassthrough");
            customRenderingSettings = state.FindPropertyRelative("customRenderingSettings");
            clearDepth = state.FindPropertyRelative("clearDepth");
            volumeLayerMask = state.FindPropertyRelative("volumeLayerMask");
            volumeAnchorOverride = state.FindPropertyRelative("volumeAnchorOverride");
            frameSettings = new SerializedFrameSettings(
                state.FindPropertyRelative("renderingPathCustomFrameSettings"),
                state.FindPropertyRelative("renderingPathCustomFrameSettingsOverrideMask")
                );

            probeLayerMask = state.FindPropertyRelative("probeLayerMask");
            allowDynamicResolution = state.FindPropertyRelative("allowDynamicResolution");
            
            baseCameraSettings.OnEnable();
        }

        public void Update()
        {
            m_SerializedObject.Update();

            // Be sure legacy HDR option is disable on camera as it cause banding in SceneView. Yes, it is a contradiction, but well, Unity...
            // When HDR option is enabled, Unity render in FP16 then convert to 8bit with a stretch copy (this cause banding as it should be convert to sRGB (or other color appropriate color space)), then do a final shader with sRGB conversion
            // When LDR, unity render in 8bitSRGB, then do a final shader with sRGB conversion
            // What should be done is just in our Post process we convert to sRGB and store in a linear 10bit, but require C++ change...
            baseCameraSettings.HDR.boolValue = false;
        }

        public void Apply()
        {
            m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
