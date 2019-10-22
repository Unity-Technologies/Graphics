using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Linq;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class PostProcessSortingInputsUIBlock : MaterialUIBlock
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
            
 			public static GUIContent meshPostProcessDepthBiasText = new GUIContent("Mesh PostProcess Depth Bias", "Sets a depth bias to stop the PostProcess's Mesh from overlapping with other Meshes.");
	 		public static GUIContent drawOrderText = new GUIContent("Draw Order", "Controls the draw order of PostProcess Projectors. HDRP draws PostProcesss with lower values first.");
        }

        Expandable  m_ExpandableBit;
        Features    m_Features;

        protected MaterialProperty PostProcessMeshDepthBias = new MaterialProperty();
        protected const string kPostProcessMeshDepthBias = "_PostProcessMeshDepthBias";

        protected MaterialProperty drawOrder = new MaterialProperty();
        protected const string kDrawOrder = "_DrawOrder";

        public PostProcessSortingInputsUIBlock(Expandable expandableBit, Features features = Features.All)
        {
            m_ExpandableBit = expandableBit;
            m_Features = features;
        }

        public override void LoadMaterialProperties()
        {
            PostProcessMeshDepthBias = FindProperty(kPostProcessMeshDepthBias);            
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
            materialEditor.ShaderProperty(PostProcessMeshDepthBias, Styles.meshPostProcessDepthBiasText);                    
        }
    }
}
