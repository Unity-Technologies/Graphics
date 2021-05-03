using System;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class DecalData : HDTargetData
    {
        public ExposableProperty<bool> affectsMetalProp = new ExposableProperty<bool>(true, true);
        public bool affectsMetal
        {
            get => affectsMetalProp.value;
            set => affectsMetalProp.value = value;
        }

        public ExposableProperty<bool> affectsAOProp = new ExposableProperty<bool>(false);
        public bool affectsAO
        {
            get => affectsAOProp.value;
            set => affectsAOProp.value = value;
        }

        public ExposableProperty<bool> affectsSmoothnessProp = new ExposableProperty<bool>(true, true);
        public bool affectsSmoothness
        {
            get => affectsSmoothnessProp.value;
            set => affectsSmoothnessProp.value = value;
        }

        public ExposableProperty<bool> affectsAlbedoProp = new ExposableProperty<bool>(true, true);
        public bool affectsAlbedo
        {
            get => affectsAlbedoProp.value;
            set => affectsAlbedoProp.value = value;
        }

        public ExposableProperty<bool> affectsNormalProp = new ExposableProperty<bool>(true, true);
        public bool affectsNormal
        {
            get => affectsNormalProp.value;
            set => affectsNormalProp.value = value;
        }

        public ExposableProperty<bool> affectsEmissionProp = new ExposableProperty<bool>(false);
        public bool affectsEmission
        {
            get => affectsEmissionProp.value;
            set => affectsEmissionProp.value = value;
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

        // Kept for migration
        [SerializeField, Obsolete("Keep for migration")]
        bool m_AffectsMetal = true;
        [SerializeField, Obsolete("Keep for migration")]
        bool m_AffectsAO = false;
        [SerializeField, Obsolete("Keep for migration")]
        bool m_AffectsSmoothness = true;
        [SerializeField, Obsolete("Keep for migration")]
        bool m_AffectsAlbedo = true;
        [SerializeField, Obsolete("Keep for migration")]
        bool m_AffectsNormal = true;
        [SerializeField, Obsolete("Keep for migration")]
        bool m_AffectsEmission = false;

        internal void MigrateToExposableProperties()
        {
#pragma warning disable 618
            // Migrate Values
            affectsMetalProp.value = m_AffectsMetal;
            affectsAOProp.value = m_AffectsAO;
            affectsSmoothnessProp.value = m_AffectsSmoothness;
            affectsAlbedoProp.value = m_AffectsAlbedo;
            affectsNormalProp.value = m_AffectsNormal;
            affectsEmissionProp.value = m_AffectsEmission;

            // properties were implicitely unexposed, now we can make it explicit
            affectsMetalProp.IsExposed = m_AffectsMetal;
            affectsAOProp.IsExposed = m_AffectsAO;
            affectsSmoothnessProp.IsExposed = m_AffectsSmoothness;
            affectsAlbedoProp.IsExposed = m_AffectsAlbedo;
            affectsNormalProp.IsExposed = m_AffectsNormal;
            affectsEmissionProp.IsExposed = m_AffectsEmission;
#pragma warning restore 618
        }
    }
}
