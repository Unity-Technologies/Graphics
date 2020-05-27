using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class LightingData : HDTargetData
    {
        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace;
        public NormalDropOffSpace normalDropOffSpace
        {
            get => m_NormalDropOffSpace;
            set => m_NormalDropOffSpace = value;
        }

        [SerializeField]
        bool m_BlendPreserveSpecular = true;
        public bool blendPreserveSpecular
        {
            get => m_BlendPreserveSpecular;
            set => m_BlendPreserveSpecular = value;
        }

        [SerializeField]
        bool m_ReceiveDecals = true;
        public bool receiveDecals
        {
            get => m_ReceiveDecals;
            set => m_ReceiveDecals = value;
        }

        [SerializeField]
        bool m_ReceiveSSR = true;
        public bool receiveSSR
        {
            get => m_ReceiveSSR;
            set => m_ReceiveSSR = value;
        }

        [SerializeField]
        bool m_ReceiveSSRTransparent = true;
        public bool receiveSSRTransparent
        {
            get => m_ReceiveSSRTransparent;
            set => m_ReceiveSSRTransparent = value;
        }

        [SerializeField]
        bool m_EnergyConservingSpecular = true;
        public bool energyConservingSpecular
        {
            get => m_EnergyConservingSpecular;
            set => m_EnergyConservingSpecular = value;
        }

        [SerializeField]
        bool m_Transmission = false;
        public bool transmission
        {
            get => m_Transmission;
            set => m_Transmission = value;
        }

        [SerializeField]
        bool m_SubsurfaceScattering = false;
        public bool subsurfaceScattering
        {
            get => m_SubsurfaceScattering;
            set => m_SubsurfaceScattering = value;
        }

        [SerializeField]
        bool m_SpecularAA;
        public bool specularAA
        {
            get => m_SpecularAA;
            set => m_SpecularAA = value;
        }
        
        // TODO: Was on HDLitMasterNode but seemingly replaced by a Port
        // [SerializeField]
        // float m_SpecularAAScreenSpaceVariance;
        // public float specularAAScreenSpaceVariance
        // {
        //     get => m_SpecularAAScreenSpaceVariance;
        //     set => m_SpecularAAScreenSpaceVariance = value;
        // }

        // TODO: Was on HDLitMasterNode but seemingly replaced by a Port
        // [SerializeField]
        // float m_SpecularAAThreshold;
        // public float specularAAThreshold
        // {
        //     get => m_SpecularAAThreshold;
        //     set => m_SpecularAAThreshold = value;
        // }

        [SerializeField]
        SpecularOcclusionMode m_SpecularOcclusionMode;
        public SpecularOcclusionMode specularOcclusionMode
        {
            get => m_SpecularOcclusionMode;
            set => m_SpecularOcclusionMode = value;
        }

        [SerializeField]
        bool m_OverrideBakedGI;
        public bool overrideBakedGI
        {
            get => m_OverrideBakedGI;
            set => m_OverrideBakedGI = value;
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
    }
}
