using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Cloud Layer Volume Component.
    /// This component setups the cloud layer for rendering.
    /// </summary>
    [VolumeComponentMenu("Sky/Cloud Layer")]
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
        public MinFloatParameter        scrollSpeed         = new MinFloatParameter(2.0f, 0.0f);


        private float scrollFactor = 0.0f, lastTime = 0.0f;

        /// <summary>
        /// Returns the shader parameters of the cloud layer.
        /// </summary>
        /// <returns>The shader parameters of the cloud layer.</returns>
        public Vector4 GetParameters()
        {
                scrollFactor += scrollSpeed.value * (Time.time - lastTime) * 0.01f;
                lastTime = Time.time;

                float rot = -Mathf.Deg2Rad*scrollDirection.value;
                bool upperOnly = upperHemisphereOnly.value || procedural.value;
                return new Vector4(upperOnly ? 1.0f : 0.0f, scrollFactor, Mathf.Cos(rot), Mathf.Sin(rot));
        }

        /// <summary>
        /// Sets keywords and parameters on a sky material to render the cloud layer.
        /// </summary>
        public static void Apply(CloudLayer layer, Material skyMaterial)
        {
            if (layer != null && layer.enabled.value == true)
            {
                Vector4 cloudParam = layer.GetParameters();

                skyMaterial.EnableKeyword("USE_CLOUD_MAP");
                skyMaterial.SetTexture(HDShaderIDs._CloudMap, layer.cloudMap.value);
                skyMaterial.SetVector(HDShaderIDs._CloudParam, cloudParam);

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
#if UNITY_2019_3 // In 2019.3, when we call GetHashCode on a VolumeParameter it generate garbage (due to the boxing of the generic parameter)
                hash = cloudMap.value != null ? hash * 23 + cloudMap.value.GetHashCode() : hash;
                hash = flowmap.value != null ? hash * 23 + flowmap.value.GetHashCode() : hash;
                hash = hash * 23 + upperHemisphereOnly.value.GetHashCode();
                hash = hash * 23 + enableDistortion.value.GetHashCode();
                hash = hash * 23 + procedural.value.GetHashCode();
                hash = hash * 23 + scrollDirection.value.GetHashCode();
                hash = hash * 23 + scrollSpeed.value.GetHashCode();

                hash = cloudMap.value != null ? hash * 23 + cloudMap.overrideState.GetHashCode() : hash;
                hash = flowmap.value != null ? hash * 23 + flowmap.overrideState.GetHashCode() : hash;
                hash = hash * 23 + upperHemisphereOnly.overrideState.GetHashCode();
                hash = hash * 23 + enableDistortion.overrideState.GetHashCode();
                hash = hash * 23 + procedural.overrideState.GetHashCode();
                hash = hash * 23 + scrollDirection.overrideState.GetHashCode();
                hash = hash * 23 + scrollSpeed.overrideState.GetHashCode();
#else
                hash = cloudMap.value != null ? hash * 23 + cloudMap.GetHashCode() : hash;
                hash = flowmap.value != null ? hash * 23 + flowmap.GetHashCode() : hash;
                hash = hash * 23 + upperHemisphereOnly.GetHashCode();
                hash = hash * 23 + enableDistortion.GetHashCode();
                hash = hash * 23 + procedural.GetHashCode();
                hash = hash * 23 + scrollDirection.GetHashCode();
                hash = hash * 23 + scrollSpeed.GetHashCode();
#endif
            }

            return hash;
        }
    }
}
