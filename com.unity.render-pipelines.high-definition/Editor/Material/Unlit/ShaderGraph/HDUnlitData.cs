using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDUnlitData : HDTargetData
    {
        [SerializeField]
        bool m_EnableShadowMatte = false;
        public bool enableShadowMatte
        {
            get => m_EnableShadowMatte;
            set => m_EnableShadowMatte = value;
        }

        // TODO: This was on HDUnlitMaster but not used anywhere, can this be removed?
        // [SerializeField]
        // bool m_DistortionOnly = true;
        // public bool distortionOnly
        // {
        //     get => m_DistortionOnly;
        //     set => m_DistortionOnly = value;
        // }

        // TODO: HDUnlitMaster used this instead of DoubleSidedMode, presumably because normals are irrelevant
        // [SerializeField]
        // bool m_DoubleSided;
        // public bool doubleSided
        // {
        //     get => m_DoubleSided;
        //     set => m_DoubleSided = value;
        // }
    }
}
