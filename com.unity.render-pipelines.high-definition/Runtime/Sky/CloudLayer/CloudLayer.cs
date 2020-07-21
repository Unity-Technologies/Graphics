using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Cloud Layer Mode.
    /// </summary>
    public enum CloudLayerMode
    {
        /// <summary>Cloud Map mode.</summary>
        CloudMap,
        /// <summary>CustomRenderTexture mode.</summary>
        RenderTexture,
    }

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
    /// Lighting Mode.
    /// </summary>
    public enum CloudLightingMode
    {
        /// <summary>No lighting.</summary>
        None,
        /// <summary>Lighting with 2D Raymarching.</summary>
        Raymarching,
    }

    /// <summary>
    /// Cloud Layer Volume Component.
    /// This component setups the cloud layer for rendering.
    /// </summary>
    [VolumeComponentMenu("Sky/Cloud Layer (Preview)")]
    public class CloudLayer : VolumeComponent
    {
        /// <summary>Enable fog.</summary>
        [Tooltip("Check to have a cloud layer in the sky.")]
        public ClampedFloatParameter            opacity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        /// <summary>Enable to cover only the upper part of the sky.</summary>
        [Tooltip("Check this box if the cloud layer covers only the upper part of the sky.")]
        public BoolParameter        upperHemisphereOnly = new BoolParameter(true);
        /// <summary>.</summary>
        [Tooltip("")]
        public VolumeParameter<CloudLayerMode>  mode    = new VolumeParameter<CloudLayerMode>();
        /// <summary>.</summary>
        [Tooltip("")]
        public VolumeParameter<CloudMapMode>    layers  = new VolumeParameter<CloudMapMode>();
        
        [Serializable]
        public class CloudSettings
        {
            /// <summary>Rotation of the clouds.</summary>
            [Tooltip("Sets the rotation of the clouds.")]
            public ClampedFloatParameter    rotation            = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
            /// <summary>Color multiplier of the clouds.</summary>
            [Tooltip("Specifies the color that HDRP uses to tint the clouds.")]
            public ColorParameter           tint                = new ColorParameter(Color.white);
            /// <summary>Intensity multipler of the clouds.</summary>
            [Tooltip("Sets the intensity multiplier for the clouds.")]
            public MinFloatParameter        intensityMultiplier = new MinFloatParameter(1.0f, 0.0f);

            /// <summary>Distortion mode.</summary>
            [Tooltip("Distortion mode.")]
            public VolumeParameter<CloudDistortionMode> distortion      = new VolumeParameter<CloudDistortionMode>();
            /// <summary>Direction of the distortion.</summary>
            [Tooltip("Sets the rotation of the distortion (in degrees).")]
            public ClampedFloatParameter                scrollDirection = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
            /// <summary>Speed of the distortion.</summary>
            [Tooltip("Sets the cloud scrolling speed. The higher the value, the faster the clouds will move.")]
            public MinFloatParameter                    scrollSpeed     = new MinFloatParameter(1.0f, 0.0f);
            /// <summary>Texture used to distort the UVs for the cloud layer.</summary>
            [Tooltip("Specify the flowmap HDRP uses for cloud distortion (in LatLong layout).")]
            public TextureParameter                     flowmap         = new TextureParameter(null);

            internal float scrollFactor = 0.0f;


            internal Vector4 GetBakingParameters()
            {
                Vector4 param = tint.value;
                param.w = rotation.value / 360.0f;
                return param;
            }

            internal Vector4 GetRenderingParameters()
            {
                float dir = -Mathf.Deg2Rad * scrollDirection.value;
                return new Vector4(intensityMultiplier.value, scrollFactor, Mathf.Cos(dir), Mathf.Sin(dir));
            }

            internal int GetBakingHashCode()
            {
                int hash = 17;

                unchecked
                {
                    hash = hash * 23 + rotation.GetHashCode();
                    hash = hash * 23 + tint.GetHashCode();
                }

                return hash;
            }
        }

        [Serializable]
        public class CloudLighting
        {
            /// <summary>Distortion mode.</summary>
            [Tooltip("Distortion mode.")]
            public VolumeParameter<CloudLightingMode>    lighting   = new VolumeParameter<CloudLightingMode>();
            /// <summary>Number of raymarching steps.</summary>
            [Tooltip("Number of raymarching steps.")]
            public ClampedIntParameter                  steps       = new ClampedIntParameter(4, 1, 10);
            /// <summary>.</summary>
            [Tooltip(".")]
            public FloatParameter                       thickness   = new FloatParameter(1.0f);
            
            /// <summary>Enable to cast shadows.</summary>
            [Tooltip("Cast Shadows.")]
            public BoolParameter    castShadows = new BoolParameter(false);


            internal int GetBakingHashCode(ref bool castShadows)
            {
                int hash = 17;

                unchecked
                {
                    hash = hash * 23 + lighting.GetHashCode();
                    hash = hash * 23 + steps.GetHashCode();
                    hash = hash * 23 + thickness.GetHashCode();
                    hash = hash * 23 + castShadows.GetHashCode();
                }

                castShadows |= this.castShadows.value;
                return hash;
            }
        }

        [Serializable]
        public class CloudMap
        {
            /// <summary>Texture used to render the clouds.</summary>
            [Tooltip("Specify the texture HDRP uses to render the clouds (in LatLong layout).")]
            public TextureParameter         cloudMap    = new TextureParameter(null);

            /// <summary>Opacity of the red layer.</summary>
            [Tooltip("Opacity of the red layer.")]
            public ClampedFloatParameter    opacityR    = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
            /// <summary>Opacity of the green layer.</summary>
            [Tooltip("Opacity of the green layer.")]
            public ClampedFloatParameter    opacityG    = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
            /// <summary>Opacity of the blue layer.</summary>
            [Tooltip("Opacity of the blue layer.")]
            public ClampedFloatParameter    opacityB    = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
            /// <summary>Opacity of the alpha layer.</summary>
            [Tooltip("Opacity of the alpha layer.")]
            public ClampedFloatParameter    opacityA    = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
            
            public CloudSettings settings = new CloudSettings();
            public CloudLighting lighting = new CloudLighting();

            public Vector4 Opacities => new Vector4(opacityR.value, opacityG.value, opacityB.value, opacityA.value);

            
            internal int GetBakingHashCode(ref bool castShadows)
            {
                int hash = 17;

                unchecked
                {
                    hash = hash * 23 + cloudMap.GetHashCode();
                    hash = hash * 23 + opacityR.GetHashCode();
                    hash = hash * 23 + opacityG.GetHashCode();
                    hash = hash * 23 + opacityB.GetHashCode();
                    hash = hash * 23 + opacityA.GetHashCode();

                    hash = hash * 23 + settings.GetBakingHashCode();
                    hash = hash * 23 + lighting.GetBakingHashCode(ref castShadows);
                }

                return hash;
            }
        }

        [Serializable]
        public class CloudCRT
        {
            /// <summary>Texture used to render the clouds.</summary>
            [Tooltip("Specify the CustomRenderTexture HDRP uses to render the clouds (in LatLong layout).")]
            public TextureParameter         cloudCRT    = new TextureParameter(null);
            
            public CloudSettings settings = new CloudSettings();
            public CloudLighting lighting = new CloudLighting();

            
            internal int GetBakingHashCode(ref bool castShadows)
            {
                int hash = 17;

                unchecked
                {
                    hash = hash * 23 + cloudCRT.GetHashCode();
                    hash = hash * 23 + settings.GetBakingHashCode();
                    hash = hash * 23 + lighting.GetBakingHashCode(ref castShadows);
                }

                return hash;
            }
        }

        public CloudMap mapA = new CloudMap();
        public CloudMap mapB = new CloudMap();
        public CloudCRT crt  = new CloudCRT();

        private float lastTime = 0.0f;


        CloudLayer()
        {
            displayName = "CloudLayer (Preview)";

        }

        /// <summary>Sets keywords and parameters on a sky material to render the cloud layer.</summary>
        /// <param name="layer">The cloud layer to apply.</param>
        /// <param name="skyMaterial">The sky material to change.</param>
        public static void Apply(BuiltinSkyParameters builtinParams, Material skyMaterial)
        {
            var layer = builtinParams.cloudLayer;
            if (layer != null && layer.opacity.value != 0.0f)
            {
                layer.mapA.settings.scrollFactor += layer.mapA.settings.scrollSpeed.value * (Time.time - layer.lastTime) * 0.01f;
                layer.lastTime = Time.time;

                Vector4 params1 = new Vector4(layer.opacity.value, layer.upperHemisphereOnly.value?1:0, 0, 0);
                Vector4 params2 = layer.mapA.settings.GetRenderingParameters();

                skyMaterial.EnableKeyword("USE_CLOUD_MAP");
                skyMaterial.SetTexture("_CloudTexture", builtinParams.cloudTexture);
                skyMaterial.SetVector("_CloudParams1", params1);
                skyMaterial.SetVector("_CloudParams2", params2);

                if (layer.mapA.settings.distortion.value != CloudDistortionMode.None)
                {
                    skyMaterial.EnableKeyword("USE_CLOUD_MOTION");
                    if (layer.mapA.settings.distortion.value == CloudDistortionMode.Procedural)
                        skyMaterial.DisableKeyword("USE_CLOUD_MAP");
                    else
                        skyMaterial.SetTexture(HDShaderIDs._CloudFlowmap, layer.mapA.settings.flowmap.value);
                }
                else
                    skyMaterial.DisableKeyword("USE_CLOUD_MOTION");
            }
            else
            {
                skyMaterial.DisableKeyword("USE_CLOUD_MAP");
                skyMaterial.DisableKeyword("USE_CLOUD_MOTION");
            }
        }

        internal int GetBakingHashCode(out bool castShadows)
        {
            int hash = 17;
            castShadows = false;

            unchecked
            {
                hash = hash * 23 + mode.GetHashCode();
                if (mode.value == CloudLayerMode.CloudMap)
                {
                    hash = hash * 23 + layers.GetHashCode();
                    hash = hash * 23 + mapA.GetBakingHashCode(ref castShadows);
                    if (layers.value == CloudMapMode.Double)
                        hash = hash * 23 + mapB.GetBakingHashCode(ref castShadows);
                }
                else
                    hash = hash * 23 + crt.GetBakingHashCode(ref castShadows);
            }

            return hash;
        }
    }
}
