using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

using RenderQueueType = UnityEngine.Rendering.HighDefinition.HDRenderQueue.RenderQueueType;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class SystemData : HDTargetData
    {
        [SerializeField]
        int m_MaterialNeedsUpdateHash;
        public int materialNeedsUpdateHash
        {
            get => m_MaterialNeedsUpdateHash;
            set => m_MaterialNeedsUpdateHash = value;
        }

        [SerializeField]
        SurfaceType m_SurfaceType = SurfaceType.Opaque;
        public SurfaceType surfaceType
        {
            get => m_SurfaceType;
            set => m_SurfaceType = value;
        }

        [SerializeField]
        RenderQueueType m_RenderingPass = RenderQueueType.Opaque;
        public RenderQueueType renderingPass
        {
            get => m_RenderingPass;
            set => m_RenderingPass = value;
        }

        [SerializeField]
        BlendMode m_BlendMode = BlendMode.Alpha;
        public BlendMode blendMode
        {
            get => m_BlendMode;
            set => m_BlendMode = value;
        }

        [SerializeField]
        CompareFunction m_ZTest = CompareFunction.LessEqual;
        public CompareFunction zTest
        {
            get => m_ZTest;
            set => m_ZTest = value;
        }    

        [SerializeField]
        bool m_ZWrite = true;
        public bool zWrite
        {
            get => m_ZWrite;
            set => m_ZWrite = value;
        }

        [SerializeField]
        TransparentCullMode m_TransparentCullMode = TransparentCullMode.Back;
        public TransparentCullMode transparentCullMode
        {
            get => m_TransparentCullMode;
            set => m_TransparentCullMode = value;
        }

        [SerializeField]
        int m_SortPriority;
        public int sortPriority
        {
            get => m_SortPriority;
            set => m_SortPriority = value;
        }

        [SerializeField]
        bool m_AlphaTest;
        public bool alphaTest
        {
            get => m_AlphaTest;
            set => m_AlphaTest = value;
        }

        [SerializeField]
        bool m_AlphaTestDepthPrepass;
        public bool alphaTestDepthPrepass
        {
            get => m_AlphaTestDepthPrepass;
            set => m_AlphaTestDepthPrepass = value;
        }

        [SerializeField]
        bool m_AlphaTestDepthPostpass;
        public bool alphaTestDepthPostpass
        {
            get => m_AlphaTestDepthPostpass;
            set => m_AlphaTestDepthPostpass = value;
        }

        [SerializeField]
        DoubleSidedMode m_DoubleSidedMode;
        public DoubleSidedMode doubleSidedMode
        {
            get => m_DoubleSidedMode;
            set => m_DoubleSidedMode = value;
        }

        [SerializeField]
        bool m_SupportLodCrossFade;
        public bool supportLodCrossFade
        {
            get => m_SupportLodCrossFade;
            set => m_SupportLodCrossFade = value;
        }

        // TODO: This was on HDUnlitMaster but not used anywhere
        // TODO: On HDLit it adds the field `HDFields.DotsInstancing`
        // TODO: Should this be added properly to HDUnlit?
        [SerializeField]
        bool m_DOTSInstancing = false;
        public bool dotsInstancing
        {
            get => m_DOTSInstancing;
            set => m_DOTSInstancing = value;
        }

        internal int inspectorFoldoutMask;
    }

    static class HDSystemDataExtensions
    {
        public static bool TryChangeRenderingPass(this SystemData systemData, HDRenderQueue.RenderQueueType value)
        {
            // Catch invalid rendering pass
            switch (value)
            {
                case HDRenderQueue.RenderQueueType.Overlay:
                case HDRenderQueue.RenderQueueType.Unknown:
                case HDRenderQueue.RenderQueueType.Background:
                    throw new ArgumentException("Unexpected kind of RenderQueue, was " + value);
            };

            // Update for SurfaceType
            switch (systemData.surfaceType)
            {
                case SurfaceType.Opaque:
                    value = HDRenderQueue.GetOpaqueEquivalent(value);
                    break;
                case SurfaceType.Transparent:
                    value = HDRenderQueue.GetTransparentEquivalent(value);
                    break;
                default:
                    throw new ArgumentException("Unknown SurfaceType");
            }

            if (Equals(systemData.renderingPass, value))
                return false;

            systemData.renderingPass = value;
            return true;
        }
    }
}
