using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class AdvancedOptionsUIBlock : MaterialUIBlock
    {
        [Flags]
        public enum Features
        {
            None                = 0,
            Instancing          = 1 << 0,
            SpecularOcclusion   = 1 << 1,
            All                 = ~0
        }

        public class Styles
        {
            public const string header = "Advanced Options";
            public static GUIContent enableSpecularOcclusionText = new GUIContent("Specular Occlusion From Bent Normal", "Requires cosine weighted bent normal and cosine weighted ambient occlusion. Specular occlusion for Reflection Probe");
        }

        protected MaterialProperty enableSpecularOcclusion = null;
        protected const string kEnableSpecularOcclusion = "_EnableSpecularOcclusion";

        Expandable  m_ExpandableBit;
        Features    m_Features;

        public AdvancedOptionsUIBlock(Expandable expandableBit, Features features = Features.All)
        {
            m_ExpandableBit = expandableBit;
            m_Features = features;
        }

        public override void LoadMaterialProperties()
        {
            enableSpecularOcclusion = FindProperty(kEnableSpecularOcclusion);
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                    DrawAdvancedOptionsGUI();
            }
        }

        void DrawAdvancedOptionsGUI()
        {
            if ((m_Features & Features.Instancing) != 0)
                materialEditor.EnableInstancingField();
            if ((m_Features & Features.SpecularOcclusion) != 0)
                materialEditor.ShaderProperty(enableSpecularOcclusion, Styles.enableSpecularOcclusionText);
        }
    }
}