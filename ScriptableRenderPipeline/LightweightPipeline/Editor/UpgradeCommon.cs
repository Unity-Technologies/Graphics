namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    public enum UpgradeBlendMode
    {
        Opaque,
        Cutout,
        Alpha
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
        public UpgradeBlendMode blendMode;
        public SpecularSource specularSource;
        public GlossinessSource glosinessSource;
    }
}
