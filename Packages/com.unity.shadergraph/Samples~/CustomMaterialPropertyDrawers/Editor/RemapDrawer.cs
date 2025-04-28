using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Samples
{
    public class RemapDrawer : MaterialPropertyDrawer
    {
        private Vector2 range = Vector2.up;

        public RemapDrawer() { }

        public RemapDrawer(float min, float max)
        {
            this.range.x = min;
            this.range.y = max;
        }

        public RemapDrawer(float min, float max, float negativeOffset)
        {
            this.range.x = min - negativeOffset;
            this.range.y = max - negativeOffset;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return 48;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            editor.BeginAnimatedCheck(position, prop);
            using (new EditorGUI.DisabledScope((prop.propertyFlags & UnityEngine.Rendering.ShaderPropertyFlags.PerRendererData) != 0))
            {
                MaterialEditor.BeginProperty(position, prop);
                float labelWidth = EditorGUIUtility.labelWidth;

                var labelRect = position;
                labelRect.height *= .5f;
                var sliderRect = position;
                sliderRect.height *= .5f;
                sliderRect.y += labelRect.height;
                var minFieldRect = sliderRect;
                var maxFieldRect = sliderRect;
                var rangeFieldWidth = minFieldRect.width = maxFieldRect.width = 64f;

                sliderRect.x += rangeFieldWidth;
                sliderRect.width -= rangeFieldWidth * 2;
                maxFieldRect.x = sliderRect.xMax;

                Vector2 v = prop.vectorValue;
                float min = v.x, max = v.y;

                EditorGUIUtility.labelWidth = 0f;
                EditorGUI.LabelField(labelRect, label);

                EditorGUI.showMixedValue = prop.hasMixedValue;

                EditorGUI.BeginChangeCheck();

                min = Mathf.Clamp(EditorGUI.FloatField(minFieldRect, prop.vectorValue.x), range.x, max);
                max = Mathf.Clamp(EditorGUI.FloatField(maxFieldRect, prop.vectorValue.y), min, range.y);
                if (!prop.hasMixedValue)
                    EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, range.x, range.y);

                if (EditorGUI.EndChangeCheck())
                    prop.vectorValue = new Vector2(min, max);

                EditorGUI.showMixedValue = false;

                EditorGUIUtility.labelWidth = labelWidth;
                MaterialEditor.EndProperty();
            }
            editor.EndAnimatedCheck();
        }
    }
}
