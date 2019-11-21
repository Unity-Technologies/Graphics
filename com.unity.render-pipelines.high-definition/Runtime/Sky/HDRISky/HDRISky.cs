using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Sky/HDRI Sky")]
    [SkyUniqueID((int)SkyType.HDRI)]
    public class HDRISky : SkySettings
    {
        [Tooltip("Specify the cubemap HDRP uses to render the sky.")]
        public CubemapParameter         hdriSky             = new CubemapParameter(null);
        public BoolParameter            enableBackplate     = new BoolParameter(false);
        public BackplateTypeParameter   backplateType       = new BackplateTypeParameter(BackplateType.Disc);
        public FloatParameter           groundLevel         = new FloatParameter(0.0f);
        public Vector2Parameter         scale               = new Vector2Parameter(Vector2.one*32.0f);
        public MinFloatParameter        projectionDistance  = new MinFloatParameter(16.0f, 1e-7f);
        public ClampedFloatParameter    plateRotation       = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
        public ClampedFloatParameter    plateTexRotation    = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
        public Vector2Parameter         plateTexOffset      = new Vector2Parameter(Vector2.zero);
        public ClampedFloatParameter    blendAmount         = new ClampedFloatParameter(0.0f, 0.0f, 100.0f);
        public ColorParameter           shadowTint          = new ColorParameter(Color.grey);
        public BoolParameter            pointLightShadow    = new BoolParameter(false);
        public BoolParameter            dirLightShadow      = new BoolParameter(false);
        public BoolParameter            rectLightShadow     = new BoolParameter(false);

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
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
            }

            return hash;
        }

        public override Type GetSkyRendererType() { return typeof(HDRISkyRenderer); }
    }
}
