using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Rendering.Universal
{
    [MovedFrom("UnityEditor.Rendering.LWRP")] public enum UpgradeSurfaceType
    {
        Opaque,
        Transparent
    }

    [MovedFrom("UnityEditor.Rendering.LWRP")] public enum UpgradeBlendMode
    {
        Alpha,
        Premultiply,
        Additive,
        Multiply
    }

    [MovedFrom("UnityEditor.Rendering.LWRP")] public enum SpecularSource
    {
        SpecularTextureAndColor,
        NoSpecular
    }

    [MovedFrom("UnityEditor.Rendering.LWRP")] public enum SmoothnessSource
    {
        BaseAlpha,
        SpecularAlpha
    }

    [MovedFrom("UnityEditor.Rendering.LWRP")] public enum ReflectionSource
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
