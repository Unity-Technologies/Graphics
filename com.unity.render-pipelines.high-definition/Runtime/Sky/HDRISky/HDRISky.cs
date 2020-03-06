using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// HDRI Sky Volume Component.
    /// This component setups HDRI sky for rendering.
    /// </summary>
    [VolumeComponentMenu("Sky/HDRI Sky")]
    [SkyUniqueID((int)SkyType.HDRI)]
    public class HDRISky : SkySettings
    {
        /// <summary>Cubemap used to render the HDRI sky.</summary>
        [Tooltip("Specify the cubemap HDRP uses to render the sky.")]
        public CubemapParameter         hdriSky             = new CubemapParameter(null);
        /// <summary>Enable Backplate to have it visible.</summary>
        [Tooltip("Enable or disable the backplate.")]
        public BoolParameter            enableBackplate     = new BoolParameter(false);
        /// <summary>Backplate Type {Disc, Rectangle, Ellipse, Infinite (Plane)}.</summary>
        [Tooltip("Backplate type.")]
        public BackplateTypeParameter   backplateType       = new BackplateTypeParameter(BackplateType.Disc);
        /// <summary>Define the ground level of the Backplate.</summary>
        [Tooltip("Define the ground level of the Backplate.")]
        public FloatParameter           groundLevel         = new FloatParameter(0.0f);
        /// <summary>Extent of the Backplate (if circle only the X value is considered).</summary>
        [Tooltip("Extent of the Backplate (if circle only the X value is considered).")]
        public Vector2Parameter         scale               = new Vector2Parameter(Vector2.one*32.0f);
        /// <summary>Backplate's projection distance to varying the cubemap projection on the plate.</summary>
        [Tooltip("Backplate's projection distance to varying the cubemap projection on the plate.")]
        public MinFloatParameter        projectionDistance  = new MinFloatParameter(16.0f, 1e-7f);
        /// <summary>Backplate rotation parameter for the geometry.</summary>
        [Tooltip("Backplate rotation parameter for the geometry.")]
        public ClampedFloatParameter    plateRotation       = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
        /// <summary>Backplate rotation parameter for the projected texture.</summary>
        [Tooltip("Backplate rotation parameter for the projected texture.")]
        public ClampedFloatParameter    plateTexRotation    = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
        /// <summary>Backplate projection offset on the plane.</summary>
        [Tooltip("Backplate projection offset on the plane.")]
        public Vector2Parameter         plateTexOffset      = new Vector2Parameter(Vector2.zero);
        /// <summary>Backplate blend parameter to blend the edge of the backplate with the background.</summary>
        [Tooltip("Backplate blend parameter to blend the edge of the backplate with the background.")]
        public ClampedFloatParameter    blendAmount         = new ClampedFloatParameter(0.0f, 0.0f, 100.0f);
        /// <summary>Backplate Shadow Tint projected on the plane.</summary>
        [Tooltip("Backplate Shadow Tint projected on the plane.")]
        public ColorParameter           shadowTint          = new ColorParameter(Color.grey);
        /// <summary>Allow backplate to receive shadow from point light.</summary>
        [Tooltip("Allow backplate to receive shadow from point light.")]
        public BoolParameter            pointLightShadow    = new BoolParameter(false);
        /// <summary>Allow backplate to receive shadow from directional light.</summary>
        [Tooltip("Allow backplate to receive shadow from directional light.")]
        public BoolParameter            dirLightShadow      = new BoolParameter(false);
        /// <summary>Allow backplate to receive shadow from Area light.</summary>
        [Tooltip("Allow backplate to receive shadow from Area light.")]
        public BoolParameter            rectLightShadow     = new BoolParameter(false);

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
                hash = hdriSky.value != null ? hash * 23 + hdriSky.value.GetHashCode() : hash;
                hash = hash * 23 + enableBackplate.value.GetHashCode();
                hash = hash * 23 + backplateType.value.GetHashCode();
                hash = hash * 23 + groundLevel.value.GetHashCode();
                hash = hash * 23 + scale.value.GetHashCode();
                hash = hash * 23 + projectionDistance.value.GetHashCode();
                hash = hash * 23 + plateRotation.value.GetHashCode();
                hash = hash * 23 + plateTexRotation.value.GetHashCode();
                hash = hash * 23 + plateTexOffset.value.GetHashCode();
                hash = hash * 23 + blendAmount.value.GetHashCode();
                hash = hash * 23 + shadowTint.value.GetHashCode();
                hash = hash * 23 + pointLightShadow.value.GetHashCode();
                hash = hash * 23 + dirLightShadow.value.GetHashCode();
                hash = hash * 23 + rectLightShadow.value.GetHashCode();

                hash = hdriSky.value != null ? hash * 23 + hdriSky.overrideState.GetHashCode() : hash;
                hash = hash * 23 + enableBackplate.overrideState.GetHashCode();
                hash = hash * 23 + backplateType.overrideState.GetHashCode();
                hash = hash * 23 + groundLevel.overrideState.GetHashCode();
                hash = hash * 23 + scale.overrideState.GetHashCode();
                hash = hash * 23 + projectionDistance.overrideState.GetHashCode();
                hash = hash * 23 + plateRotation.overrideState.GetHashCode();
                hash = hash * 23 + plateTexRotation.overrideState.GetHashCode();
                hash = hash * 23 + plateTexOffset.overrideState.GetHashCode();
                hash = hash * 23 + blendAmount.overrideState.GetHashCode();
                hash = hash * 23 + shadowTint.overrideState.GetHashCode();
                hash = hash * 23 + pointLightShadow.overrideState.GetHashCode();
                hash = hash * 23 + dirLightShadow.overrideState.GetHashCode();
                hash = hash * 23 + rectLightShadow.overrideState.GetHashCode();
#else
                hash = hdriSky.value != null ? hash * 23 + hdriSky.GetHashCode() : hash;
                hash = hash * 23 + enableBackplate.GetHashCode();
                hash = hash * 23 + backplateType.GetHashCode();
                hash = hash * 23 + groundLevel.GetHashCode();
                hash = hash * 23 + scale.GetHashCode();
                hash = hash * 23 + projectionDistance.GetHashCode();
                hash = hash * 23 + plateRotation.GetHashCode();
                hash = hash * 23 + plateTexRotation.GetHashCode();
                hash = hash * 23 + plateTexOffset.GetHashCode();
                hash = hash * 23 + blendAmount.GetHashCode();
                hash = hash * 23 + shadowTint.GetHashCode();
                hash = hash * 23 + pointLightShadow.GetHashCode();
                hash = hash * 23 + dirLightShadow.GetHashCode();
                hash = hash * 23 + rectLightShadow.GetHashCode();
#endif
            }

            return hash;
        }

        /// <summary>
        /// Returns HDRISkyRenderer type.
        /// </summary>
        /// <returns>HDRISkyRenderer type.</returns>
        public override Type GetSkyRendererType() { return typeof(HDRISkyRenderer); }
    }
}
