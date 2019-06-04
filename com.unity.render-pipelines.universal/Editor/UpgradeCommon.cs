namespace UnityEditor.Rendering.LWRP
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

    public enum SmoothnessSource
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
        public UpgradeSurfaceType surfaceType { get; set; }
        public UpgradeBlendMode blendMode { get; set; }
        public bool alphaClip { get; set; }
        public SpecularSource specularSource { get; set; }
        public SmoothnessSource smoothnessSource { get; set; }
    }
}
