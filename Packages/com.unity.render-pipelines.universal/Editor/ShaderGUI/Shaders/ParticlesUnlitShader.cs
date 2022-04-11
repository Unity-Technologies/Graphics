using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    internal class ParticlesUnlitShader : BaseShaderGUI
    {
        // Properties
        private BakedLitGUI.BakedLitProperties shadingModelProperties;
        private ParticleGUI.ParticleProperties particleProps;

        // List of renderers using this material in the scene, used for validating vertex streams
        List<ParticleSystemRenderer> m_RenderersUsingThisMaterial = new List<ParticleSystemRenderer>();

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            shadingModelProperties = new BakedLitGUI.BakedLitProperties(properties);
            particleProps = new ParticleGUI.ParticleProperties(properties);
        }

        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, null, ParticleGUI.SetMaterialKeywords);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            base.DrawSurfaceOptions(material);
            DoPopup(ParticleGUI.Styles.colorMode, particleProps.colorMode, Enum.GetNames(typeof(ParticleGUI.ColorMode)));
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            BakedLitGUI.Inputs(shadingModelProperties, materialEditor);
            DrawEmissionProperties(material, true);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            materialEditor.ShaderProperty(particleProps.flipbookMode, ParticleGUI.Styles.flipbookMode);
            ParticleGUI.FadingOptions(material, materialEditor, particleProps);
            ParticleGUI.DoVertexStreamsArea(material, m_RenderersUsingThisMaterial);

            DrawQueueOffsetField();
        }

        public override void OnOpenGUI(Material material, MaterialEditor materialEditor)
        {
            CacheRenderersUsingThisMaterial(material);
            base.OnOpenGUI(material, materialEditor);
        }

        void CacheRenderersUsingThisMaterial(Material material)
        {
            m_RenderersUsingThisMaterial.Clear();

            ParticleSystemRenderer[] renderers = UnityEngine.Object.FindObjectsOfType(typeof(ParticleSystemRenderer)) as ParticleSystemRenderer[];
            foreach (ParticleSystemRenderer renderer in renderers)
            {
                if (renderer.sharedMaterial == material)
                    m_RenderersUsingThisMaterial.Add(renderer);
            }
        }
    }
} // namespace UnityEditor
