using UnityEngine;

namespace UnityEditor.U2D
{
    namespace UnityEngine.Experimental.Rendering.LightweightPipeline
    {
        public class Light2DEditorGUI
        {
            private const float kSpacingSubLabel = 2.0f;
            private const float kMiniLabelW = 13;
            private const int kVerticalSpacingMultiField = 0;
            private const float kIndentPerLevel = 15;
            public static int s_FoldoutHash = "Foldout".GetHashCode();

            public static GUIContent IconContent(string name, string tooltip = null)
            {
                return new GUIContent(Resources.Load<Texture>(name), tooltip);
            }

            public static void MultiDelayedIntField(Rect position, GUIContent[] subLabels, int[] values, float labelWidth)
            {
                int eCount = values.Length;
                float w = (position.width - (eCount - 1) * kSpacingSubLabel) / eCount;
                Rect nr = new Rect(position);
                nr.width = w;
                float t = EditorGUIUtility.labelWidth;
                int l = EditorGUI.indentLevel;
                EditorGUIUtility.labelWidth = labelWidth;
                EditorGUI.indentLevel = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = EditorGUI.DelayedIntField(nr, subLabels[i], values[i]);
                    nr.x += w + kSpacingSubLabel;
                }
                EditorGUIUtility.labelWidth = t;
                EditorGUI.indentLevel = l;
            }

            public static Rect MultiFieldPrefixLabel(Rect totalPosition, int id, GUIContent label, int columns)
            {
                if (!LabelHasContent(label))
                {
                    return EditorGUI.IndentedRect(totalPosition);
                }

                if (EditorGUIUtility.wideMode)
                {
                    Rect labelPosition = new Rect(totalPosition.x + EditorGUI.indentLevel * kIndentPerLevel, totalPosition.y, EditorGUIUtility.labelWidth - EditorGUI.indentLevel * kIndentPerLevel, EditorGUIUtility.singleLineHeight);
                    Rect fieldPosition = totalPosition;
                    fieldPosition.xMin += EditorGUIUtility.labelWidth;

                    if (columns > 1)
                    {
                        labelPosition.width -= 1;
                        fieldPosition.xMin -= 1;
                    }

                    if (columns == 2)
                    {
                        float columnWidth = (fieldPosition.width - (3 - 1) * kSpacingSubLabel) / 3f;
                        fieldPosition.xMax -= (columnWidth + kSpacingSubLabel);
                    }

                    EditorGUI.HandlePrefixLabel(totalPosition, labelPosition, label, id);
                    return fieldPosition;
                }
                else
                {
                    Rect labelPosition = new Rect(totalPosition.x + EditorGUI.indentLevel * kIndentPerLevel, totalPosition.y, totalPosition.width - EditorGUI.indentLevel * kIndentPerLevel, EditorGUIUtility.singleLineHeight);
                    Rect fieldPosition = totalPosition;
                    fieldPosition.xMin += EditorGUI.indentLevel * kIndentPerLevel + kIndentPerLevel;
                    fieldPosition.yMin += EditorGUIUtility.singleLineHeight + kVerticalSpacingMultiField;
                    EditorGUI.HandlePrefixLabel(totalPosition, labelPosition, label, id);
                    return fieldPosition;
                }
            }

            private static bool LabelHasContent(GUIContent label)
            {
                if (label == null)
                    return true;

                return label.text != string.Empty || label.image != null;
            }
        }
    }
}
