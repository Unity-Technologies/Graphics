using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

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
            SurfaceOptions = 1 << 0,
            SurfaceInputs = 1 << 1,
            Sorting = 1 << 2,
        }

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new DecalSurfaceOptionsUIBlock((MaterialUIBlock.Expandable)Expandable.SurfaceOptions),
            new ShaderGraphUIBlock((MaterialUIBlock.Expandable)Expandable.SurfaceInputs, ShaderGraphUIBlock.Features.None),
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

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            DecalUI.SetupCommonDecalMaterialKeywordsAndPass(material);
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);
    }
}
