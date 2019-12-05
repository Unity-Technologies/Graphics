using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Unlit shader graphs
    /// </summary>
    class HDUnlitGUI : HDShaderGUI
    {
        // For surface option shader graph we only want all unlit features but alpha clip, double sided mode and back then front rendering
        const SurfaceOptionUIBlock.Features   surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Unlit
            ^ SurfaceOptionUIBlock.Features.AlphaCutoffThreshold
            ^ SurfaceOptionUIBlock.Features.DoubleSidedNormalMode
            ^ SurfaceOptionUIBlock.Features.BackThenFrontRendering;

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.Expandable.Base, features: surfaceOptionFeatures),
            new ShaderGraphUIBlock(MaterialUIBlock.Expandable.ShaderGraph, ShaderGraphUIBlock.Features.Unlit),
        };

// TODO: zz
//         protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            List<ShaderGraphBlock> myBlocks = GetHeaderBlocks(props);
//            for (int x = 0; x < myBlocks.Count; x++)
//            {
//                ShaderGraphBlock theBlock = myBlocks[x];
//                int t = theBlock.contents.Length;
//
//                Debug.Log(x.ToString() + " === " + theBlock.header + " ===   " + t.ToString());
//
//                for (int q = 0; q < t; q++)
//                {
//                    Debug.Log("   " + theBlock.contents[q].text + " " + theBlock.contents[q].tooltip + "     ===> " + theBlock.properties[q]);
//                }
//            }

            if (myBlocks == null)
            {
                OldOnGUI(materialEditor, props);
            }
            else
            {
                int push = 1;
                foreach (ShaderGraphBlock sgb in myBlocks)
                {
                    MaterialUIBlockList currentMatBlock = new MaterialUIBlockList();

                    // TODO: let's not keep this hack
                    int hackExpand = 1 << push;
                    push++;

                    currentMatBlock.Add(new ShaderGraphUIBlock((MaterialUIBlock.Expandable)hackExpand, ShaderGraphUIBlock.Features.Unlit, sgb.header));
                    currentMatBlock.OnGUI(materialEditor, sgb.properties);
                }
            }
        }

// TODO: zz
//         void OldOnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
//         {
//             using (var changed = new EditorGUI.ChangeCheckScope())
//             {
//                 uiBlocks.OnGUI(materialEditor, props);
//                 ApplyKeywordsAndPassesIfNeeded(changed.changed, uiBlocks.materials);
//             }
//         }

        public static void SetupMaterialKeywordsAndPass(Material material)
        {
            SynchronizeShaderGraphProperties(material);
            UnlitGUI.SetupUnlitMaterialKeywordsAndPass(material);
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);
    }
}
