using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// The UI block that represents the sorting inputs for decal materials.
    /// </summary>
    public class DecalSortingInputsUIBlock : MaterialUIBlock
    {
        internal class Styles
        {
            public const string header = "Sorting Inputs";

            public static GUIContent meshDecalBiasType = new GUIContent("Mesh Decal Bias Type", "Set the type of bias that is applied to the mesh decal. Depth Bias applies a bias to the final depth value, while View bias applies a world space bias (in meters) alongside the view vector.");
            public static GUIContent meshDecalDepthBiasText = new GUIContent("Mesh Decal Depth Bias", "Sets a depth bias to stop the decal's Mesh from overlapping with other Meshes.");
            public static GUIContent meshDecalViewBiasText = new GUIContent("Mesh Decal View Bias", "Sets a world-space bias alongside the view vector to stop the decal's Mesh from overlapping with other Meshes. The unit is meters.");
            public static GUIContent drawOrderText = new GUIContent("Draw Order", "Controls the draw order of Decal Projectors. HDRP draws decals with lower values first.");
        }

        ExpandableBit  m_ExpandableBit;

        MaterialProperty decalMeshBiasType = new MaterialProperty();
        const string kDecalMeshBiasType = "_DecalMeshBiasType";

        MaterialProperty decalMeshDepthBias = new MaterialProperty();
        const string kDecalMeshDepthBias = "_DecalMeshDepthBias";

        MaterialProperty decalMeshViewBias = new MaterialProperty();
        const string kDecalViewDepthBias = "_DecalMeshViewBias";

        MaterialProperty drawOrder = new MaterialProperty();
        const string kDrawOrder = "_DrawOrder";

        /// <summary>
        /// Constructs the DecalSortingInputsUIBlock.
        /// </summary>
        /// <param name="expandableBit">Bit used for the foldout state.</param>
        public DecalSortingInputsUIBlock(ExpandableBit expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            decalMeshBiasType = FindProperty(kDecalMeshBiasType);
            decalMeshViewBias = FindProperty(kDecalViewDepthBias);
            decalMeshDepthBias = FindProperty(kDecalMeshDepthBias);
            drawOrder = FindProperty(kDrawOrder);
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
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
