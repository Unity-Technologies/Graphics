using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using UnityEditor.ShaderGraph;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class TransparencyUIBlock : MaterialUIBlock
    {
        [Flags]
        public enum Features
        {
            None        = 0,
            Distortion  = 1 << 0,
            Refraction  = 1 << 1,
            All         = ~0
        }

        public class Styles
        {
            public const string header = "Transparency Inputs";
        }

        Expandable          m_ExpandableBit;
        Features            m_Features;
        MaterialUIBlockList m_TransparencyBlocks;

        public TransparencyUIBlock(Expandable expandableBit, Features features = Features.All)
        {
            m_ExpandableBit = expandableBit;
            m_Features = features;

            m_TransparencyBlocks = new MaterialUIBlockList(parent);
            if ((features & Features.Refraction) != 0)
                m_TransparencyBlocks.Add(new RefractionUIBlock(1));  // This block will not be used in by a layered shader so we can safely set the layer count to 1
            if ((features & Features.Distortion) != 0)
                m_TransparencyBlocks.Add(new DistortionUIBlock());
        }

        public override void LoadMaterialProperties() {}

        public override void OnGUI()
        {
            // Disable the block if one of the materials is not transparent:
            if (materials.Any(material => material.GetSurfaceType() != SurfaceType.Transparent))
                return ;

            // If refraction model is not enabled in SG, we don't show the section
            var shader = materials[0].shader;
            if (shader.IsShaderGraph())
            {
                var defaultRefractionModel = shader.GetPropertyDefaultFloatValue(shader.FindPropertyIndex(kRefractionModel));
                if (defaultRefractionModel == 0)
                    return;
            }

            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                {
                    m_TransparencyBlocks.OnGUI(materialEditor, properties);
                }
            }
        }
    }
}
