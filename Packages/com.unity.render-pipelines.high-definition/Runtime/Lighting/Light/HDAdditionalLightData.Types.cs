using System;
using System.Linq;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Unit of the lights supported in HDRP
    /// </summary>
    public enum LightUnit
    {
        /// <summary>Total power/flux emitted by the light.</summary>
        Lumen,
        /// <summary>Flux per steradian.</summary>
        Candela,    // lm/sr
        /// <summary>Flux per unit area.</summary>
        Lux,        // lm/m²
        /// <summary>Flux per unit area and per steradian.</summary>
        Nits,       // lm/m²/sr
        /// <summary>ISO 100 Exposure Value (https://en.wikipedia.org/wiki/Exposure_value).</summary>
        Ev100,
    }

    internal enum DirectionalLightUnit
    {
        Lux = LightUnit.Lux,
    }

    internal enum AreaLightUnit
    {
        Lumen = LightUnit.Lumen,
        Nits = LightUnit.Nits,
        Ev100 = LightUnit.Ev100,
    }

    internal enum PunctualLightUnit
    {
        Lumen = LightUnit.Lumen,
        Candela = LightUnit.Candela,
        Lux = LightUnit.Lux,
        Ev100 = LightUnit.Ev100
    }

    /// <summary>Shadow Update mode </summary>
    public enum ShadowUpdateMode
    {
        /// <summary>Shadow map will be rendered at every frame.</summary>
        EveryFrame = 0,
        /// <summary>Shadow will be rendered only when the OnEnable of the light is called.</summary>
        OnEnable,
        /// <summary>Shadow will be rendered when you call HDAdditionalLightData.RequestShadowMapRendering().</summary>
        OnDemand
    }

    /// <summary>
    /// Rendering Layer Mask.
    /// </summary>
    [Flags]
    public enum RenderingLayerMask
    {
        /// <summary>No rendering layer.</summary>
        Nothing = 0,
        /// <summary>Rendering layer 1.</summary>
        RenderingLayer1 = 1 << 0,
        /// <summary>Rendering layer 2.</summary>
        RenderingLayer2 = 1 << 1,
        /// <summary>Rendering layer 3.</summary>
        RenderingLayer3 = 1 << 2,
        /// <summary>Rendering layer 4.</summary>
        RenderingLayer4 = 1 << 3,
        /// <summary>Rendering layer 5.</summary>
        RenderingLayer5 = 1 << 4,
        /// <summary>Rendering layer 6.</summary>
        RenderingLayer6 = 1 << 5,
        /// <summary>Rendering layer 7.</summary>
        RenderingLayer7 = 1 << 6,
        /// <summary>Rendering layer 8.</summary>
        RenderingLayer8 = 1 << 7,
        /// <summary>Rendering layer 9.</summary>
        RenderingLayer9 = 1 << 8,
        /// <summary>Rendering layer 10.</summary>
        RenderingLayer10 = 1 << 9,
        /// <summary>Rendering layer 11.</summary>
        RenderingLayer11 = 1 << 10,
        /// <summary>Rendering layer 12.</summary>
        RenderingLayer12 = 1 << 11,
        /// <summary>Rendering layer 13.</summary>
        RenderingLayer13 = 1 << 12,
        /// <summary>Rendering layer 14.</summary>
        RenderingLayer14 = 1 << 13,
        /// <summary>Rendering layer 15.</summary>
        RenderingLayer15 = 1 << 14,
        /// <summary>Rendering layer 16.</summary>
        RenderingLayer16 = 1 << 15,

        /// <summary>Default Layer for lights.</summary>
        [HideInInspector]
        LightLayerDefault = RenderingLayer1,
        /// <summary>Default Layer for decals.</summary>
        [HideInInspector]
        DecalLayerDefault = RenderingLayer9,
        /// <summary>Default rendering layers mask.</summary>
        [HideInInspector]
        Default = LightLayerDefault | DecalLayerDefault,
        /// <summary>All layers enabled.</summary>
        [HideInInspector]
        Everything = 0xFFFF,


        /// <summary>Light Layer 1.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer2")]
        LightLayer1 = RenderingLayer2,
        /// <summary>Light Layer 2.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer3")]
        LightLayer2 = RenderingLayer3,
        /// <summary>Light Layer 3.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer4")]
        LightLayer3 = RenderingLayer4,
        /// <summary>Light Layer 4.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer5")]
        LightLayer4 = RenderingLayer5,
        /// <summary>Light Layer 5.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer6")]
        LightLayer5 = RenderingLayer6,
        /// <summary>Light Layer 6.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer7")]
        LightLayer6 = RenderingLayer7,
        /// <summary>Light Layer 7.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer8")]
        LightLayer7 = RenderingLayer8,

        /// <summary>Decal Layer 1.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer10")]
        DecalLayer1 = RenderingLayer10,
        /// <summary>Decal Layer 2.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer11")]
        DecalLayer2 = RenderingLayer11,
        /// <summary>Decal Layer 3.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer12")]
        DecalLayer3 = RenderingLayer12,
        /// <summary>Decal Layer 4.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer13")]
        DecalLayer4 = RenderingLayer13,
        /// <summary>Decal Layer 5.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer14")]
        DecalLayer5 = RenderingLayer14,
        /// <summary>Decal Layer 6.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer15")]
        DecalLayer6 = RenderingLayer15,
        /// <summary>Decal Layer 7.</summary>
        [HideInInspector, Obsolete("@from(2023.1) Use RenderingLayer16")]
        DecalLayer7 = RenderingLayer16,
    }

    /// <summary>
    /// Extension class for the HDLightTypeAndShape type.
    /// </summary>
    public static class HDLightTypeExtension
    {
        /// <summary>
        /// Returns true if the light type is a spot light
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsSpot(this LightType type)
            => type == LightType.Box
            || type == LightType.Pyramid
            || type == LightType.Spot;

        /// <summary>
        /// Returns true if the light type is an area light
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsArea(this LightType type)
            => type == LightType.Tube
            || type == LightType.Rectangle
            || type == LightType.Disc;

        /// <summary>
        /// Returns true if the light type can be used for runtime lighting
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool SupportsRuntimeOnly(this LightType type)
            => type != LightType.Disc;

        /// <summary>
        /// Returns true if the light type can be used for baking
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool SupportsBakedOnly(this LightType type)
            => type != LightType.Tube;

        /// <summary>
        /// Returns true if the light type can be used in mixed mode
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool SupportsMixed(this LightType type)
            => type != LightType.Tube
            && type != LightType.Disc;
    }

    public partial class HDAdditionalLightData
    {
        [System.NonSerialized]
        private static readonly LightUnit[] sAreaLightUnits = Enum.GetValues(typeof(AreaLightUnit)).Cast<LightUnit>().ToArray();
        [System.NonSerialized]
        private static readonly LightUnit[] sPunctualLightUnits = Enum.GetValues(typeof(PunctualLightUnit)).Cast<LightUnit>().ToArray();
        [System.NonSerialized]
        private static readonly LightUnit[] sDirectionalLightUnits = Enum.GetValues(typeof(DirectionalLightUnit)).Cast<LightUnit>().ToArray();
        [System.NonSerialized]
        private static readonly UInt64 sAreaLightUnitBitMask = ComputeLightUnitBitMask(sAreaLightUnits);
        [System.NonSerialized]
        private static readonly UInt64 sDirectionalLightUnitBitMask = ComputeLightUnitBitMask(sDirectionalLightUnits);
        [System.NonSerialized]
        private static readonly UInt64 sPunctualLightUnitBitMask = ComputeLightUnitBitMask(sPunctualLightUnits);

        private static UInt64 ComputeLightUnitBitMask(LightUnit[] unitArray)
        {
            UInt64 mask = 0;

            foreach (LightUnit u in unitArray)
            {
                mask |= 1UL << (int)u;
            }

            return mask;
        }

        string GetLightTypeName()
        {
            switch (legacyLight.type)
            {
                case LightType.Spot: return "ConeSpotLight";
                case LightType.Pyramid: return "PyramidSpotLight";
                case LightType.Box: return "BoxSpotLight";
                case LightType.Directional: return "DirectionalLight";
                case LightType.Point: return "PointLight";
                case LightType.Rectangle: return "RectangleAreaLight";
                case LightType.Disc: return "DiscAreaLight";
                case LightType.Tube: return "TubeAreaLight";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Give the supported lights unit for the given parameters
        /// </summary>
        /// <param name="type">The type of the light</param>
        /// <returns>Array of supported units</returns>
        public static LightUnit[] GetSupportedLightUnits(LightType type)
        {
            if (type == LightType.Rectangle || type == LightType.Disc || type == LightType.Tube)
                return sAreaLightUnits;
            else if (type == LightType.Directional || type == LightType.Box)
                return sDirectionalLightUnits;
            else
                return sPunctualLightUnits;
        }

        internal static UInt64 GetSupportedLightUnitsBitMask(LightType type)
        {
            if (type == LightType.Rectangle || type == LightType.Disc || type == LightType.Tube)
                return sAreaLightUnitBitMask;
            else if (type == LightType.Directional || type == LightType.Box)
                return sDirectionalLightUnitBitMask;
            else
                return sPunctualLightUnitBitMask;
        }

        /// <summary>
        /// Check if the given type is supported by this type and shape.
        /// </summary>
        /// <param name="type">The type of the light</param>
        /// <param name="unit">The unit to check</param>
        /// <returns>True: this unit is supported</returns>
        public static bool IsValidLightUnitForType(LightType type, LightUnit unit)
        {
            LightUnit[] allowedUnits = GetSupportedLightUnits(type);

            return allowedUnits.Any(u => u == unit);
        }

    }

}
