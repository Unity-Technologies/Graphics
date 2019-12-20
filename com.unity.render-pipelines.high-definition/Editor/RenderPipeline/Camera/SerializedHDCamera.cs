using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedHDCamera
    {
        public SerializedObject serializedObject;

        public SerializedProperty iso;
        public SerializedProperty shutterSpeed;
        public SerializedProperty aperture;
        public SerializedProperty bladeCount;
        public SerializedProperty curvature;
        public SerializedProperty barrelClipping;
        public SerializedProperty anamorphism;

        public SerializedProperty antialiasing;
        public SerializedProperty SMAAQuality;
        public SerializedProperty taaSharpenStrength;
        public SerializedProperty dithering;
        public SerializedProperty stopNaNs;
        public SerializedProperty clearColorMode;
        public SerializedProperty backgroundColorHDR;
        public SerializedProperty passThrough;
        public SerializedProperty customRenderingSettings;
        public SerializedProperty clearDepth;
        public SerializedProperty volumeLayerMask;
        public SerializedProperty volumeAnchorOverride;
        public SerializedProperty allowDynamicResolution;

        public SerializedFrameSettings frameSettings;

        public CameraEditor.Settings baseCameraSettings { get; private set; }

        // This one is internal in UnityEditor for whatever reason...
        public SerializedProperty projectionMatrixMode;

        public SerializedProperty probeLayerMask;

        public SerializedHDCamera(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            projectionMatrixMode = serializedObject.FindProperty("m_projectionMatrixMode");
            
            iso = serializedObject.FindProperty("physicalParameters.m_Iso");
            shutterSpeed = serializedObject.FindProperty("physicalParameters.m_ShutterSpeed");
            aperture = serializedObject.FindProperty("physicalParameters.m_Aperture");
            bladeCount = serializedObject.FindProperty("physicalParameters.m_BladeCount");
            curvature = serializedObject.FindProperty("physicalParameters.m_Curvature");
            barrelClipping = serializedObject.FindProperty("physicalParameters.m_BarrelClipping");
            anamorphism = serializedObject.FindProperty("physicalParameters.m_Anamorphism");

            antialiasing = serializedObject.Find((HDCamera d) => d.antialiasing);
            SMAAQuality = serializedObject.Find((HDCamera d) => d.SMAAQuality);
            taaSharpenStrength = serializedObject.Find((HDCamera d) => d.taaSharpenStrength);
            dithering = serializedObject.Find((HDCamera d) => d.dithering);
            stopNaNs = serializedObject.Find((HDCamera d) => d.stopNaNs);
            clearColorMode = serializedObject.Find((HDCamera d) => d.clearColorMode);
            backgroundColorHDR = serializedObject.Find((HDCamera d) => d.backgroundColorHDR);
            passThrough = serializedObject.Find((HDCamera d) => d.fullscreenPassthrough);
            customRenderingSettings = serializedObject.Find((HDCamera d) => d.customRenderingSettings);
            clearDepth = serializedObject.Find((HDCamera d) => d.clearDepth);
            volumeLayerMask = serializedObject.Find((HDCamera d) => d.volumeLayerMask);
            volumeAnchorOverride = serializedObject.Find((HDCamera d) => d.volumeAnchorOverride);
            frameSettings = new SerializedFrameSettings(
                serializedObject.FindProperty("m_RenderingPathCustomFrameSettings"),
                serializedObject.Find((HDCamera d) => d.renderingPathCustomFrameSettingsOverrideMask)
                );

            probeLayerMask = serializedObject.Find((HDCamera d) => d.probeLayerMask);
            allowDynamicResolution = serializedObject.Find((HDCamera d) => d.allowDynamicResolutionHD);
            
            baseCameraSettings = new CameraEditor.Settings(serializedObject);
            baseCameraSettings.OnEnable();
        }

        public void Update()
        {
            serializedObject.Update();

            // Be sure legacy HDR option is disable on camera as it cause banding in SceneView. Yes, it is a contradiction, but well, Unity...
            // When HDR option is enabled, Unity render in FP16 then convert to 8bit with a stretch copy (this cause banding as it should be convert to sRGB (or other color appropriate color space)), then do a final shader with sRGB conversion
            // When LDR, unity render in 8bitSRGB, then do a final shader with sRGB conversion
            // What should be done is just in our Post process we convert to sRGB and store in a linear 10bit, but require C++ change...
            baseCameraSettings.HDR.boolValue = false;
        }

        public void Apply()
            => serializedObject.ApplyModifiedProperties();
    }
}
