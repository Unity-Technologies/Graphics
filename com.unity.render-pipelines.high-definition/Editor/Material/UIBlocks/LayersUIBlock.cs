using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using UnityEditor;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class LayersUIBlock : MaterialUIBlock
    {
        public class Styles
        {
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

        MaterialUIBlockList[] layerUIBlocks;

        // Enable sub-headers for surface and detail inputs
        LitSurfaceInputsUIBlock.Features litInputsFeatures = (LitSurfaceInputsUIBlock.Features.All ^ LitSurfaceInputsUIBlock.Features.LayerOptions) | LitSurfaceInputsUIBlock.Features.SubHeader;
        DetailInputsUIBlock.Features detailInputsFeatures = DetailInputsUIBlock.Features.All | DetailInputsUIBlock.Features.SubHeader;

        void CreateUIBlockLayers()
        {
            layerUIBlocks = new MaterialUIBlockList[4];

            for (int i = 0; i < 4; i++)
            {
                layerUIBlocks[i] = new MaterialUIBlockList(parent)
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
            if (layerUIBlocks == null)
                CreateUIBlockLayers();

            for (int layerIndex = 0; layerIndex < layerCount.floatValue; layerIndex++)
            {
                using (var header = new MaterialHeaderScope(Styles.headers[layerIndex], (uint)Styles.layerExpandableBits[layerIndex], materialEditor, colorDot: kLayerColors[layerIndex]))
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
