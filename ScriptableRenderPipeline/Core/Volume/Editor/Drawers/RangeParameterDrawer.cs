using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [VolumeParameterDrawer(typeof(RangeParameter))]
    sealed class RangeParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Vector2)
                return false;

            var o = parameter.GetObjectRef<RangeParameter>();
            var v = value.vector2Value;

            // The layout system breaks alignement when mixing inspector fields with custom layouted
            // fields as soon as a scrollbar is needed in the inspector, so we'll do the layout
            // manually instead
            const int kFloatFieldWidth = 50;
            const int kSeparatorWidth = 5;
            float indentOffset = EditorGUI.indentLevel * 15f;
            var lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            lineRect.xMin += 4f;
            lineRect.y += 2f;
            var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
            var floatFieldLeft = new Rect(labelRect.xMax, lineRect.y, kFloatFieldWidth + indentOffset, lineRect.height);
            var sliderRect = new Rect(floatFieldLeft.xMax + kSeparatorWidth - indentOffset, lineRect.y, lineRect.width - labelRect.width - kFloatFieldWidth * 2 - kSeparatorWidth * 2, lineRect.height);
            var floatFieldRight = new Rect(sliderRect.xMax + kSeparatorWidth - indentOffset, lineRect.y, kFloatFieldWidth + indentOffset, lineRect.height);

            EditorGUI.PrefixLabel(labelRect, title);
            v.x = EditorGUI.FloatField(floatFieldLeft, v.x);
            EditorGUI.MinMaxSlider(sliderRect, ref v.x, ref v.y, o.min, o.max);
            v.y = EditorGUI.FloatField(floatFieldRight, v.y);

            value.vector2Value = v;
            return true;
        }
    }
}
