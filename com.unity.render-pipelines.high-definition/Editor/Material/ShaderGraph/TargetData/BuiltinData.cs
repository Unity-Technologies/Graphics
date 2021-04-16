using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class BuiltinData : HDTargetData
    {
        public ExposableProperty<bool> distortionProp = new ExposableProperty<bool>(false);
        public bool distortion
        {
            get => distortionProp.value;
            set => distortionProp.value = value;
        }

        [SerializeField]
        DistortionMode m_DistortionMode;
        public DistortionMode distortionMode
        {
            get => m_DistortionMode;
            set => m_DistortionMode = value;
        }

        [SerializeField]
        bool m_DistortionDepthTest = true;
        public bool distortionDepthTest
        {
            get => m_DistortionDepthTest;
            set => m_DistortionDepthTest = value;
        }

        public ExposableProperty<bool> addPrecomputedVelocityProp = new ExposableProperty<bool>(false);
        public bool addPrecomputedVelocity
        {
            get => addPrecomputedVelocityProp.value;
            set => addPrecomputedVelocityProp.value = value;
        }

        public ExposableProperty<bool> transparentWritesMotionVecProp = new ExposableProperty<bool>(default, true);
        public bool transparentWritesMotionVec
        {
            get => transparentWritesMotionVecProp.value;
            set => transparentWritesMotionVecProp.value = value;
        }

        public ExposableProperty<bool> alphaToMaskProp = new ExposableProperty<bool>(false);
        public bool alphaToMask
        {
            get => alphaToMaskProp.value;
            set => alphaToMaskProp.value = value;
        }

        public ExposableProperty<bool> depthOffsetProp = new ExposableProperty<bool>(default, true);
        public bool depthOffset
        {
            get => depthOffsetProp.value;
            set => depthOffsetProp.value = value;
        }

        public ExposableProperty<bool> conservativeDepthOffsetProp = new ExposableProperty<bool>(default, true);
        public bool conservativeDepthOffset
        {
            get => conservativeDepthOffsetProp.value;
            set => conservativeDepthOffsetProp.value = value;
        }

        public ExposableProperty<bool> transparencyFogProp = new ExposableProperty<bool>(true, true);
        public bool transparencyFog
        {
            get => transparencyFogProp.value;
            set => transparencyFogProp.value = value;
        }

        public ExposableProperty<bool> alphaTestShadowProp = new ExposableProperty<bool>();
        public bool alphaTestShadow
        {
            get => alphaTestShadowProp.value;
            set => alphaTestShadowProp.value = value;
        }

        public ExposableProperty<bool> backThenFrontRenderingProp = new ExposableProperty<bool>(default, true);
        public bool backThenFrontRendering
        {
            get => backThenFrontRenderingProp.value;
            set => backThenFrontRenderingProp.value = value;
        }

        public ExposableProperty<bool> transparentDepthPrepassProp = new ExposableProperty<bool>(default, true);
        public bool transparentDepthPrepass
        {
            get => transparentDepthPrepassProp.value;
            set => transparentDepthPrepassProp.value = value;
        }

        public ExposableProperty<bool> transparentDepthPostpassProp = new ExposableProperty<bool>(default, true);
        public bool transparentDepthPostpass
        {
            get => transparentDepthPostpassProp.value;
            set => transparentDepthPostpassProp.value = value;
        }

        [SerializeField]
        bool m_SupportLodCrossFade;
        public bool supportLodCrossFade
        {
            get => m_SupportLodCrossFade;
            set => m_SupportLodCrossFade = value;
        }

        // Kept for migration
        [SerializeField]
        bool m_Distortion = false;
        [SerializeField]
        bool m_AddPrecomputedVelocity = false;
        [SerializeField]
        bool m_TransparentWritesMotionVec;
        [SerializeField]
        bool m_AlphaToMask = false;
        [SerializeField]
        bool m_DepthOffset;
        [SerializeField]
        bool m_ConservativeDepthOffset;
        [SerializeField]
        bool m_TransparencyFog = true;
        [SerializeField]
        bool m_AlphaTestShadow;
        [SerializeField]
        bool m_BackThenFrontRendering;
        [SerializeField]
        bool m_TransparentDepthPrepass;
        [SerializeField]
        bool m_TransparentDepthPostpass;

        internal void MigrateToExposableProperties()
        {
            // Expose everything to keep same interface
            distortionProp.IsExposed = true;
            addPrecomputedVelocityProp.IsExposed = true;
            transparentWritesMotionVecProp.IsExposed = true;
            alphaToMaskProp.IsExposed = true;
            depthOffsetProp.IsExposed = true;
            conservativeDepthOffsetProp.IsExposed = true;
            transparencyFogProp.IsExposed = true;
            backThenFrontRenderingProp.IsExposed = true;
            transparentDepthPrepassProp.IsExposed = true;
            transparentDepthPostpassProp.IsExposed = true;
            // some properties were implicitely unexposed, now we can make it explicit
            alphaTestShadowProp.IsExposed = m_AlphaTestShadow;

            // Migrate Values
            distortionProp.value = m_Distortion;
            addPrecomputedVelocityProp.value = m_AddPrecomputedVelocity;
            transparentWritesMotionVecProp.value = m_TransparentWritesMotionVec;
            alphaToMaskProp.value = m_AlphaToMask;
            depthOffsetProp.value = m_DepthOffset;
            conservativeDepthOffsetProp.value = m_ConservativeDepthOffset;
            transparencyFogProp.value = m_TransparencyFog;
            alphaTestShadowProp.value = m_AlphaTestShadow;
            backThenFrontRenderingProp.value = m_BackThenFrontRendering;
            transparentDepthPrepassProp.value = m_TransparentDepthPrepass;
            transparentDepthPostpassProp.value = m_TransparentDepthPostpass;
        }
    }
}
