using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Linq;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class TransparencyUIBlock : MaterialUIBlock
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

        Expandable  m_ExpandableBit;
        Features    m_Features;

        MaterialUIBlockList transparencyBlocks = new MaterialUIBlockList
        {
            new RefractionUIBlock(1), // This block will not be used in by a layered shader so we can safely set the layer count to 1
            new DistortionUIBlock(),
        };

        public TransparencyUIBlock(Expandable expandableBit, Features features = Features.All)
        {
            m_ExpandableBit = expandableBit;
            m_Features = features;
        }

        public override void LoadMaterialProperties() {}

        public override void OnGUI()
        {
            // Disable the block if one of the materials is not transparent:
            if (materials.Any(material => material.GetSurfaceType() != SurfaceType.Transparent))
                return ;
            
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                {
                    transparencyBlocks.OnGUI(materialEditor, properties);
                }
            }
        }
    }
}