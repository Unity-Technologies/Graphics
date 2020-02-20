using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition.Compositor;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class CompositionFilterUI
    {
        static partial class TextUI
        {
            static public readonly GUIContent ChromaKeing = EditorGUIUtility.TrTextContent("Chroma Keying", "Performs chroma keying in the input frame before any other composition operations are performed.");
            static public readonly GUIContent KeyColor = EditorGUIUtility.TrTextContent("Key Color", "Use this parameter to smooth-out the edges of the generated mask. A value of 0 corresponds to sharp edges.");
            static public readonly GUIContent Threshold = EditorGUIUtility.TrTextContent("Key Threshold", "Indicates the areas of the image that will be masked (or in other words, the areas that are transparent.");
            static public readonly GUIContent Tolerance = EditorGUIUtility.TrTextContent("Key Tolerance", "Controls the sensitivity of the mask color parameter. Increasing this value will include more pixels (with a value close to the mask color) in the masked areas.");
            static public readonly GUIContent SpillRemoval = EditorGUIUtility.TrTextContent("Spill Removal", "Use this parameter to change the tint of non-masked areas.");
            static public readonly GUIContent AlphaMask = EditorGUIUtility.TrTextContent("Alpha Mask", "A static texture that overrides the alpha mask of the sub-layer. Post-processing is then applied only on the masked frame regions.");
        }
        public static void Draw(Rect rect, SerializedCompositionFilter serialized)
        {
            rect.height = CompositorStyle.k_SingleLineHeight;
            float spacing = rect.height * 1.1f;

            if (serialized.FilterType.intValue == (int)CompositionFilter.FilterType.CHROMA_KEYING)
            {
                SerializedProperty KeyColor = serialized.MaskColor;
                SerializedProperty KeyThreshold = serialized.KeyThreshold;
                SerializedProperty KeyTolerance = serialized.KeyTolerance;
                SerializedProperty SpillRemoval = serialized.SpillRemoval;

                EditorGUI.LabelField(rect, TextUI.ChromaKeing);
                rect.y += spacing;
                rect.x += 20;
                rect.width -= 20;
                EditorGUI.PropertyField(rect, KeyColor, TextUI.KeyColor);
                rect.y += spacing;
                EditorGUI.PropertyField(rect, KeyThreshold, TextUI.Threshold);
                rect.y += spacing;
                EditorGUI.PropertyField(rect, KeyTolerance, TextUI.Tolerance);
                rect.y += spacing;
                EditorGUI.PropertyField(rect, SpillRemoval, TextUI.SpillRemoval);
            }
            else
            {
                SerializedProperty AlphaMask = serialized.AlphaMask;
                EditorGUI.PropertyField(rect, AlphaMask, TextUI.AlphaMask);
            }
        }
    }
}
