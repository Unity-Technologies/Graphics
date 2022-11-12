using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    internal class ParticlesLitShader : BaseShaderGUI
    {
        // Properties
        private LitGUI.LitProperties litProperties;
        private ParticleGUI.ParticleProperties particleProps;

        // List of renderers using this material in the scene, used for validating vertex streams
        List<ParticleSystemRenderer> m_RenderersUsingThisMaterial = new List<ParticleSystemRenderer>();

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new LitGUI.LitProperties(properties);
            particleProps = new ParticleGUI.ParticleProperties(properties);
        }

        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            base.DrawSurfaceOptions(material);
            DoPopup(ParticleGUI.Styles.colorMode, particleProps.colorMode, Enum.GetNames(typeof(ParticleGUI.ColorMode)));
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            materialEditor.ShaderProperty(particleProps.flipbookMode, ParticleGUI.Styles.flipbookMode);
            ParticleGUI.FadingOptions(material, materialEditor, particleProps);
            ParticleGUI.DoVertexStreamsArea(material, m_RenderersUsingThisMaterial, true);

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

            ParticleSystemRenderer[] renderers = UnityEngine.Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.InstanceID);
            foreach (ParticleSystemRenderer renderer in renderers)
            {
                if (renderer.sharedMaterial == material)
                    m_RenderersUsingThisMaterial.Add(renderer);
            }
        }
    }
} // namespace UnityEditor
