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
        /// <summary>Controls the global opacity of the cloud layer.</summary>
        [Tooltip("Controls the global opacity of the cloud layer.")]
        public ClampedFloatParameter            opacity = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
        /// <summary>Enable to cover only the upper part of the sky.</summary>
        [Tooltip("Check this box if the cloud layer covers only the upper part of the sky.")]
        public BoolParameter        upperHemisphereOnly = new BoolParameter(true);
        /// <summary>Choose the number of cloud layers.</summary>
        public VolumeParameter<CloudMapMode>    layers  = new VolumeParameter<CloudMapMode>();


        /// <summary>Controls the opacity of the cloud shadows.</summary>
        [Tooltip("Controls the opacity of the cloud shadows.")]
        public ClampedFloatParameter    shadowsOpacity      = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        /// <summary>Controls the tiling of the cloud shadows.</summary>
        [Tooltip("Controls the tiling of the cloud shadows.")]
        public MinFloatParameter        shadowsTiling       = new MinFloatParameter(500.0f, 0.0f);

        /// <summary>
        /// Cloud Map Volume Parameters.
        /// This groups parameters for one cloud map layer.
        /// </summary>
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

            /// <summary>Rotation of the clouds.</summary>
            [Tooltip("Sets the rotation of the clouds.")]
            public ClampedFloatParameter    rotation            = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
            /// <summary>Color multiplier of the clouds.</summary>
            [Tooltip("Specifies the color that HDRP uses to tint the clouds.")]
            public ColorParameter           tint                = new ColorParameter(Color.white);
            /// <summary>Exposure of the clouds.</summary>
            [Tooltip("Sets the exposure of the clouds in EV.")]
            public FloatParameter           exposure            = new FloatParameter(0.0f);

            /// <summary>Distortion mode.</summary>
            [Tooltip("Distortion mode.")]
            public VolumeParameter<CloudDistortionMode> distortionMode  = new VolumeParameter<CloudDistortionMode>();
            /// <summary>Direction of the distortion.</summary>
            [Tooltip("Sets the rotation of the distortion (in degrees).")]
            public ClampedFloatParameter                scrollDirection = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
            /// <summary>Speed of the distortion.</summary>
            [Tooltip("Sets the cloud scrolling speed. The higher the value, the faster the clouds will move.")]
            public MinFloatParameter                    scrollSpeed     = new MinFloatParameter(1.0f, 0.0f);
            /// <summary>Texture used to distort the UVs for the cloud layer.</summary>
            [Tooltip("Specify the flowmap HDRP uses for cloud distortion (in LatLong layout).")]
            public TextureParameter                     flowmap         = new TextureParameter(null);

            /// <summary>Lighting mode.</summary>
            [Tooltip("Lighting mode.")]
            public VolumeParameter<CloudLightingMode>    lightingMode   = new VolumeParameter<CloudLightingMode>();
            /// <summary>Number of raymarching steps.</summary>
            [Tooltip("Number of raymarching steps.")]
            public ClampedIntParameter                  steps           = new ClampedIntParameter(4, 1, 10);
            /// <summary>Thickness of the clouds.</summary>
            [Tooltip("Controls the thickness of the clouds.")]
            public ClampedFloatParameter                thickness       = new ClampedFloatParameter(0.5f, 0, 2);

            /// <summary>Enable to cast shadows.</summary>
            [Tooltip("Enable or disable cloud shadows.")]
            public BoolParameter    castShadows = new BoolParameter(false);


            internal float scrollFactor = 0.0f;
            internal int NumSteps => (lightingMode == CloudLightingMode.Raymarching) ? steps.value : 0;
            internal Vector4 Opacities => new Vector4(opacityR.value, opacityG.value, opacityB.value, opacityA.value);


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

            internal (Vector4, Vector4) GetRenderingParameters()
            {
                float dir = -Mathf.Deg2Rad * scrollDirection.value;
                var params1 = new Vector4(Mathf.Cos(dir), Mathf.Sin(dir), scrollFactor, 0);
                Vector4 params2 = tint.value * ColorUtils.ConvertEV100ToExposure(-exposure.value);
                return (params1, params2);
            }

            internal bool Apply(Material skyMaterial, string mapKeyword, string motionKeyword)
            {
                if (distortionMode.value != CloudDistortionMode.None)
                {
                    skyMaterial.EnableKeyword(motionKeyword);
                    if (distortionMode.value == CloudDistortionMode.Flowmap)
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
                if (distortionMode.value != CloudDistortionMode.None)
                {
                    cs.EnableKeyword(motionKeyword);
                    if (distortionMode.value == CloudDistortionMode.Flowmap)
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

            internal int GetBakingHashCode(ref bool cloudShadows)
            {
                int hash = 17;

                unchecked
                {
                    hash = hash * 23 + cloudMap.GetHashCode();
                    hash = hash * 23 + opacityR.GetHashCode();
                    hash = hash * 23 + opacityG.GetHashCode();
                    hash = hash * 23 + opacityB.GetHashCode();
                    hash = hash * 23 + opacityA.GetHashCode();

                    hash = hash * 23 + rotation.GetHashCode();

                    hash = hash * 23 + lightingMode.GetHashCode();
                    hash = hash * 23 + steps.GetHashCode();
                    hash = hash * 23 + thickness.GetHashCode();
                    hash = hash * 23 + castShadows.GetHashCode();

#if UNITY_EDITOR
                    // In the editor, we want to rebake the texture if the texture content is modified
                    if (cloudMap.value != null)
                        hash = hash * 23 + cloudMap.value.imageContentsHash.GetHashCode();
#endif
                }

                cloudShadows |= castShadows.value;
                return hash;
            }
        }

        /// <summary>Layer A.</summary>
        public CloudMap layerA = new CloudMap();
        /// <summary>Layer B.</summary>
        public CloudMap layerB = new CloudMap();

        private float lastTime = 0.0f;
        static internal Vector4[] vectorArray = new Vector4[2];


        CloudLayer()
        {
            displayName = "CloudLayer (Preview)";

        }

        /// <summary>Sets keywords and parameters on a sky material to render the cloud layer.</summary>
        /// <param name="builtinParams">The builtin sky parameters.</param>
        /// <param name="skyMaterial">The sky material.</param>
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

            float dt = (Time.time - layer.lastTime) * 0.01f;
            layer.layerA.scrollFactor += layer.layerA.scrollSpeed.value * dt;
            layer.layerB.scrollFactor += layer.layerB.scrollSpeed.value * dt;
            layer.lastTime = Time.time;

            var paramsA = layer.layerA.GetRenderingParameters();
            var paramsB = layer.layerB.GetRenderingParameters();
            paramsA.Item1.w = layer.opacity.value;
            paramsB.Item1.w = layer.upperHemisphereOnly.value ? 1 : 0;

            skyMaterial.SetTexture(HDShaderIDs._CloudTexture, builtinParams.cloudTexture);
            vectorArray[0] = paramsA.Item1; vectorArray[1] = paramsB.Item1;
            skyMaterial.SetVectorArray(HDShaderIDs._CloudParams1, vectorArray);
            vectorArray[0] = paramsA.Item2; vectorArray[1] = paramsB.Item2;
            skyMaterial.SetVectorArray(HDShaderIDs._CloudParams2, vectorArray);

            if (layer.layerA.Apply(skyMaterial, "USE_CLOUD_MAP", "USE_CLOUD_MOTION"))
                skyMaterial.SetTexture(HDShaderIDs._CloudFlowmap1, layer.layerA.flowmap.value);

            if (layer.layers.value == CloudMapMode.Double)
            {
                if (layer.layerB.Apply(skyMaterial, "USE_SECOND_CLOUD_MAP", "USE_SECOND_CLOUD_MOTION"))
                    skyMaterial.SetTexture(HDShaderIDs._CloudFlowmap2, layer.layerB.flowmap.value);
            }
            else
            {
                skyMaterial.DisableKeyword("USE_SECOND_CLOUD_MAP");
                skyMaterial.DisableKeyword("USE_SECOND_CLOUD_MOTION");
            }
        }

        internal void SetComputeParams(CommandBuffer cmd, ComputeShader cs, int kernel)
        {
            var paramsA = layerA.GetRenderingParameters();
            var paramsB = layerB.GetRenderingParameters();
            paramsA.Item1.w = opacity.value;
            paramsB.Item1.w = upperHemisphereOnly.value ? 1 : 0;

            vectorArray[0] = paramsA.Item1; vectorArray[1] = paramsB.Item1;
            cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._CloudParams1, vectorArray);

            if (layerA.SetComputeParams(cs, "USE_CLOUD_MAP", "USE_CLOUD_MOTION"))
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CloudFlowmap1, layerA.flowmap.value);

            if (layers.value == CloudMapMode.Double)
            {
                if (layerB.SetComputeParams(cs, "USE_SECOND_CLOUD_MAP", "USE_SECOND_CLOUD_MOTION"))
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CloudFlowmap2, layerB.flowmap.value);
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
                hash = hash * 23 + layers.GetHashCode();
                hash = hash * 23 + layerA.GetBakingHashCode(ref castShadows);
                if (layers.value == CloudMapMode.Double)
                {
                    hash = hash * 23 + layerB.GetBakingHashCode(ref castShadows);
                    numLayers = 2;
                }
            }

            return hash;
        }
    }
}
