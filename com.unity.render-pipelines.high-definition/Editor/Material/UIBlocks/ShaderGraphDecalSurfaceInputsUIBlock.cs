using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class ShaderGraphDecalSurfaceInputsUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string header = "Surface Options";
            public static GUIContent affectsAlbedo = new GUIContent("Affects Albedo");
            public static GUIContent affectsNormal = new GUIContent("Affects Normal");
            public static GUIContent affectsMetal = new GUIContent("Affects Metal");
            public static GUIContent affectsAO = new GUIContent("Affects Ambient Occlusion");
            public static GUIContent affectsSmoothness = new GUIContent("Affects Smoothness");
            public static GUIContent affectsEmission = new GUIContent("Affects Emission");
        }

        Expandable  m_ExpandableBit;

        MaterialProperty affectsAlbedo = new MaterialProperty();
        MaterialProperty affectsNormal = new MaterialProperty();
        MaterialProperty affectsMetal = new MaterialProperty();
        MaterialProperty affectsAO = new MaterialProperty();
        MaterialProperty affectsSmoothness = new MaterialProperty();
        MaterialProperty affectsEmission = new MaterialProperty();

        public ShaderGraphDecalSurfaceInputsUIBlock(Expandable expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        public override void LoadMaterialProperties()
        {
            affectsAlbedo = FindProperty(kAffectsAlbedo);
            affectsNormal = FindProperty(kAffectsNormal);
            affectsMetal = FindProperty(kAffectsMetal);
            affectsAO = FindProperty(kAffectsAO);
            affectsSmoothness = FindProperty(kAffectsSmoothness);
            affectsEmission = FindProperty(kAffectsEmission);
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                {
                    DrawDecalGUI();
                }
            }
        }

        void DrawDecalGUI()
        {
            if (affectsAlbedo != null)
                materialEditor.ShaderProperty(affectsAlbedo, Styles.affectsAlbedo);
            if (affectsNormal != null)
                materialEditor.ShaderProperty(affectsNormal, Styles.affectsNormal);
            if (affectsMetal != null)
                materialEditor.ShaderProperty(affectsMetal, Styles.affectsMetal);
            if (affectsAO != null)
                materialEditor.ShaderProperty(affectsAO, Styles.affectsAO);
            if (affectsSmoothness != null)
                materialEditor.ShaderProperty(affectsSmoothness, Styles.affectsSmoothness);
            if (affectsEmission != null)
                materialEditor.ShaderProperty(affectsEmission, Styles.affectsEmission);
        }
    }
}
