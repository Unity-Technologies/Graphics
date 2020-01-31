using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Linq;
using UnityEditor;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class LayersUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public static readonly GUIContent materialToCopyHeader = EditorGUIUtility.TrTextContent("Material to copy");
            public static readonly GUIContent layerNameHeader = EditorGUIUtility.TrTextContent("Layer name");
            public static readonly GUIContent uvHeader = EditorGUIUtility.TrTextContent("UV", "Also copy UV.");
            public static readonly GUIContent copyButtonIcon = EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow", "Copy Material parameters to layer. If UV is disabled, this will not copy UV.");

            public static readonly string[] headers =
            {
                "Main layer",
                "Layer 1",
                "Layer 2",
                "Layer 3",
            };

            public static readonly Expandable[] layerExpandableBits =
            {
                Expandable.MainLayer,
                Expandable.Layer1,
                Expandable.Layer2,
                Expandable.Layer3,
            };

            // We need this because LayeringOption values are not contiguous
            public static readonly Expandable[] layeringOptionsExpandableBits =
            {
                Expandable.LayeringOptionMain,
                Expandable.LayeringOption1,
                Expandable.LayeringOption2,
                Expandable.LayeringOption3,
            };
        }

        MaterialProperty layerCount = null;

        MaterialUIBlockList[] layerUIBlocks = new MaterialUIBlockList[4];

        // Enable sub-headers for surface and detail inputs
        LitSurfaceInputsUIBlock.Features litInputsFeatures = (LitSurfaceInputsUIBlock.Features.All ^ LitSurfaceInputsUIBlock.Features.LayerOptions) | LitSurfaceInputsUIBlock.Features.SubHeader;
        DetailInputsUIBlock.Features detailInputsFeatures = DetailInputsUIBlock.Features.All | DetailInputsUIBlock.Features.SubHeader;

        public LayersUIBlock()
        {
            for (int i = 0; i < 4; i++)
            {
                layerUIBlocks[i] = new MaterialUIBlockList
                {
                    new LayeringOptionsUIBlock(Styles.layeringOptionsExpandableBits[i], i),
                    new LitSurfaceInputsUIBlock((Expandable)((uint)Expandable.MainInput + i), kMaxLayerCount, i, features: litInputsFeatures),
                    new DetailInputsUIBlock((Expandable)((uint)Expandable.MainDetail + i), kMaxLayerCount, i, features: detailInputsFeatures),
                };
            }
        }

        public override void LoadMaterialProperties()
        {
            layerCount = FindProperty(kLayerCount);
        }

        public override void OnGUI()
        {
            for (int layerIndex = 0; layerIndex < layerCount.floatValue; layerIndex++)
            {
                using (var header = new MaterialHeaderScope(Styles.headers[layerIndex], (uint)Styles.layerExpandableBits[layerIndex], materialEditor))
                {
                    if (header.expanded)
                    {
                        DrawLayerGUI(layerIndex);
                    }
                }
            }
        }

        void DrawLayerGUI(int layerIndex)
        {
            layerUIBlocks[layerIndex].OnGUI(materialEditor, properties);
        }
    }
}