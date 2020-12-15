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

            public static GUIContent meshDecalDepthBiasText = new GUIContent("Mesh Decal Depth Bias", "Sets a depth bias to stop the decal's Mesh from overlapping with other Meshes.");
            public static GUIContent drawOrderText = new GUIContent("Draw Order", "Controls the draw order of Decal Projectors. HDRP draws decals with lower values first.");
        }

        ExpandableBit  m_ExpandableBit;

        MaterialProperty decalMeshDepthBias = new MaterialProperty();
        const string kDecalMeshDepthBias = "_DecalMeshDepthBias";

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
            materialEditor.ShaderProperty(decalMeshDepthBias, Styles.meshDecalDepthBiasText);
        }
    }
}
