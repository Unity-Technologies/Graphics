using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    /// <summary>
    /// GUI for HDRP Hair shader graphs
    /// </summary>
    class FullScreenPassShaderGUI : HDShaderGUI
    {
        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new ShaderGraphUIBlock(MaterialUIBlock.Expandable.ShaderGraph, ShaderGraphUIBlock.Features.None),
        };

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                uiBlocks.OnGUI(materialEditor, props);

                // Apply material keywords and pass:
                if (changed.changed)
                {
                    foreach (var material in uiBlocks.materials)
                        SetupMaterialKeywordsAndPassInternal(material);
                }
            }
        }

        public static void SetupMaterialKeywordsAndPass(Material material)
        {
            // No keywords to setup for PostProcessing currently
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);
    }
}
