using System;
using System.Linq;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Deprecated DensityVolume</summary>
    [Obsolete("DensityVolume has been deprecated, use LocalVolumetricFog", true)]
    public class DensityVolume : LocalVolumetricFog
    {
    }

    /// <summary>Deprecated DensityVolumeArtistParameters</summary>
    [Obsolete("DensityVolumeArtistParameters has been deprecated, use LocalVolumetricFogArtistParameters", true)]
    public struct DensityVolumeArtistParameters
    {
    }

    public partial struct LocalVolumetricFogArtistParameters
    {
        /// <summary>Obsolete, do not use.</summary>
        [Obsolete("Never worked correctly due to having engine working in percent. Will be removed soon.")]
        public bool advancedFade => true;
    }

    /// <summary>
    /// Volume debug settings.
    /// </summary>
    [Obsolete("VolumeDebugSettings has been deprecated. Use HDVolumeDebugSettings instead (UnityUpgradable) -> HDVolumeDebugSettings")]
    public class VolumeDebugSettings : HDVolumeDebugSettings
    {
    }

    /// <summary>
    /// Class managing debug display in HDRP.
    /// </summary>
    public partial class DebugDisplaySettings
    {
        /// <summary>
        /// Debug data.
        /// </summary>
        public partial class DebugData
        {
            /// <summary>Current volume debug settings.</summary>
            [Obsolete("Moved to HDDebugDisplaySettings.Instance. Will be removed soon.")]
            public IVolumeDebugSettings volumeDebugSettings = new HDVolumeDebugSettings();
        }

        /// <summary>List of Full Screen Lighting RTAS Debug view names.</summary>
        [Obsolete("Use autoenum instead @from(2022.2)")]
        public static GUIContent[] lightingFullScreenRTASDebugViewStrings => Enum.GetNames(typeof(RTASDebugView)).Select(t => new GUIContent(t)).ToArray();

        /// <summary>List of Full Screen Lighting RTAS Debug view values.</summary>
        [Obsolete("Use autoenum instead @from(2022.2)")]
        public static int[] lightingFullScreenRTASDebugViewValues => (int[])Enum.GetValues(typeof(RTASDebugView));

        /// <summary>List of Full Screen Lighting RTAS Debug mode names.</summary>
        [Obsolete("Use autoenum instead @from(2022.2)")]
        public static GUIContent[] lightingFullScreenRTASDebugModeStrings => Enum.GetNames(typeof(RTASDebugMode)).Select(t => new GUIContent(t)).ToArray();

        /// <summary>List of Full Screen Lighting RTAS Debug mode values.</summary>
        [Obsolete("Use autoenum instead @from(2022.2)")]
        public static int[] lightingFullScreenRTASDebugModeValues => (int[])Enum.GetValues(typeof(RTASDebugMode));
    }

    /// <summary>
    /// AmbientOcclusion has been renamed. Use ScreenSpaceAmbientOcclusion instead
    /// </summary>
    [Obsolete("AmbientOcclusion has been renamed. Use ScreenSpaceAmbientOcclusion instead @from(2022.2) (UnityUpgradable) -> ScreenSpaceAmbientOcclusion")]
    public sealed class AmbientOcclusion
    {
    }

    /// <summary>Deprecated DiffusionProfileOverride</summary>
    [Obsolete("DiffusionProfileOverride has been deprecated @from(2022.2) (UnityUpgradable) -> DiffusionProfileList", false)]
    public sealed class DiffusionProfileOverride
    {
    }

    /// <summary>Light Layers.</summary>
    [Flags, Obsolete("LightLayersEnum has been renamed and can now use 16 bits. Use RenderingLayerMask instead @from(2023.1) (UnityUpgradable) -> RenderingLayerMask")]
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

    /// <summary>Decal Layers.</summary>
    [Flags, Obsolete("DecalLayerEnum has been renamed and can now use 16 bits. Use RenderingLayerMask instead @from(2023.1) (UnityUpgradable) -> RenderingLayerMask")]
    public enum DecalLayerEnum
    {
        /// <summary>The light will no affect any object.</summary>
        Nothing = 0,   // Custom name for "Nothing" option
        /// <summary>Decal Layer 0.</summary>
        DecalLayerDefault = 1 << 0,
        /// <summary>Decal Layer 1.</summary>
        DecalLayer1 = 1 << 1,
        /// <summary>Decal Layer 2.</summary>
        DecalLayer2 = 1 << 2,
        /// <summary>Decal Layer 3.</summary>
        DecalLayer3 = 1 << 3,
        /// <summary>Decal Layer 4.</summary>
        DecalLayer4 = 1 << 4,
        /// <summary>Decal Layer 5.</summary>
        DecalLayer5 = 1 << 5,
        /// <summary>Decal Layer 6.</summary>
        DecalLayer6 = 1 << 6,
        /// <summary>Decal Layer 7.</summary>
        DecalLayer7 = 1 << 7,
        /// <summary>Everything.</summary>
        Everything = 0xFF, // Custom name for "Everything" option
    }

    /// <summary>
    /// Debug Light Layers Filtering.
    /// </summary>
    [Flags, Obsolete("DebugLightLayersMask has been renamed and can now use 16 bits. Use RenderingLayerMask instead @from(2023.1)")]
    public enum DebugLightLayersMask
    {
        /// <summary>No light layer debug.</summary>
        None = 0,
        /// <summary>Debug light layer 1.</summary>
        LightLayer1 = 1 << 0,
        /// <summary>Debug light layer 2.</summary>
        LightLayer2 = 1 << 1,
        /// <summary>Debug light layer 3.</summary>
        LightLayer3 = 1 << 2,
        /// <summary>Debug light layer 4.</summary>
        LightLayer4 = 1 << 3,
        /// <summary>Debug light layer 5.</summary>
        LightLayer5 = 1 << 4,
        /// <summary>Debug light layer 6.</summary>
        LightLayer6 = 1 << 5,
        /// <summary>Debug light layer 7.</summary>
        LightLayer7 = 1 << 6,
        /// <summary>Debug light layer 8.</summary>
        LightLayer8 = 1 << 7,
    }

    /// <summary>This enum has been deprecated, and light types that existed only in HDRP have been moved into
    /// the `UnityEngine.LightType` instead. So you should now directly set the `type` property on the
    /// `UnityEngine.Light` monobehaviour.</summary>
    [Obsolete("This enum has been deprecated. Use the UnityEngine.LightType enum instead.", false)]
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

    /// <summary>This enum has been deprecated, and the spot light shapes now exist as separate members in the
    /// `UnityEngine.LightType` enum. The `Cone`, `Pyramid`, and `Box` types are represented by `LightType.Spot`,
    /// `LightType.Pyramid`, and `LightType.Box` respectively.</summary>
    [Obsolete("This enum has been deprecated. Use the UnityEngine.LightType enum instead.", false)]
    public enum SpotLightShape
    {
        /// <summary>Cone shape. The default shape of the spot light.</summary>
        Cone,
        /// <summary>Pyramid shape.</summary>
        Pyramid,
        /// <summary>Box shape. Similar to a directional light but with bounds.</summary>
        Box
    };

    /// <summary>This enum has been deprecated, and the area light shapes now exist as separate members in the
    /// `UnityEngine.LightType` enum. The `Rectangle`, `Tube`, and `Disc` types are represented by `LightType.Rectangle`,
    /// `LightType.Tube`, and `LightType.Disc` respectively.</summary>
    [Obsolete("This enum has been deprecated. Use the UnityEngine.LightType enum instead.", false)]
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

    /// <summary>This enum has been deprecated, and the light type and shape combos now exist as separate members in the
    /// `UnityEngine.LightType` enum.</summary>
    [Obsolete("This enum has been deprecated. Use the UnityEngine.LightType enum instead.", false)]
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

    public partial class HDAdditionalLightData
    {
        //Internal enum to differentiate built-in LightType.Point that can be Area or Point in HDRP
        //This is due to realtime support and culling behavior in Unity
        [Obsolete("This property has been deprecated. Use Light.type instead.", false)]
        internal enum PointLightHDType
        {
            Punctual,
            Area
        }

        /// <summary>This property has been deprecated, and light types that existed only in HDRP have been moved into
        /// the `UnityEngine.LightType` instead. So `hdAdditionalLightData.type = HDLightType.Point` should now be
        /// written as `light.type = LightType.Point`</summary>
        [Obsolete("This property has been deprecated. Use the UnityEngine.Light.type property instead.", false)]
        public HDLightType type
        {
            get
            {
                switch (legacyLight.type)
                {
                    case LightType.Spot:
                    case LightType.Pyramid:
                    case LightType.Box:
                        return HDLightType.Spot;
                    case LightType.Directional:
                        return HDLightType.Directional;
                    case LightType.Point:
                        return HDLightType.Point;
                    case LightType.Rectangle:
                    case LightType.Disc:
                    case LightType.Tube:
                        return HDLightType.Area;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                legacyLight.type = value switch
                {
                    HDLightType.Spot => LightType.Spot,
                    HDLightType.Directional => LightType.Directional,
                    HDLightType.Point => LightType.Point,
                    HDLightType.Area => LightType.Rectangle,
                    _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
                };
            }
        }

        /// <summary>This property has been deprecated, and the spot light shapes now exist as separate members in the
        /// `UnityEngine.LightType` enum. So `hdAdditionalLightData.spotLightShape = SpotLightShape.Pyramid` should now
        /// be written as `light.type = LightType.Pyramid`.</summary>
        [Obsolete("This property has been deprecated. Use the UnityEngine.Light.type property instead.", false)]
        public SpotLightShape spotLightShape
        {
            get
            {
                switch (legacyLight.type)
                {
                    case LightType.Pyramid:
                        return SpotLightShape.Pyramid;
                    case LightType.Box:
                        return SpotLightShape.Box;
                    case LightType.Spot:
                    case LightType.Directional:
                    case LightType.Point:
                    case LightType.Rectangle:
                    case LightType.Disc:
                    case LightType.Tube:
                        return SpotLightShape.Cone;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>This property has been deprecated, and the area light shapes now exist as separate members in the
        /// `UnityEngine.LightType` enum. So `hdAdditionalLightData.areaLightShape = AreaLightShape.Rectangle` should
        /// now be written as `light.type = LightType.Rectangle`.</summary>
        [Obsolete("This property has been deprecated. Use the UnityEngine.Light.type property instead.", false)]
        public AreaLightShape areaLightShape
        {
            get
            {
                switch (legacyLight.type)
                {
                    case LightType.Tube:
                        return AreaLightShape.Tube;
                    case LightType.Disc:
                        return AreaLightShape.Disc;
                    case LightType.Rectangle:
                    case LightType.Pyramid:
                    case LightType.Box:
                    case LightType.Spot:
                    case LightType.Directional:
                    case LightType.Point:
                        return AreaLightShape.Rectangle;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [Obsolete("This property has been deprecated. Use Light.type instead.")]
        [SerializeField, FormerlySerializedAs("lightTypeExtent"), FormerlySerializedAs("m_LightTypeExtent")]
        PointLightHDType m_PointlightHDType = PointLightHDType.Punctual;

        // Only for Spotlight, should be hide for other light
        [Obsolete("This property has been deprecated. Use Light.type instead.")]
        [SerializeField, FormerlySerializedAs("spotLightShape")]
        SpotLightShape m_SpotLightShape = SpotLightShape.Cone;

        // Only for Spotlight, should be hide for other light
        [Obsolete("This property has been deprecated. Use Light.type instead.")]
        [SerializeField]
        AreaLightShape m_AreaLightShape = AreaLightShape.Rectangle;

        /// <summary>This method has been deprecated. Setting the type of the light is now much simpler, and done
        /// completely in the `UnityEngine.Light` monobehavior. So
        /// `hdAdditionalLightData.SetLightTypeAndShape(HDLightTypeAndShape.PyramidSpot)` should now be written as
        /// `light.type = LightType.Pyramid`.</summary>
        /// <param name="typeAndShape"></param>
        [Obsolete("This method has been deprecated. Set the UnityEngine.Light.type property instead.", false)]
        public void SetLightTypeAndShape(HDLightTypeAndShape typeAndShape)
        {
            legacyLight.type = typeAndShape switch
            {
                HDLightTypeAndShape.Point => LightType.Point,
                HDLightTypeAndShape.BoxSpot => LightType.Box,
                HDLightTypeAndShape.PyramidSpot => LightType.Pyramid,
                HDLightTypeAndShape.ConeSpot => LightType.Spot,
                HDLightTypeAndShape.Directional => LightType.Directional,
                HDLightTypeAndShape.RectangleArea => LightType.Rectangle,
                HDLightTypeAndShape.TubeArea => LightType.Tube,
                HDLightTypeAndShape.DiscArea => LightType.Disc,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>This method has been deprecated. Getting the type of the light is now much simpler, and done
        /// completely via the `UnityEngine.Light` monobehavior. So
        /// `var type = hdAdditionalLightData.GetLightTypeAndShape()` should now be written as
        /// `var  type = light.type`.</summary>
        /// <returns></returns>
        [Obsolete("This method has been deprecated. Use Light.type instead.", false)]
        public HDLightTypeAndShape GetLightTypeAndShape()
        {
            return legacyLight.type switch
            {
                LightType.Point => HDLightTypeAndShape.Point,
                LightType.Box => HDLightTypeAndShape.BoxSpot,
                LightType.Pyramid => HDLightTypeAndShape.PyramidSpot,
                LightType.Spot => HDLightTypeAndShape.ConeSpot,
                LightType.Directional => HDLightTypeAndShape.Directional,
                LightType.Rectangle => HDLightTypeAndShape.RectangleArea,
                LightType.Tube => HDLightTypeAndShape.TubeArea,
                LightType.Disc => HDLightTypeAndShape.DiscArea,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// This method has been deprecated.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="spotLightShape"></param>
        /// <returns></returns>
        [Obsolete("This method has been deprecated. Use the GetSupportedLightUnits(LightType) overload instead.", false)]
        public static LightUnit[] GetSupportedLightUnits(HDLightType type, SpotLightShape spotLightShape)
        {
            LightType ltype = type switch
            {
                HDLightType.Spot => spotLightShape switch
                {
                    SpotLightShape.Cone => LightType.Spot,
                    SpotLightShape.Pyramid => LightType.Pyramid,
                    SpotLightShape.Box => LightType.Box,
                    _ => throw new ArgumentOutOfRangeException()
                },
                HDLightType.Directional => LightType.Directional,
                HDLightType.Point => LightType.Point,
                HDLightType.Area => LightType.Rectangle,
                _ => throw new ArgumentOutOfRangeException()
            };

            return GetSupportedLightUnits(ltype);
        }

        /// <summary>
        /// This method has been dperecated.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="spotLightShape"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        [Obsolete("This method has been deprecated. Use the IsValidLightUnitForType(LightType, LightUnit) overload instead.", false)]
        public static bool IsValidLightUnitForType(HDLightType type, SpotLightShape spotLightShape, LightUnit unit)
        {
            LightType ltype = type switch
            {
                HDLightType.Spot => spotLightShape switch
                {
                    SpotLightShape.Cone => LightType.Spot,
                    SpotLightShape.Pyramid => LightType.Pyramid,
                    SpotLightShape.Box => LightType.Box,
                    _ => throw new ArgumentOutOfRangeException()
                },
                HDLightType.Directional => LightType.Directional,
                HDLightType.Point => LightType.Point,
                HDLightType.Area => LightType.Rectangle,
                _ => throw new ArgumentOutOfRangeException()
            };
            return IsValidLightUnitForType(ltype, unit);
        }
    }

    public static partial class GameObjectExtension
    {
        /// <summary>
        ///  Add a new HDRP Light to a GameObject
        /// </summary>
        /// <param name="gameObject">The GameObject on which the light is going to be added</param>
        /// <param name="lightTypeAndShape">The type and shape of the HDRP light to Add</param>
        /// <returns>The created HDRP Light component</returns>
        [Obsolete("This method has been deprecated. Use the AddHDLight(LightType) overload instead.", false)]
        public static HDAdditionalLightData AddHDLight(this GameObject gameObject, HDLightTypeAndShape lightTypeAndShape)
        {
            LightType type;
            switch (lightTypeAndShape)
            {
                case HDLightTypeAndShape.Point:
                    type = LightType.Point;
                    break;
                case HDLightTypeAndShape.BoxSpot:
                    type = LightType.Box;
                    break;
                case HDLightTypeAndShape.PyramidSpot:
                    type = LightType.Pyramid;
                    break;
                case HDLightTypeAndShape.ConeSpot:
                    type = LightType.Spot;
                    break;
                case HDLightTypeAndShape.Directional:
                    type = LightType.Directional;
                    break;
                case HDLightTypeAndShape.RectangleArea:
                    type = LightType.Rectangle;
                    break;
                case HDLightTypeAndShape.TubeArea:
                    type = LightType.Tube;
                    break;
                case HDLightTypeAndShape.DiscArea:
                    type = LightType.Disc;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return AddHDLight(gameObject, type);
        }
    }
}
