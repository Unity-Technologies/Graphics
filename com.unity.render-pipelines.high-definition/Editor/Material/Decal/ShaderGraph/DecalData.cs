using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class DecalData : HDTargetData
    {
        public ExposableProperty<bool> affectsMetalProp = new ExposableProperty<bool>(true);
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

        public ExposableProperty<bool> affectsSmoothnessProp = new ExposableProperty<bool>(true);
        public bool affectsSmoothness
        {
            get => affectsSmoothnessProp.value;
            set => affectsSmoothnessProp.value = value;
        }

        public ExposableProperty<bool> affectsAlbedoProp = new ExposableProperty<bool>(true);
        public bool affectsAlbedo
        {
            get => affectsAlbedoProp.value;
            set => affectsAlbedoProp.value = value;
        }

        public ExposableProperty<bool> affectsNormalProp = new ExposableProperty<bool>(true);
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
        [SerializeField]
        bool m_AffectsMetal = true;
        [SerializeField]
        bool m_AffectsAO = false;
        [SerializeField]
        bool m_AffectsSmoothness = true;
        [SerializeField]
        bool m_AffectsAlbedo = true;
        [SerializeField]
        bool m_AffectsNormal = true;
        [SerializeField]
        bool m_AffectsEmission = false;
    }
}
