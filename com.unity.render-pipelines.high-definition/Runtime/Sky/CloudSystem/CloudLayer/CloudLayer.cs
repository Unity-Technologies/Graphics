using System;

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
    /// Cloud Layer Volume Component.
    /// This component setups the Cloud Layer for rendering.
    /// </summary>
    [VolumeComponentMenu("Sky/Cloud Layer (Preview)")]
    [CloudUniqueID((int)CloudType.CloudLayer)]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Override-Cloud-Layer" + Documentation.endURL)]
    public class CloudLayer : CloudSettings
    {
        CloudLayer()
        {
            displayName = "CloudLayer (Preview)";
        }

        /// <summary>Controls the global opacity of the cloud layer.</summary>
        [Tooltip("Controls the global opacity of the cloud layer.")]
        public ClampedFloatParameter opacity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        /// <summary>Enable to cover only the upper part of the sky.</summary>
        [Tooltip("Check this box if the cloud layer covers only the upper part of the sky.")]
        public BoolParameter upperHemisphereOnly = new BoolParameter(true);
        /// <summary>Choose the number of cloud layers.</summary>
        public VolumeParameter<CloudMapMode> layers = new VolumeParameter<CloudMapMode>();


        /// <summary>Controls the opacity of the cloud shadows.</summary>
        [Tooltip("Controls the opacity of the cloud shadows.")]
        public MinFloatParameter shadowsOpacity = new MinFloatParameter(1.0f, 0.0f);
        /// <summary>Controls the tiling of the cloud shadows.</summary>
        [Tooltip("Controls the tiling of the cloud shadows.")]
        public MinFloatParameter shadowsTiling = new MinFloatParameter(500.0f, 0.0f);


        /// <summary>
        /// Cloud Map Volume Parameters.
        /// This groups parameters for one cloud map layer.
        /// </summary>
        [Serializable]
        public class CloudMap
        {
            /// <summary>Texture used to render the clouds.</summary>
            [Tooltip("Specify the texture HDRP uses to render the clouds (in LatLong layout).")]
            public TextureParameter cloudMap = new TextureParameter(null);
            /// <summary>Opacity of the red layer.</summary>
            [Tooltip("Opacity of the red layer.")]
            public ClampedFloatParameter opacityR = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
            /// <summary>Opacity of the green layer.</summary>
            [Tooltip("Opacity of the green layer.")]
            public ClampedFloatParameter opacityG = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
            /// <summary>Opacity of the blue layer.</summary>
            [Tooltip("Opacity of the blue layer.")]
            public ClampedFloatParameter opacityB = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
            /// <summary>Opacity of the alpha layer.</summary>
            [Tooltip("Opacity of the alpha layer.")]
            public ClampedFloatParameter opacityA = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

            /// <summary>Rotation of the clouds.</summary>
            [Tooltip("Sets the rotation of the clouds.")]
            public ClampedFloatParameter rotation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
            /// <summary>Color multiplier of the clouds.</summary>
            [Tooltip("Specifies the color that HDRP uses to tint the clouds.")]
            public ColorParameter tint = new ColorParameter(Color.white, false, false, true);
            /// <summary>Exposure of the clouds.</summary>
            [Tooltip("Sets the exposure of the clouds in EV.")]
            public FloatParameter exposure = new FloatParameter(0.0f);

            /// <summary>Distortion mode.</summary>
            [Tooltip("Distortion mode.")]
            public VolumeParameter<CloudDistortionMode> distortionMode = new VolumeParameter<CloudDistortionMode>();
            /// <summary>Direction of the distortion.</summary>
            [Tooltip("Sets the rotation of the distortion (in degrees).")]
            public ClampedFloatParameter scrollDirection = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
            /// <summary>Speed of the distortion.</summary>
            [Tooltip("Sets the cloud scrolling speed. The higher the value, the faster the clouds will move.")]
            public MinFloatParameter scrollSpeed = new MinFloatParameter(1.0f, 0.0f);
            /// <summary>Texture used to distort the UVs for the cloud layer.</summary>
            [Tooltip("Specify the flowmap HDRP uses for cloud distortion (in LatLong layout).")]
            public TextureParameter flowmap = new TextureParameter(null);

            /// <summary>Enable lighting.</summary>
            [Tooltip("Lighting with 2D Raymarching.")]
            public BoolParameter lighting = new BoolParameter(false);
            /// <summary>Number of raymarching steps.</summary>
            [Tooltip("Number of raymarching steps.")]
            public ClampedIntParameter steps = new ClampedIntParameter(4, 1, 10);
            /// <summary>Thickness of the clouds.</summary>
            [Tooltip("Controls the thickness of the clouds.")]
            public ClampedFloatParameter thickness = new ClampedFloatParameter(0.5f, 0, 1);

            /// <summary>Enable to cast shadows.</summary>
            [Tooltip("Enable or disable cloud shadows.")]
            public BoolParameter castShadows = new BoolParameter(false);


            internal float scrollFactor = 0.0f;
            internal int NumSteps => lighting.value ? steps.value : 0;
            internal Vector4 Opacities => new Vector4(opacityR.value, opacityG.value, opacityB.value, opacityA.value);

            internal (Vector4, Vector4) GetRenderingParameters()
            {
                float angle = -Mathf.Deg2Rad * scrollDirection.value;
                Vector4 params1 = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), scrollFactor);
                Vector4 params2 = tint.value * ColorUtils.ConvertEV100ToExposure(-exposure.value);
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

            internal int GetBakingHashCode(Light sunLight)
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

                        hash = hash * 23 + sunLight.transform.rotation.GetHashCode();
                    }

#if UNITY_EDITOR
                    // In the editor, we want to rebake the texture if the texture content is modified
                    if (cloudMap.value != null)
                        hash = hash * 23 + cloudMap.value.imageContentsHash.GetHashCode();
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
            int hash = 17;//base.GetHashCode();

            unchecked
            {
                //hash = hash * 23 + upperHemisphereOnly.GetHashCode();
                hash = hash * 23 + layers.GetHashCode();
                hash = hash * 23 + layerA.GetBakingHashCode(sunLight);
                if (layers.value == CloudMapMode.Double)
                    hash = hash * 23 + layerB.GetBakingHashCode(sunLight);
            }

            return hash;
        }

        /// <summary>
        /// Returns CloudLayerRenderer type.
        /// </summary>
        /// <returns>CloudLayerRenderer type.</returns>
        public override Type GetCloudRendererType() { return typeof(CloudLayerRenderer); }

    }
}
