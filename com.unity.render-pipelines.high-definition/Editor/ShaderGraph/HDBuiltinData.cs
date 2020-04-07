using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDBuiltinData : HDTargetData
    {
        [SerializeField]
        bool m_TransparencyFog = true;
        public bool transparencyFog
        {
            get => m_TransparencyFog;
            set => m_TransparencyFog = value;
        }

        [SerializeField]
        bool m_AlphaToMask = false;
        public bool alphaToMask
        {
            get => m_AlphaToMask;
            set => m_AlphaToMask = value;
        }

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

        // TODO: This was on HDUnlitMaster but not used anywhere
        // TODO: Can this be removed?
        // [SerializeField]
        // bool m_DistortionOnly = true;
        // public bool distortionOnly
        // {
        //     get => m_DistortionOnly;
        //     set => m_DistortionOnly = value;
        // }

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

        // TODO: This was on HDUnlitMaster but not used anywhere
        // TODO: On HDLit it adds the field `HDFields.DotsInstancing`
        // TODO: Should this be added properly to HDUnlit?
        // [SerializeField]
        // bool m_DOTSInstancing = false;
        // public bool dotsInstancing
        // {
        //     get => m_DOTSInstancing;
        //     set => m_DOTSInstancing = value;
        // }
    }
}
