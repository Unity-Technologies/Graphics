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
    /// Distortion Mode.
    /// </summary>
    public enum DistortionMode
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
        public class CloudLighting
        {
            /// <summary>Distortion mode.</summary>
            [Tooltip("Distortion mode.")]
            public VolumeParameter<CloudLightingMode>    lighting   = new VolumeParameter<CloudLightingMode>();

            /// <summary>Number of raymarching steps.</summary>
            [Tooltip("Number of raymarching steps.")]
            public ClampedIntParameter  steps       = new ClampedIntParameter(4, 1, 10);
            /// <summary>.</summary>
            [Tooltip(".")]
            public FloatParameter       thickness   = new FloatParameter(1.0f);
            
            /// <summary>Enable to cast shadows.</summary>
            [Tooltip("Cast Shadows.")]
            public BoolParameter    castShadows =   new BoolParameter(false);
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
            public VolumeParameter<DistortionMode>      distortion      = new VolumeParameter<DistortionMode>();
            /// <summary>Direction of the distortion.</summary>
            [Tooltip("Sets the rotation of the distortion (in degrees).")]
            public ClampedFloatParameter                scrollDirection = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
            /// <summary>Speed of the distortion.</summary>
            [Tooltip("Sets the cloud scrolling speed. The higher the value, the faster the clouds will move.")]
            public MinFloatParameter                    scrollSpeed     = new MinFloatParameter(1.0f, 0.0f);
            /// <summary>Texture used to distort the UVs for the cloud layer.</summary>
            [Tooltip("Specify the flowmap HDRP uses for cloud distortion (in LatLong layout).")]
            public TextureParameter                     flowmap         = new TextureParameter(null);

            public CloudLighting lighting = new CloudLighting();

            internal float scrollFactor = 0.0f;

            internal (Vector4, Vector4) GetParameters()
            {
                float dir = -Mathf.Deg2Rad * scrollDirection.value;
                Vector4 tintAndIntensity = tint.value;
                tintAndIntensity.w = intensityMultiplier.value;
                return (new Vector4(rotation.value / 360.0f, scrollFactor, Mathf.Cos(dir), Mathf.Sin(dir)), tintAndIntensity);
            }
            
            public override int GetHashCode()
            {
                int hash = base.GetHashCode();

                unchecked
                {
                    hash = hash * 23 + cloudMap.GetHashCode();
                    hash = hash * 23 + opacityR.GetHashCode();
                    hash = hash * 23 + opacityG.GetHashCode();
                    hash = hash * 23 + opacityB.GetHashCode();
                    hash = hash * 23 + opacityA.GetHashCode();
                    hash = hash * 23 + rotation.GetHashCode();
                    hash = hash * 23 + tint.GetHashCode();
                    hash = hash * 23 + intensityMultiplier.GetHashCode();
                    hash = hash * 23 + distortion.GetHashCode();
                    hash = hash * 23 + scrollDirection.GetHashCode();
                    hash = hash * 23 + scrollSpeed.GetHashCode();
                    hash = hash * 23 + flowmap.GetHashCode();

                    hash = hash * 23 + lighting.lighting.GetHashCode();
                    hash = hash * 23 + lighting.steps.GetHashCode();
                    hash = hash * 23 + lighting.thickness.GetHashCode();
                    hash = hash * 23 + lighting.castShadows.GetHashCode();
                }

                return hash;
            }
        }

        public CloudMap mapA = new CloudMap();
        public CloudMap mapB = new CloudMap();


        private float lastTime = 0.0f;

        CloudLayer()
        {
            displayName = "CloudLayer (Preview)";

        }

        /// <summary>Sets keywords and parameters on a sky material to render the cloud layer.</summary>
        /// <param name="layer">The cloud layer to apply.</param>
        /// <param name="skyMaterial">The sky material to change.</param>
        public static void Apply(CloudLayer layer, Material skyMaterial)
        {
            if (layer != null && layer.opacity.value != 0.0f)
            {
                layer.mapA.scrollFactor += layer.mapA.scrollSpeed.value * (Time.time - layer.lastTime) * 0.01f;
                layer.lastTime = Time.time;

                (Vector4, Vector4) paramsA = layer.mapA.GetParameters();

                skyMaterial.EnableKeyword("USE_CLOUD_MAP");
                skyMaterial.SetTexture(HDShaderIDs._CloudMap, layer.mapA.cloudMap.value);
                skyMaterial.SetVector(HDShaderIDs._CloudParam, paramsA.Item1);
                skyMaterial.SetVector(HDShaderIDs._CloudParam2, paramsA.Item2);

                if (layer.mapA.distortion.value != DistortionMode.None)
                {
                    skyMaterial.EnableKeyword("USE_CLOUD_MOTION");
                    if (layer.mapA.distortion.value == DistortionMode.Procedural)
                        skyMaterial.DisableKeyword("USE_CLOUD_MAP");
                    else
                        skyMaterial.SetTexture(HDShaderIDs._CloudFlowmap, layer.mapA.flowmap.value);
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

        /// <summary>
        /// Returns the hash code of the HDRI sky parameters.
        /// </summary>
        /// <returns>The hash code of the HDRI sky parameters.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                hash = hash * 23 + opacity.GetHashCode();
                hash = hash * 23 + upperHemisphereOnly.GetHashCode();
                hash = hash * 23 + mode.GetHashCode();
                hash = hash * 23 + layers.GetHashCode();
                hash = hash * 23 + mapA.GetHashCode();
                hash = hash * 23 + mapB.GetHashCode();
            }

            return hash;
        }
    }
}
