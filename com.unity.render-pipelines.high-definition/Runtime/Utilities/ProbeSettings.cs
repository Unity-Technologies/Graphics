using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A bitflags for the probe settings field.
    /// </summary>
    [Flags]
    public enum ProbeSettingsFields
    {
        /// <summary>No fields</summary>
        none = 0,
        /// <summary>type</summary>
        type = 1 << 0,
        /// <summary>mode</summary>
        mode = 1 << 1,
        /// <summary>lightingMultiplier</summary>
        lightingMultiplier = 1 << 2,
        /// <summary>lightingWeight</summary>
        lightingWeight = 1 << 3,
        /// <summary>lightingLightLayer</summary>
        lightingLightLayer = 1 << 4,
        /// <summary>lightingRangeCompression</summary>
        lightingRangeCompression = 1 << 5,
        /// <summary>proxy.useInfluenceVolumeAsProxyVolume</summary>
        proxyUseInfluenceVolumeAsProxyVolume = 1 << 6,
        /// <summary>proxy.capturePositionProxySpace</summary>
        proxyCapturePositionProxySpace = 1 << 7,
        /// <summary>proxy.captureRotationProxySpace</summary>
        proxyCaptureRotationProxySpace = 1 << 8,
        /// <summary>proxy.mirrorPositionProxySpace</summary>
        proxyMirrorPositionProxySpace = 1 << 9,
        /// <summary>proxy.mirrorRotationProxySpace</summary>
        proxyMirrorRotationProxySpace = 1 << 10,
        /// <summary>frustum.fieldOfViewMode</summary>
        frustumFieldOfViewMode = 1 << 11,
        /// <summary>frustum.fixedValue</summary>
        frustumFixedValue = 1 << 12,
        /// <summary>frustum.automaticScale</summary>
        frustumAutomaticScale = 1 << 13,
        /// <summary>frustum.viewerScale</summary>
        frustumViewerScale = 1 << 14,
        /// <summary>lighting.fadeDistance</summary>
        lightingFadeDistance = 1 << 15,
        /// <summary>resolution.</summary>
        resolution = 1 << 16,
        /// <summary>Rough reflections.</summary>
        roughReflections = 1 << 17,
    }

    /// <summary>
    /// The overriden fields of a probe.
    /// </summary>
    [Serializable]
    struct ProbeSettingsOverride
    {
        /// <summary> Overriden probe settings</summary>
        public ProbeSettingsFields probe;
        /// <summary> Overriden camera settings</summary>
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

        /// <summary>Realtime mode of the probe.</summary>
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
            [Obsolete("Since 2019.3, use Lighting.NewDefault() instead.")]
            public static readonly Lighting @default = default;
            /// <summary>Default value.</summary>
            /// <returns>The default value.</returns>
            public static Lighting NewDefault() => new Lighting
            {
                multiplier = 1.0f,
                weight = 1.0f,
                lightLayer = LightLayerEnum.LightLayerDefault,
                fadeDistance = 10000f,
                rangeCompressionFactor = 1.0f
            };

            /// <summary>A multiplier applied to the radiance of the Probe.</summary>
            public float multiplier;
            /// <summary>A weight applied to the influence of the Probe.</summary>
            [Range(0, 1)]
            public float weight;
            /// <summary>An enum flag to select which Light Layers this Probe interacts with.</summary>
            public LightLayerEnum lightLayer;
            /// <summary>The distance at which reflections smoothly fade out before HDRP cut them completely.</summary>
            public float fadeDistance;
            /// <summary>The result of the rendering of the probe will be divided by this factor. When the probe is read, this factor is undone as the probe data is read.
            /// This is to simply avoid issues with values clamping due to precision of the storing format.</summary>
            [Min(1e-6f)]
            public float rangeCompressionFactor;
        }

        /// <summary>Settings of this probe in the current proxy.</summary>
        [Serializable]
        public struct ProxySettings
        {
            /// <summary>Default value.</summary>
            [Obsolete("Since 2019.3, use ProxySettings.NewDefault() instead.")]
            public static readonly ProxySettings @default = default;
            /// <summary>Default value.</summary>
            /// <returns>The default value.</returns>
            public static ProxySettings NewDefault() => new ProxySettings
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
            /// <summary>Obsolete</summary>
            [Obsolete("Since 2019.3, use Frustum.NewDefault() instead.")]
            public static readonly Frustum @default = default;
            /// <summary>Default value.</summary>
            /// <returns>The default value.</returns>
            public static Frustum NewDefault() => new Frustum
            {
                fieldOfViewMode = FOVMode.Viewer,
                fixedValue = 90,
                automaticScale = 1.0f,
                viewerScale = 1.0f
            };

            /// <summary>
            /// The FOV mode of a probe.
            /// </summary>
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
            [Range(0, 179f)]
            public float fixedValue;
            /// <summary>The automatic value of the FOV is multiplied by this factor at the end.</summary>
            [Min(0)]
            public float automaticScale;
            /// <summary>The viewer's FOV is multiplied by this factor at the end.</summary>
            [Min(0)]
            public float viewerScale;
        }

        /// <summary>Default value.</summary>
        [Obsolete("Since 2019.3, use ProbeSettings.NewDefault() instead.")]
        public static ProbeSettings @default = default;
        /// <summary>Default value.</summary>
        /// <returns>The default value.</returns>
        public static ProbeSettings NewDefault()
        {
            ProbeSettings probeSettings = new ProbeSettings
            {
                type = ProbeType.ReflectionProbe,
                realtimeMode = RealtimeMode.EveryFrame,
                mode = Mode.Baked,
                cameraSettings = CameraSettings.NewDefault(),
                influence = null,
                lighting = Lighting.NewDefault(),
                proxy = null,
                proxySettings = ProxySettings.NewDefault(),
                frustum = Frustum.NewDefault(),
                resolutionScalable = new PlanarReflectionAtlasResolutionScalableSettingValue(),
                roughReflections = true,
                distanceBasedRoughness = false,
            };
            probeSettings.resolutionScalable.@override = PlanarReflectionAtlasResolution.Resolution512;

            return probeSettings;
        }

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
        /// <summary> An int scalable setting value</summary>
        [Serializable] public class PlanarReflectionAtlasResolutionScalableSettingValue : ScalableSettingValue<PlanarReflectionAtlasResolution> { }
        /// <summary>Camera settings to use when capturing data.</summary>
        /// <summary>The resolution of the probe.</summary>
        public PlanarReflectionAtlasResolutionScalableSettingValue resolutionScalable;
        [SerializeField]
        internal PlanarReflectionAtlasResolution resolution;
        /// <summary>Probe camera settings.</summary>
        [Serialization.FormerlySerializedAs("camera")]
        public CameraSettings cameraSettings;

        /// <summary>Indicates whether the ReflectionProbe supports rough reflections.</summary>
        public bool roughReflections;

        /// <summary>Indicates whether the ReflectionProbe supports distance-based roughness.</summary>
        public bool distanceBasedRoughness;

        /// <summary>
        /// Compute a hash of the settings.
        /// </summary>
        /// <returns>The computed hash.</returns>
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
            h2 = cameraSettings.GetHash();
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
