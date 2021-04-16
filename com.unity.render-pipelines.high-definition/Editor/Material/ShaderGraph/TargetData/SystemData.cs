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

        public ExposableProperty<SurfaceType> surfaceTypeProp = new ExposableProperty<SurfaceType>(SurfaceType.Opaque, true);
        public SurfaceType surfaceType
        {
            get => surfaceTypeProp.value;
            set => surfaceTypeProp.value = value;
        }

        public ExposableProperty<RenderQueueType> renderQueueTypeProp = new ExposableProperty<RenderQueueType>(RenderQueueType.Opaque);
        public RenderQueueType renderQueueType
        {
            get => renderQueueTypeProp.value;
            set => renderQueueTypeProp.value = value;
        }

        public ExposableProperty<BlendMode> blendModeProp = new ExposableProperty<BlendMode>(BlendMode.Alpha);
        public BlendMode blendMode
        {
            get => blendModeProp.value;
            set => blendModeProp.value = value;
        }

        public ExposableProperty<CompareFunction> zTestProp = new ExposableProperty<CompareFunction>(CompareFunction.LessEqual, true);
        public CompareFunction zTest
        {
            get => zTestProp.value;
            set => zTestProp.value = value;
        }

        public ExposableProperty<bool> transparentZWriteProp = new ExposableProperty<bool>(false, true);
        public bool transparentZWrite
        {
            get => transparentZWriteProp.value;
            set => transparentZWriteProp.value = value;
        }

        public ExposableProperty<TransparentCullMode> transparentCullModeProp = new ExposableProperty<TransparentCullMode>(TransparentCullMode.Back);
        public TransparentCullMode transparentCullMode
        {
            get => transparentCullModeProp.value;
            set => transparentCullModeProp.value = value;
        }

        public ExposableProperty<OpaqueCullMode> opaqueCullModeProp = new ExposableProperty<OpaqueCullMode>(OpaqueCullMode.Back);
        public OpaqueCullMode opaqueCullMode
        {
            get => opaqueCullModeProp.value;
            set => opaqueCullModeProp.value = value;
        }

        public ExposableProperty<int> sortPriorityProp = new ExposableProperty<int>();
        public int sortPriority
        {
            get => sortPriorityProp.value;
            set => sortPriorityProp.value = value;
        }

        public ExposableProperty<bool> alphaTestProp = new ExposableProperty<bool>();
        public bool alphaTest
        {
            get => alphaTestProp.value;
            set => alphaTestProp.value = value;
        }

        [SerializeField, Obsolete("Keep for migration")]
        internal bool m_TransparentDepthPrepass;

        [SerializeField, Obsolete("Keep for migration")]
        internal bool m_TransparentDepthPostpass;

        [SerializeField, Obsolete("Keep for migration")]
        internal bool m_SupportLodCrossFade;

        public ExposableProperty<DoubleSidedMode> doubleSidedModeProp;
        public DoubleSidedMode doubleSidedMode
        {
            get => doubleSidedModeProp.value;
            set => doubleSidedModeProp.value = value;
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

        [SerializeField]
        ShaderGraphVersion m_Version = MigrationDescription.LastVersion<ShaderGraphVersion>();
        public ShaderGraphVersion version
        {
            get => m_Version;
            set => m_Version = value;
        }

        [SerializeField]
        bool m_FirstTimeMigrationExecuted = false;
        public bool firstTimeMigrationExecuted
        {
            get => m_FirstTimeMigrationExecuted;
            set => m_FirstTimeMigrationExecuted = value;
        }


        [SerializeField]
        internal int inspectorFoldoutMask;

        /*
        // Kept for migration
        [SerializeField]
        SurfaceType m_SurfaceType = SurfaceType.Opaque;
        [SerializeField]
        RenderQueueType m_RenderingPass = RenderQueueType.Opaque;
        [SerializeField]
        BlendMode m_BlendMode = BlendMode.Alpha;
        [SerializeField]
        CompareFunction m_ZTest = CompareFunction.LessEqual;
        [SerializeField]
        bool m_ZWrite = false;
        [SerializeField]
        TransparentCullMode m_TransparentCullMode = TransparentCullMode.Back;
        [SerializeField]
        OpaqueCullMode m_OpaqueCullMode = OpaqueCullMode.Back;
        [SerializeField]
        int m_SortPriority;
        [SerializeField]
        bool m_AlphaTest;
        [SerializeField]
        DoubleSidedMode m_DoubleSidedMode;
        */
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
            }
            ;

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

            if (Equals(systemData.renderQueueType, value))
                return false;

            systemData.renderQueueType = value;
            return true;
        }
    }
}
