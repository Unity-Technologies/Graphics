using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class DecalData : HDTargetData
    {
        [SerializeField]
        bool m_AffectsMetal = true;
        public bool affectsMetal
        {
            get => m_AffectsMetal;
            set => m_AffectsMetal = value;
        }

        [SerializeField]
        bool m_AffectsAO = false;
        public bool affectsAO
        {
            get => m_AffectsAO;
            set => m_AffectsAO = value;
        }

        [SerializeField]
        bool m_AffectsSmoothness = true;
        public bool affectsSmoothness
        {
            get => m_AffectsSmoothness;
            set => m_AffectsSmoothness = value;
        }

        [SerializeField]
        bool m_AffectsAlbedo = true;
        public bool affectsAlbedo
        {
            get => m_AffectsAlbedo;
            set => m_AffectsAlbedo = value;
        }

        [SerializeField]
        bool m_AffectsNormal = true;
        public bool affectsNormal
        {
            get => m_AffectsNormal;
            set => m_AffectsNormal = value;
        }

        [SerializeField]
        bool m_AffectsEmission = false;
        public bool affectsEmission
        {
            get => m_AffectsEmission;
            set => m_AffectsEmission = value;
        }

        [SerializeField]
        int m_DrawOrder;
        public int drawOrder
        {
            get => m_DrawOrder;
            set => m_DrawOrder = value;
        }

        [SerializeField]
        bool m_SupportLodCrossFade;
        public bool supportLodCrossFade
        {
            get => m_SupportLodCrossFade;
            set => m_SupportLodCrossFade = value;
        }

        [SerializeField]
        bool m_TransparentDynamicUpdate;
        public bool transparentDynamicUpdate
        {
            get => m_TransparentDynamicUpdate;
            set => m_TransparentDynamicUpdate = value;
        }

        public bool affectsMaskmap => affectsSmoothness || affectsMetal || affectsAO;
    }
}
