using System;
using UnityEngine.Rendering.HighDefinition.Attributes;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Engine lighting property.</summary>
    public enum LightingProperty
    {
        /// <summary>No debug output.</summary>
        None = 0,
        /// <summary>Render only diffuse.</summary>
        DiffuseOnly,
        /// <summary>Render only specular.</summary>
        SpecularOnly,
        /// <summary>Render only direct diffuse.</summary>
        DirectDiffuseOnly,
        /// <summary>Render only direct specular.</summary>
        DirectSpecularOnly,
        /// <summary>Render only indirect diffuse.</summary>
        IndirectDiffuseOnly,
        /// <summary>Render only reflection.</summary>
        ReflectionOnly,
        /// <summary>Render only refraction.</summary>
        RefractionOnly,
        /// <summary>Render only emissive.</summary>
        EmissiveOnly
    }

    /// <summary>Output a specific debug mode.</summary>
    public enum DebugFullScreen
    {
        /// <summary>No debug output.</summary>
        None,
        /// <summary>Depth buffer.</summary>
        Depth,
        /// <summary>Screen space ambient occlusion buffer.</summary>
        ScreenSpaceAmbientOcclusion,
        /// <summary>Motion vectors buffer.</summary>
        MotionVectors,
        /// <summary> The world space position of visible surfaces.</summary>
        WorldSpacePosition
    }

    /// <summary>Use this request to define how to render an AOV.</summary>
    public unsafe struct AOVRequest
    {
        /// <summary>Default settings.</summary>
        [Obsolete("Since 2019.3, use AOVRequest.NewDefault() instead.")]
        public static readonly AOVRequest @default = default;
        /// <summary>Default settings.</summary>
        /// <returns></returns>
        public static AOVRequest NewDefault() => new AOVRequest
        {
            m_MaterialProperty = MaterialSharedProperty.None,
            m_LightingProperty = LightingProperty.None,
            m_DebugFullScreen = DebugFullScreen.None,
            m_LightFilterProperty = DebugLightFilterMode.None,
            m_OverrideRenderFormat = false
        };

        MaterialSharedProperty m_MaterialProperty;
        LightingProperty m_LightingProperty;
        DebugLightFilterMode m_LightFilterProperty;
        DebugFullScreen m_DebugFullScreen;

        // When this variable is true, HDRP will render internally with the graphics format of teh user provided AOV output buffer
        // Use the SetOverrideRenderFormat member function to change the value of this parameter.
        internal bool overrideRenderFormat => m_OverrideRenderFormat;
        internal bool m_OverrideRenderFormat;

        AOVRequest* thisPtr
        {
            get
            {
                fixed (AOVRequest* pThis = &this)
                    return pThis;
            }
        }

        /// <summary>Create a new instance by copying values from <paramref name="other"/>.</summary>
        /// <param name="other"></param>
        public AOVRequest(AOVRequest other)
        {
            m_MaterialProperty = other.m_MaterialProperty;
            m_LightingProperty = other.m_LightingProperty;
            m_DebugFullScreen = other.m_DebugFullScreen;
            m_LightFilterProperty = other.m_LightFilterProperty;
            m_OverrideRenderFormat = other.m_OverrideRenderFormat;
        }

        /// <summary>State the property to render. In case of several SetFullscreenOutput chained call, only last will be used.</summary>
        /// <param name="materialProperty">The property to render.</param>
        /// <returns>A ref return to chain calls.</returns>
        public ref AOVRequest SetFullscreenOutput(MaterialSharedProperty materialProperty)
        {
            m_MaterialProperty = materialProperty;
            return ref *thisPtr;
        }

        /// <summary>State the property to render. In case of several SetFullscreenOutput chained call, only last will be used.</summary>
        /// <param name="lightingProperty">The property to render.</param>
        /// <returns>A ref return to chain calls.</returns>
        public ref AOVRequest SetFullscreenOutput(LightingProperty lightingProperty)
        {
            m_LightingProperty = lightingProperty;
            return ref *thisPtr;
        }

        /// <summary>State the property to render. In case of several SetFullscreenOutput chained call, only last will be used.</summary>
        /// <param name="debugFullScreen">The property to render.</param>
        /// <returns>A ref return to chain calls.</returns>
        public ref AOVRequest SetFullscreenOutput(DebugFullScreen debugFullScreen)
        {
            m_DebugFullScreen = debugFullScreen;
            return ref *thisPtr;
        }

        /// <summary>Set the light filter to use.</summary>
        /// <param name="filter">The light filter to use</param>
        /// <returns>A ref return to chain calls.</returns>
        public ref AOVRequest SetLightFilter(DebugLightFilterMode filter)
        {
            m_LightFilterProperty = filter;
            return ref *thisPtr;
        }

        /// <summary>Allows AOVs to be rendered at the same format/precision as the user allocated buffers.</summary>
        /// <param name="flag">Set to true to override the rendering buffer format</param>
        /// <returns>A ref return to chain calls.</returns>
        public ref AOVRequest SetOverrideRenderFormat(bool flag)
        {
            m_OverrideRenderFormat = flag;
            return ref *thisPtr;
        }

        /// <summary>
        /// Populate the debug display settings with the AOV data.
        /// </summary>
        /// <param name="debug">The debug display settings to fill.</param>
        public void FillDebugData(DebugDisplaySettings debug)
        {
            debug.SetDebugViewCommonMaterialProperty(m_MaterialProperty);

            switch (m_LightingProperty)
            {
                case LightingProperty.DiffuseOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.DiffuseLighting);
                    break;
                case LightingProperty.SpecularOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.SpecularLighting);
                    break;
                case LightingProperty.DirectDiffuseOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.DirectDiffuseLighting);
                    break;
                case LightingProperty.DirectSpecularOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.DirectSpecularLighting);
                    break;
                case LightingProperty.IndirectDiffuseOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.IndirectDiffuseLighting);
                    break;
                case LightingProperty.ReflectionOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.ReflectionLighting);
                    break;
                case LightingProperty.RefractionOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.RefractionLighting);
                    break;
                case LightingProperty.EmissiveOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.EmissiveLighting);
                    break;
                default:
                {
                    debug.SetDebugLightingMode(DebugLightingMode.None);
                    break;
                }
            }

            debug.SetDebugLightFilterMode(m_LightFilterProperty);

            switch (m_DebugFullScreen)
            {
                case DebugFullScreen.None:
                    debug.SetFullScreenDebugMode(FullScreenDebugMode.None);
                    break;
                case DebugFullScreen.Depth:
                    debug.SetFullScreenDebugMode(FullScreenDebugMode.DepthPyramid);
                    break;
                case DebugFullScreen.ScreenSpaceAmbientOcclusion:
                    debug.SetFullScreenDebugMode(FullScreenDebugMode.ScreenSpaceAmbientOcclusion);
                    break;
                case DebugFullScreen.MotionVectors:
                    debug.SetFullScreenDebugMode(FullScreenDebugMode.MotionVectors);
                    break;
                case DebugFullScreen.WorldSpacePosition:
                    debug.SetFullScreenDebugMode(FullScreenDebugMode.WorldSpacePosition);
                    break;
                default:
                    throw new ArgumentException("Unknown DebugFullScreen");
            }
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="obj">The AOV request to compare to.</param>
        /// <returns>True if the provided AOV request is equal to this.</returns>
        public override bool Equals(object obj)
        {
            return obj is AOVRequest && ((AOVRequest)obj) == this;
        }

        /// <summary>
        /// Compares if two AOV requests have the same settings.
        /// </summary>
        /// <param name="a">The first AOVRequest to compare.</param>
        /// <param name="b">The second AOVRequest to compare.</param>
        /// <returns>True if the two AOV requests have the same settings.</returns>
        public static bool operator ==(AOVRequest a, AOVRequest b)
        {
            return a.m_DebugFullScreen == b.m_DebugFullScreen &&
                a.m_LightFilterProperty == b.m_LightFilterProperty &&
                a.m_LightingProperty == b.m_LightingProperty &&
                a.m_MaterialProperty == b.m_MaterialProperty &&
                a.m_OverrideRenderFormat == b.m_OverrideRenderFormat;
        }

        /// <summary>
        /// Compares if two AOV requests have the same settings.
        /// </summary>
        /// <param name="a">The first AOVRequest to compare.</param>
        /// <param name="b">The second AOVRequest to compare.</param>
        /// <returns>True if the two AOV requests have not the same settings.</returns>
        public static bool operator !=(AOVRequest a, AOVRequest b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Computes a hash code for the AOV Request.
        /// </summary>
        /// <returns>A hash code for the AOV Request.</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (int)m_DebugFullScreen;
            hash = hash * 23 + (int)m_LightFilterProperty;
            hash = hash * 23 + (int)m_LightingProperty;
            hash = hash * 23 + (int)m_MaterialProperty;
            hash = m_OverrideRenderFormat ? hash * 23 + 1 : hash;

            return hash;
        }
    }
}
