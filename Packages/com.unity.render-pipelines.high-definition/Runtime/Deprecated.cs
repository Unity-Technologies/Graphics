using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
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
        [Obsolete("This method has been deprecated. Use LightUnitUtils.IsLightUnitSupported(LightType, LightUnit) instead.", false)]
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
            return LightUnitUtils.IsLightUnitSupported(ltype, unit);
        }

        [Obsolete("This property has been deprecated. Use Light.enableSpotReflector instead.", false)]
        [SerializeField, FormerlySerializedAs("enableSpotReflector")]
        bool m_EnableSpotReflector = true;

        /// <summary>This property has been deprecated and moved to Light.</summary>
        [Obsolete("This property has been deprecated. Use Light.enableSpotReflector instead.", false)]
        public bool enableSpotReflector
        {
            get => legacyLight.enableSpotReflector;
            set => legacyLight.enableSpotReflector = value;
        }

        [Obsolete("This property has been deprecated. Use Light.lightUnit instead.", false)]
        [SerializeField, FormerlySerializedAs("lightUnit")]
        LightUnit m_LightUnit = LightUnit.Lumen;

        /// <summary>This property has been deprecated and moved to Light.</summary>
        [Obsolete("This property has been deprecated. Use Light.lightUnit instead.", false)]
        public LightUnit lightUnit
        {
            get => legacyLight.lightUnit;
            set => legacyLight.lightUnit = value;
        }

        /// <summary>This method has been deprecated.</summary>
        /// <param name="unit">Unit of the light</param>
        [Obsolete("This property has been deprecated. Directly set Light.lightUnit instead.", false)]
        public void SetLightUnit(LightUnit unit)
        {
            legacyLight.lightUnit = unit;
        }

        /// <summary>This method has been deprecated. If you need to set a light's intensity measured by some light unit,
        /// you should use the ConvertIntensity(...) method in LightUnitUtils and set Light.intensity directly.</summary>
        /// <param name="intensity">Light intensity</param>
        [Obsolete("This method has been deprecated. Use LightUnitUtils.ConvertIntensity(...) & directly set Light.intensity instead.", false)]
        public void SetIntensity(float intensity)
        {
            legacyLight.intensity = LightUnitUtils.ConvertIntensity(legacyLight, intensity, legacyLight.lightUnit, LightUnitUtils.GetNativeLightUnit(legacyLight.type));
        }

        /// <summary>This method has been deprecated. If you need to set a light's intensity measured by some light unit,
        /// you should use the ConvertIntensity(...) method in LightUnitUtils and set Light.intensity directly.
        /// If you need to change the unit, set Light.lightUnit directly.</summary>
        /// <param name="intensity">Light intensity</param>
        /// <param name="unit">Unit must be a valid Light Unit for the current light type</param>
        [Obsolete("This property has been deprecated. Use LightUnitUtils.ConvertIntensity(...) & directly set Light.lightUnit + Light.intensity instead.", false)]
        public void SetIntensity(float intensity, LightUnit unit)
        {
            legacyLight.intensity = LightUnitUtils.ConvertIntensity(legacyLight, intensity, unit, LightUnitUtils.GetNativeLightUnit(legacyLight.type));
            legacyLight.lightUnit = unit;
        }

        [Obsolete("This property has been deprecated. Use Light.luxAtDistance instead.", false)]
        [SerializeField, FormerlySerializedAs("luxAtDistance")]
        float m_LuxAtDistance = 1.0f;

        /// <summary>This property has been deprecated and moved to Light.</summary>
        [Obsolete("This property has been deprecated. Use Light.luxAtDistance instead.", false)]
        public float luxAtDistance
        {
            get => legacyLight.luxAtDistance;
            set => legacyLight.luxAtDistance = value;
        }

        /// <summary>This method has been deprecated. If you need to set a light's intensity measured by some light unit,
        /// you should use the ConvertIntensity(...) method in LightUnitUtils and set Light.intensity directly.
        /// If you need to change the unit, set Light.lightUnit directly.
        /// If you need to change lux at distance, set light.luxAtDistance directly.</summary>
        /// <param name="luxIntensity">Lux intensity</param>
        /// <param name="distance">Lux at distance</param>
        [Obsolete("This method has been deprecated. Use LightUnitUtils.ConvertIntensity(...) & directly set Light.luxAtDistance + Light.lightUnit + Light.intensity instead.", false)]
        public void SetSpotLightLuxAt(float luxIntensity, float distance)
        {
            legacyLight.luxAtDistance = distance;
            legacyLight.intensity = LightUnitUtils.ConvertIntensity(legacyLight, luxIntensity, LightUnit.Lux, LightUnitUtils.GetNativeLightUnit(legacyLight.type));
            legacyLight.lightUnit = LightUnit.Lux;
        }


        [Obsolete("This property has been deprecated. Use Light.intensity instead.", false)]
        [SerializeField, FormerlySerializedAs("displayLightIntensity")]
        float m_Intensity;

        /// <summary>This property has been deprecated and moved to Light.</summary>
        [Obsolete("This property has been deprecated. Use Light.intensity instead.", false)]
        public float intensity
        {
            get => legacyLight.intensity;
            set => legacyLight.intensity = value;
        }

        /// <summary>This method has been deprecated. Use the equivalent function LightUnitUtils.IsLightUnitSupported(...) instead.</summary>
        /// <param name="type">The type of the light</param>
        /// <param name="unit">The unit to check</param>
        /// <returns>True: this unit is supported</returns>
        [Obsolete("This function has been deprecated. Use LightUnitUtils.IsLightUnitSupported(LightType, LightUnit) instead.", false)]
        public static bool IsValidLightUnitForType(LightType type, LightUnit unit)
        {
            return LightUnitUtils.IsLightUnitSupported(type, unit);
        }

        /// <summary>This method has been deprecated.</summary>
        /// <returns>Array of supported units</returns>
        [Obsolete("This function has been deprecated. Use LightUnitUtils.IsLightUnitSupported(LightType, LightUnit) instead.", false)]
        public LightUnit[] GetSupportedLightUnits()
        {
            return GetSupportedLightUnits(legacyLight.type);
        }

        /// <summary>This method has been deprecated.</summary>
        /// <param name="type">The type of the light</param>
        /// <returns>Array of supported units</returns>
        [Obsolete("This function has been deprecated. Use LightUnitUtils.IsLightUnitSupported(LightType, LightUnit) instead.", false)]
        public static LightUnit[] GetSupportedLightUnits(LightType type)
        {
            return type switch {
                LightType.Directional or LightType.Box => new[] { LightUnit.Lux },
                LightType.Spot or LightType.Pyramid or LightType.Point => new[] { LightUnit.Candela, LightUnit.Lumen, LightUnit.Lux, LightUnit.Ev100 },
                LightType.Rectangle or LightType.Disc or LightType.Tube => new [] { LightUnit.Nits, LightUnit.Lumen, LightUnit.Ev100 },
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
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
  
    partial class HDRenderPipelineGlobalSettings
    {
        #region Custom Post Processes Injections

        [SerializeField, Obsolete("Keep for migration. #from(23.2)")]
        private CustomPostProcessOrdersSettings m_CustomPostProcessOrdersSettings = new();

        // List of custom post process Types that will be executed in the project, in the order of the list (top to back)
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")]
        internal List<string> beforeTransparentCustomPostProcesses = new List<string>();
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")]
        internal List<string> beforePostProcessCustomPostProcesses = new List<string>();
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")]
        internal List<string> afterPostProcessBlursCustomPostProcesses = new List<string>();
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")]
        internal List<string> afterPostProcessCustomPostProcesses = new List<string>();
        [SerializeField, Obsolete("Keep for migration. #from(23.2)")]
        internal List<string> beforeTAACustomPostProcesses = new List<string>();

        #endregion

        [SerializeField, Obsolete("Keep for Migration. #from(23.2)")] internal ShaderStrippingSetting m_ShaderStrippingSetting = new();

#pragma warning disable 0414
        [SerializeField, FormerlySerializedAs("shaderVariantLogLevel"), Obsolete("Keep for Migration. #from(23.2)")] internal ShaderVariantLogLevel m_ShaderVariantLogLevel = ShaderVariantLogLevel.Disabled;
        [SerializeField, FormerlySerializedAs("supportRuntimeDebugDisplay"), Obsolete("Keep for Migration. #from(23.2)")] internal bool m_SupportRuntimeDebugDisplay = false;

        [SerializeField, Obsolete("Keep for Migration. #from(23.2)")] internal bool m_ExportShaderVariants = true;
        [SerializeField, Obsolete("Keep for Migration. #from(23.2)")] internal bool m_StripDebugVariants = false;
#pragma warning restore 0414

        [SerializeField]
        [Obsolete("This field is not used anymore. #from(2023.2)")]
        internal string DLSSProjectId = "000000";

        [SerializeField]
        [Obsolete("This field is not used anymore. #from(2023.2)")]
        internal bool useDLSSCustomProjectId = false;

        [SerializeField, Obsolete("Keep for Migration. #from(23.2)")]
        internal bool supportProbeVolumes = false;

        [Obsolete("Keep for Migration. #from(23.2)")]
        public bool autoRegisterDiffusionProfiles = true;

        [Obsolete("Keep for Migration. #from(23.2)")]
        public bool analyticDerivativeEmulation = false;

        [Obsolete("Keep for Migration. #from(23.2)")]
        public bool analyticDerivativeDebugOutput = false;

        [SerializeField]
        [Obsolete("Keep for Migration. #from(23.2)")]
        internal LensAttenuationMode lensAttenuationMode;

        [SerializeField]
        [Obsolete("Keep for Migration. #from(23.2)")]
        internal ColorGradingSpace colorGradingSpace;

        [SerializeField, FormerlySerializedAs("diffusionProfileSettingsList")]
        [Obsolete("Keep for Migration. #from(23.2)")]
        internal DiffusionProfileSettings[] m_ObsoleteDiffusionProfileSettingsList;

        [SerializeField]
        [Obsolete("Keep for Migration. #from(23.2)")]
        internal bool specularFade;

        [SerializeField]
        [Obsolete("Keep for Migration. #from(23.2)")]
        internal bool rendererListCulling;

        [SerializeField, FormerlySerializedAs("m_DefaultVolumeProfile"), FormerlySerializedAs("m_VolumeProfileDefault")]
        [Obsolete("Kept for migration. #from(2023.3)")]
        internal VolumeProfile m_ObsoleteDefaultVolumeProfile;

#if UNITY_EDITOR
        [SerializeField, FormerlySerializedAs("m_LookDevVolumeProfile"), FormerlySerializedAs("VolumeProfileLookDev")]
        [Obsolete("Kept for migration. #from(2023.3)")]
        internal VolumeProfile m_ObsoleteLookDevVolumeProfile;
#endif

        #region Camera's FrameSettings
        // To be able to turn on/off FrameSettings properties at runtime for debugging purpose without affecting the original one
        // we create a runtime copy (m_ActiveFrameSettings that is used, and any parametrization is done on serialized frameSettings)
        [SerializeField, FormerlySerializedAs("m_RenderingPathDefaultCameraFrameSettings"), Obsolete("Kept For Migration. #from(2023.2")]
        FrameSettings m_ObsoleteRenderingPathDefaultCameraFrameSettings = FrameSettingsDefaults.Get(FrameSettingsRenderType.Camera);

        [SerializeField, FormerlySerializedAs("m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings"), Obsolete("Kept For Migration. #from(2023.2")]
        FrameSettings m_ObsoleteRenderingPathDefaultBakedOrCustomReflectionFrameSettings = FrameSettingsDefaults.Get(FrameSettingsRenderType.CustomOrBakedReflection);

        [SerializeField, FormerlySerializedAs("m_RenderingPathDefaultRealtimeReflectionFrameSettings"), Obsolete("Kept For Migration. #from(2023.2")]
        FrameSettings m_ObsoleteRenderingPathDefaultRealtimeReflectionFrameSettings = FrameSettingsDefaults.Get(FrameSettingsRenderType.RealtimeReflection);

        [SerializeField, FormerlySerializedAs("m_RenderingPath"), Obsolete("Kept For Migration. #from(2023.2")]
        internal RenderingPathFrameSettings m_ObsoleteRenderingPath = new();

        [Obsolete("Kept For Migration. #from(2023.2")]
        internal ref FrameSettings GetDefaultFrameSettings(FrameSettingsRenderType type)
        {
#pragma warning disable 618 // Type or member is obsolete
            return ref m_ObsoleteRenderingPath.GetDefaultFrameSettings(type);
#pragma warning restore 618
        }

        #endregion
    }
}
