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
        bool m_AlphaToMask = false;
        public bool alphaToMask
        {
            get => m_AlphaToMask;
            set => m_AlphaToMask = value;
        }

        [SerializeField]
        bool m_DepthOffset;
        public bool depthOffset
        {
            get => m_DepthOffset;
            set => m_DepthOffset = value;
        }

        [SerializeField]
        bool m_TransparencyFog = true;
        public bool transparencyFog
        {
            get => m_TransparencyFog;
            set => m_TransparencyFog = value;
        }
    }
}
