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

            public static GUIContent meshDecalBiasType = new GUIContent("Mesh Decal Bias Type", "Set the type of bias that is applied to the mesh decal. Depth Bias applies a bias to the final depth value, while View bias applies a world space bias (in meters) alongside the view vector.");
            public static GUIContent meshDecalDepthBiasText = new GUIContent("Mesh Decal Depth Bias", "Sets a depth bias to stop the decal's Mesh from overlapping with other Meshes.");
            public static GUIContent meshDecalViewBiasText = new GUIContent("Mesh Decal View Bias", "Sets a world-space bias alongside the view vector to stop the decal's Mesh from overlapping with other Meshes. The unit is meters.");
            public static GUIContent drawOrderText = new GUIContent("Draw Order", "Controls the draw order of Decal Projectors. HDRP draws decals with lower values first.");
        }

        Expandable     m_ExpandableBit;
        Features       m_Features;
 
		protected MaterialProperty decalMeshBiasType = new MaterialProperty();
        protected const string kDecalMeshBiasType = "_DecalMeshBiasType";

        protected MaterialProperty decalMeshDepthBias = new MaterialProperty();
        protected const string kDecalMeshDepthBias = "_DecalMeshDepthBias";

        protected MaterialProperty decalMeshViewBias = new MaterialProperty();
        protected const string kDecalViewDepthBias = "_DecalMeshViewBias";

        protected MaterialProperty drawOrder = new MaterialProperty();
        protected const string kDrawOrder = "_DrawOrder";


        public DecalSortingInputsUIBlock(Expandable expandableBit, Features features = Features.All)
        {
            m_ExpandableBit = expandableBit;
            m_Features = features;
        }

        public override void LoadMaterialProperties()
        {
            decalMeshBiasType = FindProperty(kDecalMeshBiasType);
            decalMeshViewBias = FindProperty(kDecalViewDepthBias);
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
            EditorGUI.BeginChangeCheck();
            materialEditor.ShaderProperty(drawOrder, Styles.drawOrderText);
            if (EditorGUI.EndChangeCheck())
                drawOrder.floatValue = Math.Max(-HDRenderQueue.meshDecalPriorityRange, Math.Min((int)drawOrder.floatValue, HDRenderQueue.meshDecalPriorityRange));

            materialEditor.ShaderProperty(decalMeshBiasType, Styles.meshDecalBiasType);
            if ((int)decalMeshBiasType.floatValue == (int)DecalMeshDepthBiasType.DepthBias)
            {
                materialEditor.ShaderProperty(decalMeshDepthBias, Styles.meshDecalDepthBiasText);
            }
            else if ((int)decalMeshBiasType.floatValue == (int)DecalMeshDepthBiasType.ViewBias)
            {
                materialEditor.ShaderProperty(decalMeshViewBias, Styles.meshDecalViewBiasText);
            }
        }
    }
}
