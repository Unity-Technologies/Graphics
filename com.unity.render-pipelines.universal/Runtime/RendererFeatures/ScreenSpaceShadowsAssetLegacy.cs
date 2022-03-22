using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{

    [DisallowMultipleRendererFeature("Screen Space Shadows")]
    [Obsolete("Renderer Features are no longer used as assets.")]
    internal class ScreenSpaceShadowsAssetLegacy : ScriptableRendererFeatureAssetLegacy
    {
        // Serialized Fields
        [SerializeField, HideInInspector] internal Shader m_Shader = null;
        [SerializeField, HideInInspector] internal ScreenSpaceShadowsSettings m_Settings = new ScreenSpaceShadowsSettings();

        // Private Fields
        //private Material m_Material;
        //private ScreenSpaceShadowsPass m_SSShadowsPass = null;
        //private ScreenSpaceShadowsPostPass m_SSShadowsPostPass = null;

        public override ScriptableRendererFeature UpgradeToRendererFeatureWithoutAsset()
        {
            return ScreenSpaceShadows.UpgradeFrom(this);
        }
    }
}
