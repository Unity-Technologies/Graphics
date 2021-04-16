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

        public ExposableProperty<bool> receiveDecalsProp = new ExposableProperty<bool>(true);
        public bool receiveDecals
        {
            get => receiveDecalsProp.value;
            set => receiveDecalsProp.value = value;
        }

        public ExposableProperty<bool> receiveSSRProp = new ExposableProperty<bool>(true);
        public bool receiveSSR
        {
            get => receiveSSRProp.value;
            set => receiveSSRProp.value = value;
        }

        public ExposableProperty<bool> receiveSSRTransparentProp = new ExposableProperty<bool>(false);
        public bool receiveSSRTransparent
        {
            get => receiveSSRTransparentProp.value;
            set => receiveSSRTransparentProp.value = value;
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

        // Kept for migration
        [SerializeField]
        bool m_ReceiveDecals = true;
        [SerializeField]
        bool m_ReceiveSSR = true;
        [SerializeField]
        bool m_ReceiveSSRTransparent = false;

        internal void MigrateToExposableProperties()
        {
            // Expose everything to keep same interface
            receiveDecalsProp.IsExposed = true;
            receiveSSRProp.IsExposed = true;
            receiveSSRTransparentProp.IsExposed = true;

            // Migrate Values
            receiveDecalsProp.value = m_ReceiveDecals;
            receiveSSRProp.value = m_ReceiveSSR;
            receiveSSRTransparentProp.value = m_ReceiveSSRTransparent;
        }
    }
}
