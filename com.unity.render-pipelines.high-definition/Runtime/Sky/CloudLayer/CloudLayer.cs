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
        public ClampedFloatParameter            opacity = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
        /// <summary>Enable to cover only the upper part of the sky.</summary>
        [Tooltip("Check this box if the cloud layer covers only the upper part of the sky.")]
        public BoolParameter        upperHemisphereOnly = new BoolParameter(true);
        /// <summary>Select the cloud layer mode.</summary>
        [Tooltip("Choose the cloud layer mode;")]
        public VolumeParameter<CloudLayerMode>  mode    = new VolumeParameter<CloudLayerMode>();
        /// <summary>Choose the number of cloud layers.</summary>
        public VolumeParameter<CloudMapMode>    layers  = new VolumeParameter<CloudMapMode>();
       

        /// <summary>Controls the opacity of the cloud shadows.</summary>
        [Tooltip("Controls the opacity of the cloud shadows.")]
        public ClampedFloatParameter    shadowsOpacity      = new ClampedFloatParameter(0.5f, 0.0f, 4.0f);
        /// <summary>Controls the scale of the cloud shadows.</summary>
        [Tooltip("Controls the scale of the cloud shadows.")]
        public MinFloatParameter        shadowsScale        = new MinFloatParameter(500.0f, 0.0f); 


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
            public MinFloatParameter        intensityMultiplier = new MinFloatParameter(10.0f, 0.0f);

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


            internal (Vector4, Vector4) GetRenderingParameters()
            {
                float dir = -Mathf.Deg2Rad * scrollDirection.value;
                var params1 = new Vector4(Mathf.Cos(dir), Mathf.Sin(dir), scrollFactor, 0);
                Vector4 params2 = tint.value;
                params2.w = intensityMultiplier.value;
                return (params1, params2);
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
            /// <summary>Thickness of the clouds.</summary>
            [Tooltip("Controls the thickness of the clouds.")]
            public ClampedFloatParameter                thickness   = new ClampedFloatParameter(1, 0, 2);

            /// <summary>Enable to cast shadows.</summary>
            [Tooltip("Enable or disable cloud shadows.")]
            public BoolParameter    castShadows = new BoolParameter(false);

            internal int NumSteps => (lighting == CloudLightingMode.Raymarching) ? steps.value : 0;


            internal int GetBakingHashCode(ref bool cloudShadows)
            {
                int hash = 17;

                unchecked
                {
                    hash = hash * 23 + lighting.GetHashCode();
                    hash = hash * 23 + steps.GetHashCode();
                    hash = hash * 23 + thickness.GetHashCode();
                    hash = hash * 23 + castShadows.GetHashCode();
                }

                cloudShadows |= castShadows.value;
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
            public ClampedFloatParameter    opacityG    = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
            /// <summary>Opacity of the blue layer.</summary>
            [Tooltip("Opacity of the blue layer.")]
            public ClampedFloatParameter    opacityB    = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
            /// <summary>Opacity of the alpha layer.</summary>
            [Tooltip("Opacity of the alpha layer.")]
            public ClampedFloatParameter    opacityA    = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
            
            public CloudSettings settings = new CloudSettings();
            public CloudLighting lighting = new CloudLighting();

            internal Vector4 Opacities => new Vector4(opacityR.value, opacityG.value, opacityB.value, opacityA.value);


            internal (Vector4, Vector4) GetBakingParameters()
            {
                Vector4 parameters = new Vector4(
                    settings.rotation.value / 360.0f,
                    lighting.NumSteps,
                    lighting.thickness.value,
                    0
                );
                return (Opacities, parameters);
            }

            internal bool Apply(Material skyMaterial, string mapKeyword, string motionKeyword)
            {
                if (settings.distortion.value != CloudDistortionMode.None)
                {
                    skyMaterial.EnableKeyword(motionKeyword);
                    if (settings.distortion.value == CloudDistortionMode.Flowmap)
                    {
                        skyMaterial.EnableKeyword(mapKeyword);
                        return true;
                    }
                    else
                        skyMaterial.DisableKeyword(mapKeyword);
                }
                else
                {
                    skyMaterial.EnableKeyword(mapKeyword);
                    skyMaterial.DisableKeyword(motionKeyword);
                }
                return false;
            }

            internal bool SetComputeParams(ComputeShader cs, string mapKeyword, string motionKeyword)
            {
                if (settings.distortion.value != CloudDistortionMode.None)
                {
                    cs.EnableKeyword(motionKeyword);
                    if (settings.distortion.value == CloudDistortionMode.Flowmap)
                    {
                        cs.EnableKeyword(mapKeyword);
                        return true;
                    }
                    else
                        cs.DisableKeyword(mapKeyword);
                }
                else
                {
                    cs.EnableKeyword(mapKeyword);
                    cs.DisableKeyword(motionKeyword);
                }
                return false;
            }
            
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

                    hash = hash * 23 + settings.rotation.GetHashCode();
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
                    hash = hash * 23 + settings.rotation.GetHashCode();
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
            if (layer == null || layer.opacity.value == 0.0f)
            {
                skyMaterial.DisableKeyword("USE_CLOUD_MAP");
                skyMaterial.DisableKeyword("USE_CLOUD_MOTION");
                skyMaterial.DisableKeyword("USE_SECOND_CLOUD_MAP");
                skyMaterial.DisableKeyword("USE_SECOND_CLOUD_MOTION");
                return;
            }

            if (layer.mode.value == CloudLayerMode.CloudMap)
            {
                float dt = (Time.time - layer.lastTime) * 0.01f;
                layer.mapA.settings.scrollFactor += layer.mapA.settings.scrollSpeed.value * dt;
                layer.mapB.settings.scrollFactor += layer.mapB.settings.scrollSpeed.value * dt;
                layer.lastTime = Time.time;

                var paramsA = layer.mapA.settings.GetRenderingParameters();
                var paramsB = layer.mapB.settings.GetRenderingParameters();
                paramsA.Item1.w = layer.opacity.value;
                paramsB.Item1.w = layer.upperHemisphereOnly.value ? 1 : 0;

                skyMaterial.SetTexture("_CloudTexture", builtinParams.cloudTexture);
                skyMaterial.SetVectorArray("_CloudParams1", new Vector4[]{ paramsA.Item1, paramsB.Item1 });
                skyMaterial.SetVectorArray("_CloudParams2", new Vector4[]{ paramsA.Item2, paramsB.Item2 });

                if (layer.mapA.Apply(skyMaterial, "USE_CLOUD_MAP", "USE_CLOUD_MOTION"))
                    skyMaterial.SetTexture("_CloudFlowmap1", layer.mapA.settings.flowmap.value);

                if (layer.layers.value == CloudMapMode.Double)
                {
                    if (layer.mapB.Apply(skyMaterial, "USE_SECOND_CLOUD_MAP", "USE_SECOND_CLOUD_MOTION"))
                        skyMaterial.SetTexture("_CloudFlowmap2", layer.mapB.settings.flowmap.value);
                }
                else
                {
                    skyMaterial.DisableKeyword("USE_SECOND_CLOUD_MAP");
                    skyMaterial.DisableKeyword("USE_SECOND_CLOUD_MOTION");
                }
            }
        }

        internal void SetComputeParams(CommandBuffer cmd, ComputeShader cs, int kernel)
        {
            var paramsA = mapA.settings.GetRenderingParameters();
            var paramsB = mapB.settings.GetRenderingParameters();
            paramsA.Item1.w = opacity.value;
            paramsB.Item1.w = upperHemisphereOnly.value ? 1 : 0;

            cmd.SetComputeVectorArrayParam(cs, "_CloudParams1", new Vector4[]{ paramsA.Item1, paramsB.Item1 });

            if (mapA.SetComputeParams(cs, "USE_CLOUD_MAP", "USE_CLOUD_MOTION"))
                cmd.SetComputeTextureParam(cs, kernel, "_CloudFlowmap1", mapA.settings.flowmap.value);

            if (layers.value == CloudMapMode.Double)
            {
                if (mapB.SetComputeParams(cs, "USE_SECOND_CLOUD_MAP", "USE_SECOND_CLOUD_MOTION"))
                    cmd.SetComputeTextureParam(cs, kernel, "_CloudFlowmap2", mapB.settings.flowmap.value);
            }
            else
            {
                cs.DisableKeyword("USE_SECOND_CLOUD_MAP");
                cs.DisableKeyword("USE_SECOND_CLOUD_MOTION");
            }
        }

        internal int GetBakingHashCode(out int numLayers, out bool castShadows)
        {
            int hash = 17;
            castShadows = false;
            numLayers = 1;

            unchecked
            {
                hash = hash * 23 + mode.GetHashCode();
                if (mode.value == CloudLayerMode.CloudMap)
                {
                    hash = hash * 23 + layers.GetHashCode();
                    hash = hash * 23 + mapA.GetBakingHashCode(ref castShadows);
                    if (layers.value == CloudMapMode.Double)
                    {
                        hash = hash * 23 + mapB.GetBakingHashCode(ref castShadows);
                        numLayers = 2;
                    }
                }
                else
                    hash = hash * 23 + crt.GetBakingHashCode(ref castShadows);
            }

            return hash;
        }
    }
}
