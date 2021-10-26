using UnityEditor.ShaderGraph;
using UnityEngine;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;
using UnityEditor.Rendering.BuiltIn;
using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.Rendering;
using BlendMode = UnityEngine.Rendering.BlendMode;
using BlendOp = UnityEditor.ShaderGraph.BlendOp;

namespace UnityEditor.Rendering.Fullscreen.ShaderGraph
{
    internal class FullscreenData : JsonObject
    {
        public enum Version
        {
            Initial,
        }

        [SerializeField]
        Version m_Version = Version.Initial;
        public Version version
        {
            get => m_Version;
            set => m_Version = value;
        }

        [SerializeField]
        FullscreenMode m_fullscreenMode;
        public FullscreenMode fullscreenMode
        {
            get => m_fullscreenMode;
            set => m_fullscreenMode = value;
        }

        [SerializeField]
        FullscreenBlendMode m_BlendMode = FullscreenBlendMode.Disabled;
        public FullscreenBlendMode blendMode
        {
            get => m_BlendMode;
            set => m_BlendMode = value;
        }

        [SerializeField]
        BlendMode m_SrcColorBlendMode = BlendMode.Zero;
        public BlendMode srcColorBlendMode
        {
            get => m_SrcColorBlendMode;
            set => m_SrcColorBlendMode = value;
        }

        [SerializeField]
        BlendMode m_DstColorBlendMode = BlendMode.One;
        public BlendMode dstColorBlendMode
        {
            get => m_DstColorBlendMode;
            set => m_DstColorBlendMode = value;
        }

        [SerializeField]
        BlendOp m_ColorBlendOperation = BlendOp.Add;
        public BlendOp colorBlendOperation
        {
            get => m_ColorBlendOperation;
            set => m_ColorBlendOperation = value;
        }

        [SerializeField]
        BlendMode m_SrcAlphaBlendMode = BlendMode.Zero;
        public BlendMode srcAlphaBlendMode
        {
            get => m_SrcAlphaBlendMode;
            set => m_SrcAlphaBlendMode = value;
        }

        [SerializeField]
        BlendMode m_DstAlphaBlendMode = BlendMode.One;
        public BlendMode dstAlphaBlendMode
        {
            get => m_DstAlphaBlendMode;
            set => m_DstAlphaBlendMode = value;
        }

        [SerializeField]
        BlendOp m_AlphaBlendOperation = BlendOp.Add;
        public BlendOp alphaBlendOperation
        {
            get => m_AlphaBlendOperation;
            set => m_AlphaBlendOperation = value;
        }

        [SerializeField]
        bool m_EnableStencil = false;
        public bool enableStencil
        {
            get => m_EnableStencil;
            set => m_EnableStencil = value;
        }

        [SerializeField]
        int m_StencilReference = 0;
        public int stencilReference
        {
            get => m_StencilReference;
            set => m_StencilReference = Mathf.Clamp(value, 0, 255);
        }

        [SerializeField]
        int m_StencilReadMask = 255;
        public int stencilReadMask
        {
            get => m_StencilReadMask;
            set => m_StencilReadMask = Mathf.Clamp(value, 0, 255);
        }

        [SerializeField]
        int m_StencilWriteMask = 255;
        public int stencilWriteMask
        {
            get => m_StencilWriteMask;
            set => m_StencilWriteMask = Mathf.Clamp(value, 0, 255);
        }

        [SerializeField]
        CompareFunction m_StencilCompareFunction = CompareFunction.Always;
        public CompareFunction stencilCompareFunction
        {
            get => m_StencilCompareFunction;
            set => m_StencilCompareFunction = value;
        }

        [SerializeField]
        StencilOp m_StencilPassOperation = StencilOp.Keep;
        public StencilOp stencilPassOperation
        {
            get => m_StencilPassOperation;
            set => m_StencilPassOperation = value;
        }

        [SerializeField]
        StencilOp m_StencilFailOperation = StencilOp.Keep;
        public StencilOp stencilFailOperation
        {
            get => m_StencilFailOperation;
            set => m_StencilFailOperation = value;
        }

        [SerializeField]
        StencilOp m_StencilDepthFailOperation = StencilOp.Keep;
        public StencilOp stencilDepthTestFailOperation
        {
            get => m_StencilDepthFailOperation;
            set => m_StencilDepthFailOperation = value;
        }

        [SerializeField]
        bool m_DepthWrite = false;
        public bool depthWrite
        {
            get => m_DepthWrite;
            set => m_DepthWrite = value;
        }

        [SerializeField]
        FullscreenDepthWriteMode m_depthWriteMode = FullscreenDepthWriteMode.LinearEye;
        public FullscreenDepthWriteMode depthWriteMode
        {
            get => m_depthWriteMode;
            set => m_depthWriteMode = value;
        }

        // When checked, allows the material to control ALL surface settings (uber shader style)
        [SerializeField]
        bool m_AllowMaterialOverride = false;
        public bool allowMaterialOverride
        {
            get => m_AllowMaterialOverride;
            set => m_AllowMaterialOverride = value;
        }

        [SerializeField]
        CompareFunction m_DepthTestMode = CompareFunction.Disabled;
        public CompareFunction depthTestMode
        {
            get => m_DepthTestMode;
            set => m_DepthTestMode = value;
        }
    }
}
