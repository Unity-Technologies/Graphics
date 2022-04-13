using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class SerializedProbeSettingsOverride
    {
        internal SerializedProperty root;

        internal SerializedProperty probe;
        internal SerializedCameraSettingsOverride camera;

        public SerializedProbeSettingsOverride(SerializedProperty root)
        {
            this.root = root;

            probe = root.Find((ProbeSettingsOverride p) => p.probe);
            camera = new SerializedCameraSettingsOverride(root.Find((ProbeSettingsOverride p) => p.camera));
        }
    }

    internal class SerializedProbeSettings
    {
        internal SerializedProperty root;
        internal SerializedCameraSettings cameraSettings;
        internal SerializedInfluenceVolume influence;
        internal SerializedProxyVolume proxy;

        internal SerializedProperty type;
        internal SerializedProperty mode;
        internal SerializedProperty realtimeMode;
        internal SerializedProperty lightingMultiplier;
        internal SerializedProperty lightingWeight;
        internal SerializedProperty lightingFadeDistance;
        internal SerializedProperty lightingLightLayer;
        internal SerializedProperty lightingRangeCompressionFactor;
        internal SerializedProperty proxyUseInfluenceVolumeAsProxyVolume;
        internal SerializedProperty proxyCapturePositionProxySpace;
        internal SerializedProperty proxyCaptureRotationProxySpace;
        internal SerializedProperty proxyMirrorPositionProxySpace;
        internal SerializedProperty proxyMirrorRotationProxySpace;
        internal SerializedScalableSettingValue resolutionScalable;
        internal SerializedProperty roughReflections;
        internal SerializedProperty distanceBasedRoughness;
        internal SerializedProperty frustumFieldOfViewMode;
        internal SerializedProperty frustumFixedValue;
        internal SerializedProperty frustumViewerScale;
        internal SerializedProperty frustumAutomaticScale;

        internal SerializedProbeSettings(SerializedProperty root)
        {
            this.root = root;

            type = root.Find((ProbeSettings p) => p.type);
            mode = root.Find((ProbeSettings p) => p.mode);
            realtimeMode = root.Find((ProbeSettings p) => p.realtimeMode);
            lightingMultiplier = root.FindPropertyRelative("lighting.multiplier");
            lightingWeight = root.FindPropertyRelative("lighting.weight");
            lightingFadeDistance = root.FindPropertyRelative("lighting.fadeDistance");
            lightingLightLayer = root.FindPropertyRelative("lighting.lightLayer");
            lightingRangeCompressionFactor = root.FindPropertyRelative("lighting.rangeCompressionFactor");
            proxyUseInfluenceVolumeAsProxyVolume = root.FindPropertyRelative("proxySettings.useInfluenceVolumeAsProxyVolume");
            proxyCapturePositionProxySpace = root.FindPropertyRelative("proxySettings.capturePositionProxySpace");
            proxyCaptureRotationProxySpace = root.FindPropertyRelative("proxySettings.captureRotationProxySpace");
            proxyMirrorPositionProxySpace = root.FindPropertyRelative("proxySettings.mirrorPositionProxySpace");
            proxyMirrorRotationProxySpace = root.FindPropertyRelative("proxySettings.mirrorRotationProxySpace");
            resolutionScalable = new SerializedScalableSettingValue(root.Find((ProbeSettings p) => p.resolutionScalable));
            roughReflections = root.FindPropertyRelative("roughReflections");
            distanceBasedRoughness = root.FindPropertyRelative("distanceBasedRoughness");
            frustumFieldOfViewMode = root.FindPropertyRelative("frustum.fieldOfViewMode");
            frustumFixedValue = root.FindPropertyRelative("frustum.fixedValue");
            frustumViewerScale = root.FindPropertyRelative("frustum.viewerScale");
            frustumAutomaticScale = root.FindPropertyRelative("frustum.automaticScale");

            cameraSettings = new SerializedCameraSettings(root.Find((ProbeSettings p) => p.cameraSettings));
            influence = new SerializedInfluenceVolume(root.Find((ProbeSettings p) => p.influence));
            proxy = new SerializedProxyVolume(root.Find((ProbeSettings p) => p.proxy));
        }
    }
}
