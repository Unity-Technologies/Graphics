using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [HideInInspector]
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Tools", Order = 900)]
    [Categorization.ElementInfo(Name = "Color Checker", Order = 0)]
    class ColorCheckerResources : IRenderPipelineResources
    {
        public int version => -1;

        [SerializeField, ResourcePath("Runtime/Tools/ColorChecker/ColorCheckerMaterial.mat")]
        private Material m_ColorCheckerMaterial; 

        public Material colorCheckerMaterial
        {
            get => m_ColorCheckerMaterial;
            set => this.SetValueAndNotify(ref m_ColorCheckerMaterial, value);
        }
    }
}



