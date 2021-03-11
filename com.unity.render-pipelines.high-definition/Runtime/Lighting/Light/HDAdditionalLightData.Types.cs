using System;
using System.Linq;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Type of an HDRP Light.</summary>
    public enum HDLightType
    {
        /// <summary>Spot Light. Complete this type by setting the SpotLightShape too.</summary>
        Spot = LightType.Spot,
        /// <summary>Directional Light.</summary>
        Directional = LightType.Directional,
        /// <summary>Point Light.</summary>
        Point = LightType.Point,
        /// <summary>Area Light. Complete this type by setting the AreaLightShape too.</summary>
        Area = LightType.Area,
    }

    /// <summary>Shape of a spot light.</summary>
    public enum SpotLightShape
    {
        /// <summary>Cone shape. The default shape of the spot light.</summary>
        Cone,
        /// <summary>Pyramid shape.</summary>
        Pyramid,
        /// <summary>Box shape. Similar to a directional light but with bounds.</summary>
        Box
    };

    /// <summary>Shape of an area light</summary>
    public enum AreaLightShape
    {
        /// <summary>Rectangle shape.</summary>
        Rectangle,
        /// <summary>Tube shape. Runtime only</summary>
        Tube,
        /// <summary>Disc shape. Baking only.</summary>
        Disc,
        // Sphere,
    };

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

    /// <summary>Light Layers.</summary>
    public enum LightLayerEnum
    {
        /// <summary>The light will no affect any object.</summary>
        Nothing = 0,   // Custom name for "Nothing" option
        /// <summary>Light Layer 0.</summary>
        LightLayerDefault = 1 << 0,
        /// <summary>Light Layer 1.</summary>
        LightLayer1 = 1 << 1,
        /// <summary>Light Layer 2.</summary>
        LightLayer2 = 1 << 2,
        /// <summary>Light Layer 3.</summary>
        LightLayer3 = 1 << 3,
        /// <summary>Light Layer 4.</summary>
        LightLayer4 = 1 << 4,
        /// <summary>Light Layer 5.</summary>
        LightLayer5 = 1 << 5,
        /// <summary>Light Layer 6.</summary>
        LightLayer6 = 1 << 6,
        /// <summary>Light Layer 7.</summary>
        LightLayer7 = 1 << 7,
        /// <summary>Everything.</summary>
        Everything = 0xFF, // Custom name for "Everything" option
    }

    // Note: do not use internally, this enum only exists for the user API to set the light type and shape at once
    /// <summary>
    /// Type of an HDRP Light including shape
    /// </summary>
    public enum HDLightTypeAndShape
    {
        /// <summary>Point Light.</summary>
        Point,
        /// <summary>Spot Light with box shape.</summary>
        BoxSpot,
        /// <summary>Spot Light with pyramid shape.</summary>
        PyramidSpot,
        /// <summary>Spot Light with cone shape.</summary>
        ConeSpot,
        /// <summary>Directional Light.</summary>
        Directional,
        /// <summary>Rectangle Light.</summary>
        RectangleArea,
        /// <summary>Tube Light, runtime Only.</summary>
        TubeArea,
        /// <summary>Disc light, baking Only</summary>
        DiscArea,
    }

    /// <summary>
    /// Extension class for the HDLightTypeAndShape type.
    /// </summary>
    public static class HDLightTypeExtension
    {
        /// <summary>
        /// Returns true if the hd light type is a spot light
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsSpot(this HDLightTypeAndShape type)
            => type == HDLightTypeAndShape.BoxSpot
            || type == HDLightTypeAndShape.PyramidSpot
            || type == HDLightTypeAndShape.ConeSpot;

        /// <summary>
        /// Returns true if the hd light type is an area light
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsArea(this HDLightTypeAndShape type)
            => type == HDLightTypeAndShape.TubeArea
            || type == HDLightTypeAndShape.RectangleArea
            || type == HDLightTypeAndShape.DiscArea;

        /// <summary>
        /// Returns true if the hd light type can be used for runtime lighting
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool SupportsRuntimeOnly(this HDLightTypeAndShape type)
            => type != HDLightTypeAndShape.DiscArea;

        /// <summary>
        /// Returns true if the hd light type can be used for baking
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool SupportsBakedOnly(this HDLightTypeAndShape type)
            => type != HDLightTypeAndShape.TubeArea;

        /// <summary>
        /// Returns true if the hd light type can be used in mixed mode
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool SupportsMixed(this HDLightTypeAndShape type)
            => type != HDLightTypeAndShape.TubeArea
            && type != HDLightTypeAndShape.DiscArea;
    }

    public partial class HDAdditionalLightData
    {
        //Private enum to differentiate built-in LightType.Point that can be Area or Point in HDRP
        //This is due to realtime support and culling behavior in Unity
        private enum PointLightHDType
        {
            Punctual,
            Area
        }

        [System.NonSerialized]
        static Dictionary<int, LightUnit[]> supportedLightTypeCache = new Dictionary<int, LightUnit[]>();

        [SerializeField, FormerlySerializedAs("lightTypeExtent"), FormerlySerializedAs("m_LightTypeExtent")]
        PointLightHDType m_PointlightHDType = PointLightHDType.Punctual;

        // Only for Spotlight, should be hide for other light
        [SerializeField, FormerlySerializedAs("spotLightShape")]
        SpotLightShape m_SpotLightShape = SpotLightShape.Cone;

        // Only for Spotlight, should be hide for other light
        [SerializeField]
        AreaLightShape m_AreaLightShape = AreaLightShape.Rectangle;

        //Not to be used in render loop instead as we can add on the fly an
        //HDAdditionalLightData that is not really added to the GameObject
        //In this case, use ComputeLightType directly.
        //This is for scripting and case where the HDAdditionnalLightData is existing
        /// <summary>
        /// The type of light used.
        /// This handle some internal conversion in Light component for culling purpose.
        /// </summary>
        public HDLightType type
        {
            get => ComputeLightType(legacyLight);
            set
            {
                if (type != value)
                {
                    if (m_ShadowUpdateMode != ShadowUpdateMode.EveryFrame)
                    {
                        HDShadowManager.cachedShadowManager.EvictLight(this);
                    }

                    switch (value)
                    {
                        case HDLightType.Directional:
                            legacyLight.type = LightType.Directional;
                            m_PointlightHDType = PointLightHDType.Punctual;
                            break;
                        case HDLightType.Spot:
                            legacyLight.type = LightType.Spot;
                            m_PointlightHDType = PointLightHDType.Punctual;
                            break;
                        case HDLightType.Point:
                            legacyLight.type = LightType.Point;
                            m_PointlightHDType = PointLightHDType.Punctual;
                            break;
                        case HDLightType.Area:
                            ResolveAreaShape();
                            break;
                        default:
                            Debug.Assert(false, $"Unknown {typeof(HDLightType).Name} {value}.");
                            break;
                    }

                    if (legacyLight.shadows != LightShadows.None && m_ShadowUpdateMode != ShadowUpdateMode.EveryFrame)
                    {
                        HDShadowManager.cachedShadowManager.RegisterLight(this);
                    }

                    // If the current light unit is not supported by the new light type, we change it
                    var supportedUnits = GetSupportedLightUnits(value, m_SpotLightShape);
                    if (!supportedUnits.Any(u => u == lightUnit))
                        lightUnit = supportedUnits.First();
                    UpdateAllLightValues();
                }
            }
        }

        /// <summary>
        /// Control the shape of the spot light.
        /// </summary>
        public SpotLightShape spotLightShape
        {
            get => m_SpotLightShape;
            set
            {
                if (m_SpotLightShape == value)
                    return;

                m_SpotLightShape = value;

                // If the current light unit is not supported by this spot light shape, we change it
                var supportedUnits = GetSupportedLightUnits(type, value);
                if (!supportedUnits.Any(u => u == lightUnit))
                    lightUnit = supportedUnits.First();
                UpdateAllLightValues();
            }
        }

        /// <summary>
        /// Control the shape of the spot light.
        /// </summary>
        public AreaLightShape areaLightShape
        {
            get => m_AreaLightShape;
            set
            {
                if (m_AreaLightShape == value)
                    return;

                m_AreaLightShape = value;
                if (type == HDLightType.Area)
                    ResolveAreaShape();
                UpdateAllLightValues();
            }
        }

        void ResolveAreaShape()
        {
            m_PointlightHDType = PointLightHDType.Area;
            if (areaLightShape == AreaLightShape.Disc)
            {
                legacyLight.type = LightType.Disc;
#if UNITY_EDITOR
                legacyLight.lightmapBakeType = LightmapBakeType.Baked;
#endif
            }
            else
            {
                legacyLight.type = LightType.Point;
            }
        }

        /// <summary>
        /// Set the type of the light and its shape.
        /// Note: this will also change the unit of the light if the current one is not supported by the new light type.
        /// </summary>
        /// <param name="typeAndShape"></param>
        public void SetLightTypeAndShape(HDLightTypeAndShape typeAndShape)
        {
            switch (typeAndShape)
            {
                case HDLightTypeAndShape.Point:
                    type = HDLightType.Point;
                    break;
                case HDLightTypeAndShape.Directional:
                    type = HDLightType.Directional;
                    break;
                case HDLightTypeAndShape.ConeSpot:
                    type = HDLightType.Spot;
                    spotLightShape = SpotLightShape.Cone;
                    break;
                case HDLightTypeAndShape.PyramidSpot:
                    type = HDLightType.Spot;
                    spotLightShape = SpotLightShape.Pyramid;
                    break;
                case HDLightTypeAndShape.BoxSpot:
                    type = HDLightType.Spot;
                    spotLightShape = SpotLightShape.Box;
                    break;
                case HDLightTypeAndShape.RectangleArea:
                    type = HDLightType.Area;
                    areaLightShape = AreaLightShape.Rectangle;
                    break;
                case HDLightTypeAndShape.TubeArea:
                    type = HDLightType.Area;
                    areaLightShape = AreaLightShape.Tube;
                    break;
                case HDLightTypeAndShape.DiscArea:
                    type = HDLightType.Area;
                    areaLightShape = AreaLightShape.Disc;
                    break;
            }
        }

        /// <summary>
        /// Get the HD condensed light type and its shape.
        /// </summary>
        /// <returns></returns>
        public HDLightTypeAndShape GetLightTypeAndShape()
        {
            switch (type)
            {
                case HDLightType.Directional:   return HDLightTypeAndShape.Directional;
                case HDLightType.Point:         return HDLightTypeAndShape.Point;
                case HDLightType.Spot:
                    switch (spotLightShape)
                    {
                        case SpotLightShape.Cone: return HDLightTypeAndShape.ConeSpot;
                        case SpotLightShape.Box: return HDLightTypeAndShape.BoxSpot;
                        case SpotLightShape.Pyramid: return HDLightTypeAndShape.PyramidSpot;
                        default: throw new Exception($"Unknown {typeof(SpotLightShape)}: {spotLightShape}");
                    }
                case HDLightType.Area:
                    switch (areaLightShape)
                    {
                        case AreaLightShape.Rectangle: return HDLightTypeAndShape.RectangleArea;
                        case AreaLightShape.Tube: return HDLightTypeAndShape.TubeArea;
                        case AreaLightShape.Disc: return HDLightTypeAndShape.DiscArea;
                        default: throw new Exception($"Unknown {typeof(AreaLightShape)}: {areaLightShape}");
                    }
                default: throw new Exception($"Unknown {typeof(HDLightType)}: {type}");
            }
        }

        string GetLightTypeName()
        {
            if (type == HDLightType.Area)
                return $"{areaLightShape}AreaLight";
            else
            {
                if (legacyLight.type == LightType.Spot)
                    return $"{spotLightShape}SpotLight";
                else
                    return $"{legacyLight.type}Light";
            }
        }

        /// <summary>
        /// Give the supported lights unit for the given parameters
        /// </summary>
        /// <param name="type">The type of the light</param>
        /// <param name="spotLightShape">the shape of the spot.You can put anything in case it is not a spot light.</param>
        /// <returns>Array of supported units</returns>
        public static LightUnit[] GetSupportedLightUnits(HDLightType type, SpotLightShape spotLightShape)
        {
            LightUnit[] supportedTypes;

            // Combine the two light types to access the dictionary
            int cacheKey = ((int)type & 0xFF) << 0;
            cacheKey |= ((int)spotLightShape & 0xFF) << 8;

            // We cache the result once they are computed, it avoid garbage generated by Enum.GetValues and Linq.
            if (supportedLightTypeCache.TryGetValue(cacheKey, out supportedTypes))
                return supportedTypes;

            if (type == HDLightType.Area)
                supportedTypes = Enum.GetValues(typeof(AreaLightUnit)).Cast<LightUnit>().ToArray();
            else if (type == HDLightType.Directional || (type == HDLightType.Spot && spotLightShape == SpotLightShape.Box))
                supportedTypes = Enum.GetValues(typeof(DirectionalLightUnit)).Cast<LightUnit>().ToArray();
            else
                supportedTypes = Enum.GetValues(typeof(PunctualLightUnit)).Cast<LightUnit>().ToArray();

            supportedLightTypeCache[cacheKey] = supportedTypes;

            return supportedTypes;
        }

        /// <summary>
        /// Check if the given type is supported by this type and shape.
        /// </summary>
        /// <param name="type">The type of the light</param>
        /// <param name="spotLightShape">the shape of the spot.You can put anything in case it is not a spot light.</param>
        /// <param name="unit">The unit to check</param>
        /// <returns>True: this unit is supported</returns>
        public static bool IsValidLightUnitForType(HDLightType type, SpotLightShape spotLightShape, LightUnit unit)
        {
            LightUnit[] allowedUnits = GetSupportedLightUnits(type, spotLightShape);

            return allowedUnits.Any(u => u == unit);
        }

        //To use in render loop instead of type as we can add on the fly an
        //HDAdditionalLightData that is not really added to the GameObject
        //In this case, the type property will return a false value as this will
        //be base on a default(HDAdditionnalData) which will have a point type
        internal HDLightType ComputeLightType(Light attachedLight)
        {
            // Shuriken lights won't have a Light component.
            if (attachedLight == null)
                return HDLightType.Point;

            switch (attachedLight.type)
            {
                case LightType.Spot: return HDLightType.Spot;
                case LightType.Directional: return HDLightType.Directional;
                case LightType.Point:
                    switch (m_PointlightHDType)
                    {
                        case PointLightHDType.Punctual: return HDLightType.Point;
                        case PointLightHDType.Area: return HDLightType.Area;
                        default:
                            //Debug.Assert(false, $"Unknown {typeof(PointLightHDType).Name} {m_PointlightHDType}. Fallback on Punctual");
                            return HDLightType.Point;
                    }
                case LightType.Disc:
                    return HDLightType.Area;
                case LightType.Rectangle:
                    // not supported directly. Convert now to equivalent if not default HDAdditionalLightData:
                    if (this != HDUtils.s_DefaultHDAdditionalLightData)
                    {
                        legacyLight.type = LightType.Point;
                        m_PointlightHDType = PointLightHDType.Area;
                        m_AreaLightShape = AreaLightShape.Rectangle;

                        //sanitycheck on the baking mode first time we add additionalLightData
#if UNITY_EDITOR
                        legacyLight.lightmapBakeType = LightmapBakeType.Realtime;
#endif
                    }
                    return HDLightType.Area;
                default:
                    Debug.Assert(false, $"Unknown {typeof(LightType).Name} {attachedLight.type}. Fallback on Point");
                    return HDLightType.Point;
            }
        }
    }
}
