using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;
using static UnityEngine.Experimental.Rendering.HDPipeline.MaterialDebugSettings;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Flags]
    public enum LightingProperty
    {
        DiffuseOnlyDirectional = 1 << 0,
        DiffuseOnlyIndirect = 1 << 1,
        SpecularOnlyDirectional = 1 << 2,
        SpecularOnlyIndirect = 1 << 3
    }

    public enum DebugFullScreen
    {
        None,
        Depth,
        ScreenSpaceAmbientOcclusion,
        MotionVectors
    }

    public unsafe struct FramePassSettings
    {
        public static FramePassSettings @default = new FramePassSettings
        {
            m_MaterialProperty = MaterialSharedProperty.None,
            m_LightingProperty = LightingProperty.DiffuseOnlyDirectional | LightingProperty.DiffuseOnlyIndirect | LightingProperty.SpecularOnlyDirectional | LightingProperty.SpecularOnlyIndirect,
            m_DebugFullScreen = DebugFullScreen.None
        };

        MaterialSharedProperty m_MaterialProperty;
        LightingProperty m_LightingProperty;
        DebugFullScreen m_DebugFullScreen;

        FramePassSettings* thisPtr
        {
            get
            {
                fixed (FramePassSettings* pThis = &this)
                    return pThis;
            }
        }

        public FramePassSettings(FramePassSettings other)
        {
            m_MaterialProperty = other.m_MaterialProperty;
            m_LightingProperty = other.m_LightingProperty;
            m_DebugFullScreen = other.m_DebugFullScreen;
        }

        /// <summary>State the property to render. In case of several SetFullscreenOutput chained call, only last will be used.</summary>
        public ref FramePassSettings SetFullscreenOutput(MaterialSharedProperty materialProperty)
        {
            m_MaterialProperty = materialProperty;
            return ref *thisPtr;
        }

        /// <summary>State the property to render. In case of several SetFullscreenOutput chained call, only last will be used.</summary>
        public ref FramePassSettings SetFullscreenOutput(LightingProperty lightingProperty)
        {
            m_LightingProperty = lightingProperty;
            return ref *thisPtr;
        }

        /// <summary>State the property to render. In case of several SetFullscreenOutput chained call, only last will be used.</summary>
        public ref FramePassSettings SetFullscreenOutput(DebugFullScreen debugFullScreen)
        {
            m_DebugFullScreen = debugFullScreen;
            return ref *thisPtr;
        }

        // Usage example:
        // (new FramePassSettings(FramePassSettings.@default)).SetFullscreenOutput(prop).FillDebugData((RenderPipelineManager.currentPipeline as HDRenderPipeline).debugDisplaySettings.data);
        public void FillDebugData(DebugDisplaySettings.DebugData data)
        {
            data.materialDebugSettings.SetDebugViewCommonMaterialProperty(m_MaterialProperty);

            switch (m_LightingProperty)
            {
                case LightingProperty.DiffuseOnlyDirectional | LightingProperty.DiffuseOnlyIndirect | LightingProperty.SpecularOnlyDirectional | LightingProperty.SpecularOnlyIndirect:
                    data.lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
                    break;
                case LightingProperty.DiffuseOnlyDirectional | LightingProperty.DiffuseOnlyIndirect:
                    data.lightingDebugSettings.debugLightingMode = DebugLightingMode.DiffuseLighting;
                    break;
                case LightingProperty.SpecularOnlyDirectional | LightingProperty.SpecularOnlyIndirect:
                    data.lightingDebugSettings.debugLightingMode = DebugLightingMode.SpecularLighting;
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (m_DebugFullScreen)
            {
                case DebugFullScreen.None:
                    data.fullScreenDebugMode = FullScreenDebugMode.None;
                    break;
                case DebugFullScreen.Depth:
                    data.fullScreenDebugMode = FullScreenDebugMode.DepthPyramid;
                    break;
                case DebugFullScreen.ScreenSpaceAmbientOcclusion:
                    data.fullScreenDebugMode = FullScreenDebugMode.SSAO;
                    break;
                case DebugFullScreen.MotionVectors:
                    data.fullScreenDebugMode = FullScreenDebugMode.MotionVectors;
                    break;
                default:
                    throw new ArgumentException("Unknown DebugFullScreen");
            }
        }
    }
}

