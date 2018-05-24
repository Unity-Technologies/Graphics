namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    public enum UpgradeSurfaceType
    {
        Opaque,
        Transparent
    }

    public enum UpgradeBlendMode
    {
        Alpha,
        Premultiply,
        Additive,
        Multiply
    }

    public enum SpecularSource
    {
        SpecularTextureAndColor,
        NoSpecular
    }

    public enum GlossinessSource
    {
        BaseAlpha,
        SpecularAlpha
    }

    public enum ReflectionSource
    {
        NoReflection,
        Cubemap,
        ReflectionProbe
    }

    public struct UpgradeParams
    {
        public UpgradeSurfaceType surfaceType;
        public UpgradeBlendMode blendMode;
        public bool alphaClip;
        public SpecularSource specularSource;
        public GlossinessSource glosinessSource;
    }
}
