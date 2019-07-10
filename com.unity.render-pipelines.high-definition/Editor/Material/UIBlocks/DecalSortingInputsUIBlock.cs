using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class DecalSortingInputsUIBlock : MaterialUIBlock
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
            public const string header = "Sorting Inputs";

 			public static GUIContent meshDecalDepthBiasText = new GUIContent("Mesh Decal Depth Bias", "Sets a depth bias to stop the decal's Mesh from overlapping with other Meshes.");
	 		public static GUIContent drawOrderText = new GUIContent("Draw Order", "Controls the draw order of Decal Projectors. HDRP draws decals with lower values first.");
        }

        Expandable  m_ExpandableBit;
        Features    m_Features;

        protected MaterialProperty decalMeshDepthBias = new MaterialProperty();
        protected const string kDecalMeshDepthBias = "_DecalMeshDepthBias";

        protected MaterialProperty drawOrder = new MaterialProperty();
        protected const string kDrawOrder = "_DrawOrder";

        public DecalSortingInputsUIBlock(Expandable expandableBit, Features features = Features.All)
        {
            m_ExpandableBit = expandableBit;
            m_Features = features;
        }

        public override void LoadMaterialProperties()
        {
            decalMeshDepthBias = FindProperty(kDecalMeshDepthBias);
            drawOrder = FindProperty(kDrawOrder);
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                {
                    DrawSortingInputsGUI();
                }
            }
        }

        void DrawSortingInputsGUI()
        {
            materialEditor.ShaderProperty(drawOrder, Styles.drawOrderText);
            materialEditor.ShaderProperty(decalMeshDepthBias, Styles.meshDecalDepthBiasText);
        }
    }
}
