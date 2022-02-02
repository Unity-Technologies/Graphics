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
        bool m_ReceiveSSRTransparent = false;
        public bool receiveSSRTransparent
        {
            get => m_ReceiveSSRTransparent;
            set => m_ReceiveSSRTransparent = value;
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
        SpecularOcclusionMode m_SpecularOcclusionMode = SpecularOcclusionMode.FromAO;
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
    }
}
