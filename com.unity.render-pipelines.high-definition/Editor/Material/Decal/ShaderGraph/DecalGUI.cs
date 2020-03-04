using System;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Shadergraph Decal materials
    /// </summary>
    class DecalGUI : HDShaderGUI
    {
        [Flags]
        enum Expandable : uint
        {
            Sorting = 1 << 0,
            ShaderGraph = 1 << 1,
        }

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new ShaderGraphUIBlock((MaterialUIBlock.Expandable)Expandable.ShaderGraph, ShaderGraphUIBlock.Features.None),
            new DecalSortingInputsUIBlock((MaterialUIBlock.Expandable)Expandable.Sorting),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            // always instanced
            SerializedProperty instancing = materialEditor.serializedObject.FindProperty("m_EnableInstancingVariants");
            instancing.boolValue = true;

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                uiBlocks.OnGUI(materialEditor, props);
                ApplyKeywordsAndPassesIfNeeded(changed.changed, uiBlocks.materials);
            }

            // We should always do this call at the end
            materialEditor.serializedObject.ApplyModifiedProperties();
        }

        // We don't have any keyword/pass to setup currently for decal shader graphs
        protected override void SetupMaterialKeywordsAndPassInternal(Material material) {}
    }
}
