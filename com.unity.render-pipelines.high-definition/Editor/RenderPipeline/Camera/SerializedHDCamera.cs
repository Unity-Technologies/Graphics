using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedHDCamera
    {
        public SerializedObject serializedObject;
        public SerializedObject serializedAdditionalDataObject;

        //public SerializedProperty backgroundColor;
        
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

            var additionals = CoreEditorUtils.GetAdditionalData<HDCamera>(serializedObject.targetObjects, HDCamera.InitDefaultHDAdditionalCameraData);
            serializedAdditionalDataObject = new SerializedObject(additionals);

            var hideFlags = serializedAdditionalDataObject.FindProperty("m_ObjectHideFlags");
            // We don't hide additional camera data anymore on UX team request. To be compatible with already author scene we force to be visible
            if ((hideFlags.intValue & (int)HideFlags.HideInInspector) > 0)
                hideFlags.intValue = (int)HideFlags.None;
            serializedAdditionalDataObject.ApplyModifiedProperties();

            //backgroundColor = serializedObject.FindProperty("m_BackGroundColor");
            iso = serializedAdditionalDataObject.FindProperty("physicalParameters.m_Iso");
            shutterSpeed = serializedAdditionalDataObject.FindProperty("physicalParameters.m_ShutterSpeed");
            aperture = serializedAdditionalDataObject.FindProperty("physicalParameters.m_Aperture");
            bladeCount = serializedAdditionalDataObject.FindProperty("physicalParameters.m_BladeCount");
            curvature = serializedAdditionalDataObject.FindProperty("physicalParameters.m_Curvature");
            barrelClipping = serializedAdditionalDataObject.FindProperty("physicalParameters.m_BarrelClipping");
            anamorphism = serializedAdditionalDataObject.FindProperty("physicalParameters.m_Anamorphism");

            antialiasing = serializedAdditionalDataObject.Find((HDCamera d) => d.antialiasing);
            SMAAQuality = serializedAdditionalDataObject.Find((HDCamera d) => d.SMAAQuality);
            taaSharpenStrength = serializedAdditionalDataObject.Find((HDCamera d) => d.taaSharpenStrength);
            dithering = serializedAdditionalDataObject.Find((HDCamera d) => d.dithering);
            stopNaNs = serializedAdditionalDataObject.Find((HDCamera d) => d.stopNaNs);
            clearColorMode = serializedAdditionalDataObject.Find((HDCamera d) => d.clearColorMode);
            backgroundColorHDR = serializedAdditionalDataObject.Find((HDCamera d) => d.backgroundColorHDR);
            passThrough = serializedAdditionalDataObject.Find((HDCamera d) => d.fullscreenPassthrough);
            customRenderingSettings = serializedAdditionalDataObject.Find((HDCamera d) => d.customRenderingSettings);
            clearDepth = serializedAdditionalDataObject.Find((HDCamera d) => d.clearDepth);
            volumeLayerMask = serializedAdditionalDataObject.Find((HDCamera d) => d.volumeLayerMask);
            volumeAnchorOverride = serializedAdditionalDataObject.Find((HDCamera d) => d.volumeAnchorOverride);
            frameSettings = new SerializedFrameSettings(
                serializedAdditionalDataObject.FindProperty("m_RenderingPathCustomFrameSettings"),
                serializedAdditionalDataObject.Find((HDCamera d) => d.renderingPathCustomFrameSettingsOverrideMask)
                );

            probeLayerMask = serializedAdditionalDataObject.Find((HDCamera d) => d.probeLayerMask);
            allowDynamicResolution = serializedAdditionalDataObject.Find((HDCamera d) => d.allowDynamicResolution);

            baseCameraSettings = new CameraEditor.Settings(serializedObject);
            baseCameraSettings.OnEnable();
        }

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

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            serializedAdditionalDataObject.ApplyModifiedProperties();
        }
    }
}
