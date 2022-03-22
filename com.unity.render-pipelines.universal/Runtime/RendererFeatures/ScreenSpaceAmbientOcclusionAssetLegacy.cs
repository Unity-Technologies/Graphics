using System;

namespace UnityEngine.Rendering.Universal
{
    [DisallowMultipleRendererFeature("Screen Space Ambient Occlusion")]
    [Obsolete("Renderer Features are no longer used as assets.")]
    internal class ScreenSpaceAmbientOcclusionAssetLegacy : ScriptableRendererFeatureAssetLegacy
    {
        // Serialized Fields
        [SerializeField, HideInInspector] internal Shader m_Shader = null;
        [SerializeField] internal ScreenSpaceAmbientOcclusionSettings m_Settings = new ScreenSpaceAmbientOcclusionSettings();

        // Private Fields
        //private Material m_Material;
        //private ScreenSpaceAmbientOcclusionPass m_SSAOPass = null;

        public override ScriptableRendererFeature UpgradeToRendererFeatureWithoutAsset()
        {
            return ScreenSpaceAmbientOcclusion.UpgradeFrom(this);
        }
    }
}
