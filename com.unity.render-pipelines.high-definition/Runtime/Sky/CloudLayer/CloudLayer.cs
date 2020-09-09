using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Cloud Layer Volume Component.
    /// This component setups the cloud layer for rendering.
    /// </summary>
    [VolumeComponentMenu("Sky/Cloud Layer (Preview)")]
    public class CloudLayer : VolumeComponent
    {
        /// <summary>Enable fog.</summary>
        [Tooltip("Check to have a cloud layer in the sky.")]
        public BoolParameter         enabled                = new BoolParameter(false);

        /// <summary>Texture used to render the clouds.</summary>
        [Tooltip("Specify the texture HDRP uses to render the clouds (in LatLong layout).")]
        public TextureParameter         cloudMap            = new TextureParameter(null);
        /// <summary>Enable to cover only the upper part of the sky.</summary>
        [Tooltip("Check this box if the cloud layer covers only the upper part of the sky.")]
        public BoolParameter            upperHemisphereOnly = new BoolParameter(true);
        /// <summary>Color multiplier of the clouds.</summary>
        [Tooltip("Specifies the color that HDRP uses to tint the clouds.")]
        public ColorParameter           tint                = new ColorParameter(Color.white);
        /// <summary>Intensity multipler of the clouds.</summary>
        [Tooltip("Sets the intensity multiplier for the clouds.")]
        public MinFloatParameter        intensityMultiplier = new MinFloatParameter(1.0f, 0.0f);
        /// <summary>Rotation of the clouds.</summary>
        [Tooltip("Sets the rotation of the clouds.")]
        public ClampedFloatParameter    rotation            = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);

        /// <summary>Enable to have cloud distortion.</summary>
        [Tooltip("Enable or disable cloud distortion.")]
        public BoolParameter            enableDistortion    = new BoolParameter(false);
        /// <summary>Enable to have a simple, procedural distorsion.</summary>
        [Tooltip("If enabled, the clouds will be distorted by a constant wind.")]
        public BoolParameter            procedural          = new BoolParameter(true);
        /// <summary>Texture used to distort the UVs for the cloud layer.</summary>
        [Tooltip("Specify the flowmap HDRP uses for cloud distortion (in LatLong layout).")]
        public TextureParameter         flowmap             = new TextureParameter(null);
        /// <summary>Direction of the distortion.</summary>
        [Tooltip("Sets the rotation of the distortion (in degrees).")]
        public ClampedFloatParameter    scrollDirection     = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
        /// <summary>Speed of the distortion.</summary>
        [Tooltip("Sets the cloud scrolling speed. The higher the value, the faster the clouds will move.")]
        public MinFloatParameter        scrollSpeed         = new MinFloatParameter(1.0f, 0.0f);


        private float scrollFactor = 0.0f, lastTime = 0.0f;

        CloudLayer()
        {
            displayName = "CloudLayer (Preview)";

        }

        /// <summary>
        /// Returns the shader parameters of the cloud layer.
        /// </summary>
        /// <returns>The shader parameters of the cloud layer.</returns>
        public Vector4 GetParameters()
        {
                float rot = -Mathf.Deg2Rad*scrollDirection.value;
                float upper = upperHemisphereOnly.value ? 1.0f : -1.0f;
                return new Vector4(upper * (rotation.value / 360.0f + 1), scrollFactor, Mathf.Cos(rot), Mathf.Sin(rot));
        }

        /// <summary>Sets keywords and parameters on a sky material to render the cloud layer.</summary>
        /// <param name="layer">The cloud layer to apply.</param>
        /// <param name="skyMaterial">The sky material to change.</param>
        public static void Apply(CloudLayer layer, Material skyMaterial)
        {
            if (layer != null && layer.enabled.value == true)
            {
                layer.scrollFactor += layer.scrollSpeed.value * (Time.time - layer.lastTime) * 0.01f;
                layer.lastTime = Time.time;

                Vector4 cloudParam = layer.GetParameters();
                Vector4 cloudParam2 = layer.tint.value;
                cloudParam2.w = layer.intensityMultiplier.value;

                skyMaterial.EnableKeyword("USE_CLOUD_MAP");
                skyMaterial.SetTexture(HDShaderIDs._CloudMap, layer.cloudMap.value);
                skyMaterial.SetVector(HDShaderIDs._CloudParam, cloudParam);
                skyMaterial.SetVector(HDShaderIDs._CloudParam2, cloudParam2);

                if (layer.enableDistortion.value == true)
                {
                    skyMaterial.EnableKeyword("USE_CLOUD_MOTION");
                    if (layer.procedural.value == true)
                        skyMaterial.DisableKeyword("USE_CLOUD_MAP");
                    else
                        skyMaterial.SetTexture(HDShaderIDs._CloudFlowmap, layer.flowmap.value);
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
                hash = hash * 23 + cloudMap.GetHashCode();
                hash = hash * 23 + flowmap.GetHashCode();
                hash = hash * 23 + enabled.GetHashCode();
                hash = hash * 23 + upperHemisphereOnly.GetHashCode();
                hash = hash * 23 + tint.GetHashCode();
                hash = hash * 23 + intensityMultiplier.GetHashCode();
                hash = hash * 23 + rotation.GetHashCode();
                hash = hash * 23 + enableDistortion.GetHashCode();
                hash = hash * 23 + procedural.GetHashCode();
                hash = hash * 23 + scrollDirection.GetHashCode();
                hash = hash * 23 + scrollSpeed.GetHashCode();
            }

            return hash;
        }
    }
}
