using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Flags]
    public enum ProbeSettingsFields
    {
        none = 0,
        type = 1 << 0,
        mode = 1 << 1,
        lightingMultiplier = 1 << 2,
        lightingWeight = 1 << 3,
        lightingLightLayer = 1 << 4,
        proxyUseInfluenceVolumeAsProxyVolume = 1 << 5,
        proxyCapturePositionProxySpace = 1 << 6,
        proxyCaptureRotationProxySpace = 1 << 7,
        proxyMirrorPositionProxySpace = 1 << 8,
        proxyMirrorRotationProxySpace = 1 << 9,
        frustumFieldOfViewMode = 1 << 10,
        frustumFixedValue = 1 << 11,
        frustumAutomaticScale = 1 << 12,
        frustumViewerScale = 1 << 13
    }

    [Serializable]
    public struct ProbeSettingsOverride
    {
        public ProbeSettingsFields probe;
        public CameraSettingsOverride camera;
    }

    /// <summary>Settings that defines the rendering of a probe.</summary>
    [Serializable]
    public struct ProbeSettings
    {
        /// <summary>The type of the probe.</summary>
        public enum ProbeType
        {
            /// <summary>
            /// Standard reflection probe.
            ///
            /// A reflection probe captures a cubemap around a capture position.
            /// </summary>
            ReflectionProbe,
            /// <summary>
            /// Planar reflection probe.
            ///
            /// A planar reflection probe captures a single camera render.
            /// The capture position is the mirrored viewer's position against a mirror plane.
            /// This plane is defined by the probe's transform:
            ///  * center = center of the probe
            ///  * normal = forward of the probe
            ///
            /// The viewer's transform must be provided with <see cref="ProbeCapturePositionSettings.referencePosition"/>
            /// and <see cref="ProbeCapturePositionSettings.referenceRotation"/> when calling <see cref="HDRenderUtilities.Render(ProbeSettings, ProbeCapturePositionSettings, Texture)"/>.
            /// </summary>
            PlanarProbe
        }

        /// <summary>The rendering mode of the probe.</summary>
        public enum Mode
        {
            /// <summary>Capture data is baked in editor and loaded as assets.</summary>
            Baked,
            /// <summary>Capture data is computed during runtime.</summary>
            Realtime,
            /// <summary>Capture data provided as an assets.</summary>
            Custom
        }

        public enum RealtimeMode
        {
            /// <summary>The real time probe will be rendered when a camera see its influence, once per frame.</summary>
            EveryFrame,
            /// <summary>The real time probe will be rendered when a camera see its influence, once after OnEnable.</summary>
            OnEnable,
            /// <summary>The real time probe will be rendered when a camera see its influence and the udpate was requested by a script. <see cref="HDProbe.RequestRenderNextUpdate"/>.</summary>
            OnDemand
        }

        /// <summary>Lighting parameters for the probe.</summary>
        [Serializable]
        public struct Lighting
        {
            /// <summary>Default value.</summary>
            public static readonly Lighting @default = new Lighting
            {
                multiplier = 1.0f,
                weight = 1.0f,
                lightLayer = LightLayerEnum.LightLayerDefault
            };

            /// <summary>A multiplier applied to the radiance of the probe.</summary>
            public float multiplier;
            /// <summary>A weight applied to the influence of the probe.</summary>
            public float weight;
            public LightLayerEnum lightLayer;
        }

        /// <summary>Settings of this probe in the current proxy.</summary>
        [Serializable]
        public struct ProxySettings
        {
            /// <summary>Default value.</summary>
            public static readonly ProxySettings @default = new ProxySettings
            {
                capturePositionProxySpace = Vector3.zero,
                captureRotationProxySpace = Quaternion.identity,
                useInfluenceVolumeAsProxyVolume = false
            };

            /// <summary>
            /// Whether to use the influence volume as proxy volume
            /// when <c><see cref="proxy"/> == null</c>.
            /// </summary>
            public bool useInfluenceVolumeAsProxyVolume;
            /// <summary>Position of the capture in proxy space. (Reflection Probe only)</summary>
            public Vector3 capturePositionProxySpace;
            /// <summary>Rotation of the capture in proxy space. (Reflection Probe only)</summary>
            public Quaternion captureRotationProxySpace;
            /// <summary>Position of the mirror in proxy space. (Planar Probe only)</summary>
            public Vector3 mirrorPositionProxySpace;
            /// <summary>Rotation of the mirror in proxy space. (Planar Probe only)</summary>
            public Quaternion mirrorRotationProxySpace;
        }

        /// <summary>Describe how frustum is handled when rendering probe.</summary>
        [Serializable]
        public struct Frustum
        {
            public static readonly Frustum @default = new Frustum
            {
                fieldOfViewMode = FOVMode.Viewer,
                fixedValue = 90,
                automaticScale = 1.0f,
                viewerScale = 1.0f
            };

            public enum FOVMode
            {
                /// <summary>FOV is fixed, its value is <paramref name="fixedValue"/> in degree.</summary>
                Fixed,
                /// <summary>FOV is the one used by the viewer's camera.</summary>
                Viewer,
                /// <summary>FOV is computed to encompass the influence volume, then it is multiplied by <paramref name="automaticScale"/>.</summary>
                Automatic
            }

            /// <summary>
            /// Mode to use when computing the field of view.
            ///
            /// For planar reflection probes: this value is used.
            /// For reflection probes: this value is ignored, FOV will be 90°.
            /// </summary>
            public FOVMode fieldOfViewMode;
            /// <summary>Value to use when FOV is fixed.</summary>
            [Range(0, 180)]
            public float fixedValue;
            /// <summary>The automatic value of the FOV is multiplied by this factor at the end.</summary>
            public float automaticScale;
            /// <summary>The viewer's FOV is multiplied by this factor at the end.</summary>
            public float viewerScale;
        }

        /// <summary>Default value.</summary>
        public static ProbeSettings @default = new ProbeSettings
        {
            type = ProbeType.ReflectionProbe,
            realtimeMode = RealtimeMode.EveryFrame,
            mode = Mode.Baked,
            camera = CameraSettings.@default,
            influence = null,
            lighting = Lighting.@default,
            proxy = null,
            proxySettings = ProxySettings.@default,
            frustum = Frustum.@default
        };

        /// <summary>The way the frustum is handled by the probe.</summary>
        public Frustum frustum;
        /// <summary>The type of the probe.</summary>
        public ProbeType type;
        /// <summary>The mode of the probe.</summary>
        public Mode mode;
        /// <summary>The mode of the probe.</summary>
        public RealtimeMode realtimeMode;
        /// <summary>The lighting of the probe.</summary>
        public Lighting lighting;
        /// <summary>The influence volume of the probe.</summary>
        public InfluenceVolume influence;
        /// <summary>Set this variable to explicitly set the proxy volume to use.</summary>
        public ProxyVolume proxy;
        /// <summary>The proxy settings of the probe for the current volume.</summary>
        public ProxySettings proxySettings;
        /// <summary>Camera settings to use when capturing data.</summary>
        public CameraSettings camera;

        public Hash128 ComputeHash()
        {
            var h = new Hash128();
            var h2 = new Hash128();
            HashUtilities.ComputeHash128(ref type, ref h);
            HashUtilities.ComputeHash128(ref mode, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref lighting, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref proxySettings, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref camera, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            if (influence != null)
            {
                h2 = influence.ComputeHash();
                HashUtilities.AppendHash(ref h2, ref h);
            }
            if (proxy != null)
            {
                h2 = proxy.ComputeHash();
                HashUtilities.AppendHash(ref h2, ref h);
            }
            return h;
        }
    }
}
