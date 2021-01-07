using System;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.Rendering.MaterialVariants;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Represents an advanced options material UI block.
    /// </summary>
    public class VariantHierarchyUIBlock : MaterialUIBlock
    {
        HierarchyUI m_HierarchyUI;

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        public override void OnGUI()
        {
            if (materials.Length != 1) // No multiediting of hierarchy
                return;

            if (m_HierarchyUI == null)
                m_HierarchyUI = new HierarchyUI(materialEditor.target);

            using (var header = new MaterialHeaderScope(HierarchyUI.Styles.materialVariantHierarchyText, (uint)1, materialEditor))
            {
                if (header.expanded)
                {
                    m_HierarchyUI.OnGUI();
                }
            }
        }
    }
}
