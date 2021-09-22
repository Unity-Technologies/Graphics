using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Cloud Map Mode.
    /// </summary>
    public enum CloudMapMode
    {
        /// <summary>One layer mode.</summary>
        Single,
        /// <summary>Two layer mode.</summary>
        Double,
    }

    /// <summary>
    /// Cloud Distortion Mode.
    /// </summary>
    public enum CloudDistortionMode
    {
        /// <summary>No distortion.</summary>
        None,
        /// <summary>Procedural distortion.</summary>
        Procedural,
        /// <summary>Distortion from a flowmap.</summary>
        Flowmap,
    }

    /// <summary>
    /// Resolution of the cloud texture.
    /// </summary>
    public enum CloudResolution
    {
        /// <summary>Size 256</summary>
        CloudResolution256 = 256,
        /// <summary>Size 512</summary>
        CloudResolution512 = 512,
        /// <summary>Size 1024</summary>
        CloudResolution1024 = 1024,
        /// <summary>Size 2048</summary>
        CloudResolution2048 = 2048,
        /// <summary>Size 4096</summary>
        CloudResolution4096 = 4096,
        /// <summary>Size 8192</summary>
        CloudResolution8192 = 8192,
    }

    /// <summary>
    /// Resolution of the cloud shadow.
    /// </summary>
    public enum CloudShadowsResolution
    {
        /// <summary>Size 64</summary>
        VeryLow = 64,
        /// <summary>Size 128</summary>
        Low = 128,
        /// <summary>Size 256</summary>
        Medium = 256,
        /// <summary>Size 512</summary>
        High = 512,
    }


    /// <summary>
    /// Enum volume parameter.
    /// </summary>
    /// <typeparam name="T">The type of value to hold in this parameter.</typeparam>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class CloudLayerEnumParameter<T> : VolumeParameter<T>
    {
        /// <summary>
        /// Enum volume parameter constructor.
        /// </summary>
        /// <param name="value">Enum parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public CloudLayerEnumParameter(T value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// Cloud Layer Volume Component.
    /// This component setups the Cloud Layer for rendering.
    /// </summary>
    [VolumeComponentMenuForRenderPipeline("Sky/Cloud Layer", typeof(HDRenderPipeline))]
    [CloudUniqueID((int)CloudType.CloudLayer)]
    [HDRPHelpURLAttribute("Override-Cloud-Layer")]
    public class CloudLayer : CloudSettings
    {
        /// <summary>Controls the global opacity of the cloud layer.</summary>
        [Tooltip("Controls the global opacity of the cloud layer.")]
        public ClampedFloatParameter opacity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        /// <summary>Enable to cover only the upper part of the sky.</summary>
        [AdditionalProperty]
        [Tooltip("Check this box if the cloud layer covers only the upper part of the sky.")]
        public BoolParameter upperHemisphereOnly = new BoolParameter(true);
        /// <summary>Choose the number of cloud layers.</summary>
        public VolumeParameter<CloudMapMode> layers = new VolumeParameter<CloudMapMode>();
        /// <summary>Choose the resolution of the baked cloud texture.</summary>
        [AdditionalProperty]
        [Tooltip("Specifies the resolution of the texture HDRP uses to represent the clouds.")]
        public CloudLayerEnumParameter<CloudResolution> resolution = new CloudLayerEnumParameter<CloudResolution>(CloudResolution.CloudResolution1024);


        /// <summary>Controls the opacity of the cloud shadows.</summary>
        [Header("Cloud Shadows")]
        [Tooltip("Controls the opacity of the cloud shadows.")]
        public MinFloatParameter shadowMultiplier = new MinFloatParameter(1.0f, 0.0f);
        /// <summary>Controls the tint of the cloud shadows.</summary>
        [Tooltip("Controls the tint of the cloud shadows.")]
        public ColorParameter shadowTint = new ColorParameter(Color.black, false, false, true);
        /// <summary>Choose the resolution of the texture for the cloud shadows.</summary>
        [AdditionalProperty]
        [Tooltip("Specifies the resolution of the texture HDRP uses to represent the cloud shadows.")]
        public CloudLayerEnumParameter<CloudShadowsResolution> shadowResolution = new CloudLayerEnumParameter<CloudShadowsResolution>(CloudShadowsResolution.Medium);
        /// <summary>Specifies the size of the projected shadows.</summary>
        [Tooltip("Specifies the size of the projected shadows.")]
        public MinFloatParameter shadowSize = new MinFloatParameter(500f, 0.0f);


        /// <summary>
        /// Cloud Map Volume Parameters.
        /// This groups parameters for one cloud map layer.
        /// </summary>
        [Serializable]
        public class CloudMap
        {
            internal static Texture s_DefaultTexture = null;

            /// <summary>Texture used to render the clouds.</summary>
            [Tooltip("Specify the texture HDRP uses to render the clouds (in LatLong layout).")]
            public Texture2DParameter cloudMap = new Texture2DParameter(CloudMap.s_DefaultTexture);
            /// <summary>Opacity multiplier for the red channel.</summary>
            [Tooltip("Opacity multiplier for the red channel.")]
            public ClampedFloatParameter opacityR = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
            /// <summary>Opacity multiplier for the green channel.</summary>
            [Tooltip("Opacity multiplier for the green channel.")]
            public ClampedFloatParameter opacityG = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
            /// <summary>Opacity multiplier for the blue channel.</summary>
            [Tooltip("Opacity multiplier for the blue channel.")]
            public ClampedFloatParameter opacityB = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
            /// <summary>Opacity multiplier for the alpha channel.</summary>
            [Tooltip("Opacity multiplier for the alpha channel.")]
            public ClampedFloatParameter opacityA = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

            /// <summary>Rotation of the clouds.</summary>
            [Tooltip("Sets the rotation of the clouds (in degrees).")]
            public ClampedFloatParameter rotation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
            /// <summary>Color multiplier of the clouds.</summary>
            [Tooltip("Specifies the color HDRP uses to tint the clouds.")]
            public ColorParameter tint = new ColorParameter(Color.white, false, false, true);
            /// <summary>Relative exposure of the clouds.</summary>
            [Tooltip("Sets the exposure of the clouds in EV relative to the sun light intensity.")]
            public FloatParameter exposure = new FloatParameter(0.0f);

            /// <summary>Distortion mode.</summary>
            [Tooltip("Distortion mode used to simulate cloud movement.\nIn Scene View, requires Always Refresh to be enabled.")]
            public VolumeParameter<CloudDistortionMode> distortionMode = new VolumeParameter<CloudDistortionMode>();
            /// <summary>Direction of the distortion. This value can be relative to the Global Wind Orientation defined in the Visual Environment.</summary>
            public WindOrientationParameter scrollOrientation = new WindOrientationParameter();
            /// <summary>Speed of the distortion. This value can be relative to the Global Wind Speed defined in the Visual Environment.</summary>
            public WindSpeedParameter scrollSpeed = new WindSpeedParameter();
            /// <summary>Texture used to distort the UVs for the cloud layer.</summary>
            [Tooltip("Specify the flowmap HDRP uses for cloud distortion (in LatLong layout).")]
            public Texture2DParameter flowmap = new Texture2DParameter(null);

            /// <summary>Simulates cloud self-shadowing using raymarching.</summary>
            [Tooltip("Simulates cloud self-shadowing using raymarching.")]
            public BoolParameter lighting = new BoolParameter(false);
            /// <summary>Number of raymarching steps.</summary>
            [Tooltip("Number of raymarching steps.")]
            public ClampedIntParameter steps = new ClampedIntParameter(4, 1, 10);
            /// <summary>Thickness of the clouds.</summary>
            [Tooltip("Controls the thickness of the clouds.")]
            public ClampedFloatParameter thickness = new ClampedFloatParameter(0.5f, 0, 1);

            /// <summary>Enable to cast shadows.</summary>
            [Tooltip("Projects a portion of the clouds around the sun light to simulate cloud shadows. This will override the cookie of your directional light.")]
            public BoolParameter castShadows = new BoolParameter(false);


            internal float scrollFactor = 0.0f;
            internal int NumSteps => lighting.value ? steps.value : 0;
            internal Vector4 Opacities => new Vector4(opacityR.value, opacityG.value, opacityB.value, opacityA.value);

            internal (Vector4, Vector4) GetRenderingParameters(HDCamera camera, float intensity)
            {
                float angle = Mathf.Deg2Rad * scrollOrientation.GetValue(camera);
                Vector4 params1 = new Vector3(-Mathf.Cos(angle), -Mathf.Sin(angle), scrollFactor / 200.0f);
                Vector4 params2 = tint.value * (ColorUtils.ConvertEV100ToExposure(-exposure.value) * intensity);
                return (params1, params2);
            }

            internal (Vector4, Vector4) GetBakingParameters()
            {
                Vector4 parameters = new Vector4(
                    -rotation.value / 360.0f,
                    NumSteps,
                    thickness.value,
                    0
                );
                return (Opacities, parameters);
            }

            internal int GetBakingHashCode()
            {
                int hash = 0;

                unchecked
                {
                    hash = hash * 23 + cloudMap.GetHashCode();
                    hash = hash * 23 + opacityR.GetHashCode();
                    hash = hash * 23 + opacityG.GetHashCode();
                    hash = hash * 23 + opacityB.GetHashCode();
                    hash = hash * 23 + opacityA.GetHashCode();

                    hash = hash * 23 + rotation.GetHashCode();
                    hash = hash * 23 + castShadows.GetHashCode();

                    if (lighting.value)
                    {
                        hash = hash * 23 + lighting.GetHashCode();
                        hash = hash * 23 + steps.GetHashCode();
                        hash = hash * 23 + thickness.GetHashCode();
                    }

#if UNITY_EDITOR
                    // In the editor, we want to rebake the texture if the texture content is modified
                    if (cloudMap.value != null)
                        hash = hash * 23 + cloudMap.value.imageContentsHash.GetHashCode();
#endif
                }

                return hash;
            }

            /// <summary>
            /// Returns the hash code of the CloudMap parameters.
            /// </summary>
            /// <returns>The hash code of the CloudMap parameters.</returns>
            public override int GetHashCode()
            {
                int hash = GetBakingHashCode();

                unchecked
                {
                    hash = hash * 23 + tint.GetHashCode();
                    hash = hash * 23 + exposure.GetHashCode();

                    hash = hash * 23 + distortionMode.GetHashCode();
                    hash = hash * 23 + scrollOrientation.GetHashCode();
                    hash = hash * 23 + scrollSpeed.GetHashCode();
                    hash = hash * 23 + flowmap.GetHashCode();

                    hash = hash * 23 + distortionMode.GetHashCode();

#if UNITY_EDITOR
                    // In the editor, we want to rebake the texture if the texture content is modified
                    if (flowmap.value != null)
                        hash = hash * 23 + flowmap.value.imageContentsHash.GetHashCode();
#endif
                }

                return hash;
            }
        }

        /// <summary>Layer A.</summary>
        public CloudMap layerA = new CloudMap();
        /// <summary>Layer B.</summary>
        public CloudMap layerB = new CloudMap();

        internal int NumLayers => (layers == CloudMapMode.Single) ? 1 : 2;
        internal bool CastShadows => layerA.castShadows.value || (layers == CloudMapMode.Double && layerB.castShadows.value);

        internal int GetBakingHashCode(Light sunLight)
        {
            int hash = 17;
            bool lighting = layerA.lighting.value;
            bool shadows = sunLight != null && layerA.castShadows.value;

            unchecked
            {
                hash = hash * 23 + upperHemisphereOnly.GetHashCode();
                hash = hash * 23 + layers.GetHashCode();
                hash = hash * 23 + resolution.GetHashCode();
                hash = hash * 23 + layerA.GetBakingHashCode();
                if (layers.value == CloudMapMode.Double)
                {
                    hash = hash * 23 + layerB.GetBakingHashCode();
                    lighting |= layerB.lighting.value;
                    shadows |= layerB.castShadows.value;
                }

                if (lighting && sunLight != null)
                    hash = hash * 23 + sunLight.transform.rotation.GetHashCode();
                if (shadows)
                    hash = hash * 23 + shadowResolution.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Returns the hash code of the CloudLayer parameters.
        /// </summary>
        /// <returns>The hash code of the CloudLayer parameters.</returns>
        public override int GetHashCode()
        {
            int hash = 17;

            unchecked
            {
                hash = hash * 23 + opacity.GetHashCode();
                hash = hash * 23 + upperHemisphereOnly.GetHashCode();
                hash = hash * 23 + layers.GetHashCode();
                hash = hash * 23 + resolution.GetHashCode();

                hash = hash * 23 + layerA.GetHashCode();
                if (layers.value == CloudMapMode.Double)
                    hash = hash * 23 + layerB.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Returns CloudLayerRenderer type.
        /// </summary>
        /// <returns>CloudLayerRenderer type.</returns>
        public override Type GetCloudRendererType() { return typeof(CloudLayerRenderer); }

        /// <summary>
        /// Called though reflection by the VolumeManager.
        /// </summary>
        static void Init()
        {
            var globalSettings = HDRenderPipelineGlobalSettings.instance;
            if (globalSettings != null)
                CloudMap.s_DefaultTexture = globalSettings.renderPipelineResources?.textures.defaultCloudMap;
        }
    }
}
