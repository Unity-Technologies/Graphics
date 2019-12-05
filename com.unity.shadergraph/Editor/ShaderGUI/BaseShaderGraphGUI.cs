using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Graphing.Util;

namespace UnityEditor.ShaderGraph
{
    class BaseShaderGraphGUI : ShaderGUI
    {
        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            // new SurfaceOptionUIBlock(MaterialUIBlock.Expandable.Base, features: surfaceOptionFeatures),
            new ShaderGraphUIBlock(MaterialUIBlock.Expandable.ShaderGraph, ShaderGraphUIBlock.Features.Unlit),
        };

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
                base.OnGUI(materialEditor, props);
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

        struct ShaderGraphBlock
        {
            public string header;
            public MaterialProperty[] properties;
            public GUIContent[] contents;
        }

        List<ShaderGraphBlock> GetHeaderBlocks(MaterialProperty[] allProperties)
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

            List<ShaderGraphBlock> shaderBlocks = new List<ShaderGraphBlock>();

            ShaderGraphBlock currentBlock = new ShaderGraphBlock();
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
                        currentBlock.contents = currentContents.ToArray();

                        currentProperties = new List<MaterialProperty>();
                        currentContents = new List<GUIContent>();
                        shaderBlocks.Add(currentBlock);
                        currentBlock = new ShaderGraphBlock();

                        currentBlock.header = headers[currentDisplayedIndex];
                    }

                    currentProperties.Add(currentProperty);
                    currentContents.Add(new GUIContent(currentProperty.displayName, tooltips[currentDisplayedIndex]));

                    currentDisplayedIndex++;
                }
            }

            // Wrap up the final block
            currentBlock.properties = currentProperties.ToArray();
            currentBlock.contents = currentContents.ToArray();
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
