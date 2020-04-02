using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    // TODO: z what have I done to this file?
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
         protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
//        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            List<ShaderGraphUIBlock> myBlocks = GetHeaderBlocks(props);

        // Printing Bullshit
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
                foreach (ShaderGraphUIBlock sgb in myBlocks)
                {
                    MaterialUIBlockList currentMatBlock = new MaterialUIBlockList();

                    // TODO: let's not keep this hack
                    int hackExpand = 1 << push;
                    push++;

                    currentMatBlock.Add(new ShaderGraphUIBlock(MaterialUIBlock.Expandable.ShaderGraph, ShaderGraphUIBlock.Features.Unlit));
                    currentMatBlock.OnGUI(materialEditor, sgb.properties);
                }
            }
        }

         void OldOnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
         {
             using (var changed = new EditorGUI.ChangeCheckScope())
             {
                 uiBlocks.OnGUI(materialEditor, props);
                 ApplyKeywordsAndPassesIfNeeded(changed.changed, uiBlocks.materials);
             }
         }

        public static void SetupMaterialKeywordsAndPass(Material material)
        {
            SynchronizeShaderGraphProperties(material);
            UnlitGUI.SetupUnlitMaterialKeywordsAndPass(material);
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);







        // TODO: z where did this come from? Do I need it?
        List<ShaderGraphUIBlock> GetHeaderBlocks(MaterialProperty[] allProperties)
        {
            string[] tooltips = null;
            string[] headers = null;

            HackweekHacks.GatherTooltipsAndHeaders(out tooltips, out headers);
            if ((tooltips == null) || (headers == null))
            {
                Debug.LogError("HackweekHacks.GatherTooltipsAndHeaders: (tooltips == null) || (headers == null)");
                return null;
            }

            int c = tooltips.Length;
            int propCount = allProperties.Length;
            if (c == 0 || propCount == 0)
            {
                return null; // No exposed properties for the Shader Graph
            }

            List<ShaderGraphUIBlock> shaderBlocks = new List<ShaderGraphUIBlock>();

            ShaderGraphUIBlock currentBlock = new ShaderGraphUIBlock();
            List<MaterialProperty> currentProperties = new List<MaterialProperty>();
            List<GUIContent> currentContents = new List<GUIContent>();

            // We always start with a header
            if (headers[0] != null)
            {
                currentBlock.header = headers[0];
            }
            else
            {
                currentBlock.header = "Exposed Properties";
            }

            int currentDisplayedIndex = 0;
            for (int x = 0; x < propCount; x++)
            {
                MaterialProperty currentProperty = allProperties[x];

                if (IsDisplayWorthy(currentProperty))
                {
                    // New block: wrap up the old and start a new one
                    if (headers[currentDisplayedIndex] != null && currentDisplayedIndex != 0)
                    {
                        currentBlock.properties = currentProperties.ToArray();
                        // currentBlock.contents = currentContents.ToArray(); // TODO: z

                        currentProperties = new List<MaterialProperty>();
                        currentContents = new List<GUIContent>();
                        shaderBlocks.Add(currentBlock);
                        currentBlock = new ShaderGraphUIBlock();

                        currentBlock.header = headers[currentDisplayedIndex];
                    }

                    currentProperties.Add(currentProperty);
                    currentContents.Add(new GUIContent(currentProperty.displayName, tooltips[currentDisplayedIndex]));

                    currentDisplayedIndex++;
                }
            }

            // Wrap up the final block
            currentBlock.properties = currentProperties.ToArray();
            // currentBlock.contents = currentContents.ToArray(); // TODO: z
            shaderBlocks.Add(currentBlock);

            return shaderBlocks;
        }

        private bool IsDisplayWorthy(MaterialProperty prop)
        {
            return ((prop.flags &
                     (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) == 0);
        }
    }
}
