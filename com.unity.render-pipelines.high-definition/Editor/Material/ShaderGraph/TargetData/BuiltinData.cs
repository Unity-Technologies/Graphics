using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class BuiltinData : HDTargetData
    {
        [SerializeField]
        bool m_Distortion = false;
        public bool distortion
        {
            get => m_Distortion;
            set => m_Distortion = value;
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

        [SerializeField]
        bool m_AddPrecomputedVelocity = false;
        public bool addPrecomputedVelocity
        {
            get => m_AddPrecomputedVelocity;
            set => m_AddPrecomputedVelocity = value;
        }

        [SerializeField]
        bool m_TransparentWritesMotionVec;
        public bool transparentWritesMotionVec
        {
            get => m_TransparentWritesMotionVec;
            set => m_TransparentWritesMotionVec = value;
        }

        [SerializeField]
        bool m_DepthOffset;
        public bool depthOffset
        {
            get => m_DepthOffset;
            set => m_DepthOffset = value;
        }

        [SerializeField]
        bool m_ConservativeDepthOffset;
        public bool conservativeDepthOffset
        {
            get => m_ConservativeDepthOffset;
            set => m_ConservativeDepthOffset = value;
        }

        [SerializeField]
        bool m_TransparencyFog = true;
        public bool transparencyFog
        {
            get => m_TransparencyFog;
            set => m_TransparencyFog = value;
        }

        [SerializeField]
        bool m_AlphaTestShadow;
        public bool alphaTestShadow
        {
            get => m_AlphaTestShadow;
            set => m_AlphaTestShadow = value;
        }

        [SerializeField]
        bool m_BackThenFrontRendering;
        public bool backThenFrontRendering
        {
            get => m_BackThenFrontRendering;
            set => m_BackThenFrontRendering = value;
        }

        [SerializeField]
        bool m_TransparentDepthPrepass;
        public bool transparentDepthPrepass
        {
            get => m_TransparentDepthPrepass;
            set => m_TransparentDepthPrepass = value;
        }

        [SerializeField]
        bool m_TransparentDepthPostpass;
        public bool transparentDepthPostpass
        {
            get => m_TransparentDepthPostpass;
            set => m_TransparentDepthPostpass = value;
        }

        [SerializeField]
        bool m_SupportLodCrossFade;
        public bool supportLodCrossFade
        {
            get => m_SupportLodCrossFade;
            set => m_SupportLodCrossFade = value;
        }
    }
}
