using System;
using System.Collections.Generic;
using UIElements.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
    /*class ConvolutionFilterControlPresenter : GraphControlPresenter
    {
        enum KernelPresets
        {
            Presets,
            BoxBlur,
            GaussianBlur,
            EdgesDetection,
            EnhanceEdges,
            Sharpen,
            Emboss,
        };

        private void ApplyPreset(KernelPresets preset, ref float[,] values, ref float divisor)
        {
            if (preset == KernelPresets.BoxBlur)
            {
                values = new float[,] { {1,1,1,1,1},
                                        {1,1,1,1,1},
                                        {1,1,1,1,1},
                                        {1,1,1,1,1},
                                        {1,1,1,1,1} };
                divisor = 25;
            }
            if (preset == KernelPresets.GaussianBlur)
            {
                values = new float[,] { { 1, 4, 6, 4, 1},
                                        { 4,16,24,16, 4},
                                        { 6,24,36,24, 6},
                                        { 4,16,24,16, 4},
                                        { 1, 4, 6, 4, 1} };
                divisor = 256;
            }
            if (preset == KernelPresets.EdgesDetection)
            {
                values = new float[,] { {-1,-1,-1, -1,-1},
                                        {-1,-2,-2, -2,-1},
                                        {-1,-2, 33,-2,-1},
                                        {-1,-2,-2, -2,-1},
                                        {-1,-1,-1, -1,-1} };
                divisor = 1;
            }
            if (preset == KernelPresets.EnhanceEdges)
            {
                values = new float[,] { { 0, 0, 0, 0, 0},
                                        { 0, 1, 1, 1, 0},
                                        { 0, 1,-7, 1, 0},
                                        { 0, 1, 1, 1, 0},
                                        { 0, 0, 0, 0, 0} };
                divisor = 1;
            }
            if (preset == KernelPresets.Sharpen)
            {
                values = new float[,] { {-1,-1,-1, -1,-1},
                                        {-1, 2, 2,  2,-1},
                                        {-1, 2, 8,  2,-1},
                                        {-1, 2, 2,  2,-1},
                                        {-1,-1,-1, -1,-1} };
                divisor = 8;
            }
            if (preset == KernelPresets.Emboss)
            {
                values = new float[,] { {-1,-1,-1,-1, 0},
                                        {-1,-1,-1, 0, 1},
                                        {-1,-1, 0, 1, 1},
                                        {-1, 0, 1, 1, 1},
                                        { 0, 1, 1, 1, 1} };
                divisor = 1;
            }
        }

        public override void OnGUIHandler()
        {
            base.OnGUIHandler();

            var tNode = node as ConvolutionFilterNode;
            if (tNode == null)
                return;

            float divisor = tNode.GetConvolutionDivisor();
            float[,] values = new float[5,5];
            for (int row = 0; row < 5; ++row)
            {
                for (int col = 0; col < 5; ++col)
                {
                    values[row,col] = tNode.GetConvolutionWeight(row,col);
                }
            }

            EditorGUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Kernel");
            KernelPresets kernelPresets = KernelPresets.Presets;
            kernelPresets = (KernelPresets)EditorGUILayout.EnumPopup(kernelPresets);
            ApplyPreset(kernelPresets, ref values, ref divisor);

            for (int col = 0; col < 5; ++col)
            {
                EditorGUILayout.BeginHorizontal();
                for (int row = 0; row < 5; ++row)
                {
                    values[row, col] = EditorGUILayout.FloatField(values[row, col], GUILayout.Width(35));
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();

            divisor = EditorGUILayout.FloatField("Divisor", divisor);

            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                tNode.SetConvolutionDivisor(divisor);
                for (int row = 0; row < 5; ++row)
                {
                    for (int col = 0; col < 5; ++col)
                    {
                         tNode.SetConvolutionWeight(row, col, values[row, col]);
                    }
                }
            }
        }

        public override float GetHeight()
        {
            return (EditorGUIUtility.singleLineHeight * 10 + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [Serializable]
    public class ConvolutionFilterNodePresenter : PropertyNodePresenter
    {
        protected override IEnumerable<GraphElementPresenter> GetControlData()
        {
            var instance = CreateInstance<ConvolutionFilterControlPresenter>();
            instance.Initialize(node);
            return new List<GraphElementPresenter>(base.GetControlData()) { instance };
        }
    }*/
}
