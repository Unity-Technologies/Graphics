using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXFloatPropertyIM : VFXPropertyIM<float>
    {
        public override float OnParameterGUI(VFXDataAnchorController controller, float value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(controller, label);
            value = EditorGUILayout.FloatField(value, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }

        public override float OnParameterGUI(Rect rect, float value, string label)
        {
            Label(rect, label);

            rect.xMin += m_LabelWidth;
            value = EditorGUI.FloatField(rect, value);

            return value;
        }
    }
    class VFXIntPropertyIM : VFXPropertyIM<int>
    {
        public override int OnParameterGUI(VFXDataAnchorController controller, int value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(controller, label);
            value = EditorGUILayout.IntField(value, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }

        public override int OnParameterGUI(Rect rect, int value, string label)
        {
            Label(rect, label);

            rect.xMin += m_LabelWidth;
            value = EditorGUI.IntField(rect, value);

            return value;
        }
    }
    class VFXUIntPropertyIM : VFXPropertyIM<uint>
    {
        public override uint OnParameterGUI(VFXDataAnchorController controller, uint value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(controller, label);
            value = (uint)EditorGUILayout.IntField((int)value, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }

        public override uint OnParameterGUI(Rect rect, uint value, string label)
        {
            Label(rect, label);

            rect.xMin += m_LabelWidth;
            value = (uint)EditorGUI.IntField(rect, (int)value);

            return value;
        }
    }

    class VFXBoolPropertyIM : VFXPropertyIM<bool>
    {
        public override bool OnParameterGUI(VFXDataAnchorController controller, bool value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(controller, label);
            value = EditorGUILayout.Toggle(value, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }

        public override bool OnParameterGUI(Rect rect, bool value, string label)
        {
            Label(rect, label);

            rect.xMin += m_LabelWidth;
            value = EditorGUI.Toggle(rect, value);

            return value;
        }
    }
    class VFXVector3PropertyIM : VFXPropertyIM<Vector3>
    {
        public override Vector3 OnParameterGUI(VFXDataAnchorController controller, Vector3 value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(controller, label);
            GUILayout.Label("x", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.x = EditorGUILayout.FloatField(value.x, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("y", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.y = EditorGUILayout.FloatField(value.y, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("z", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.z = EditorGUILayout.FloatField(value.z, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }

        public override Vector3 OnParameterGUI(Rect rect, Vector3 value, string label)
        {
            Label(rect, label);

            rect.xMin += m_LabelWidth;

            float paramWidth = Mathf.Floor(rect.width / 3);
            float labelWidth = 20;

            rect.width = labelWidth;
            GUI.Label(rect, "x");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.x = EditorGUI.FloatField(rect, value.x);

            rect.xMin += rect.width;
            rect.width = labelWidth;
            GUI.Label(rect, "y");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.y = EditorGUI.FloatField(rect, value.y);

            rect.xMin += rect.width;
            rect.width = labelWidth;
            GUI.Label(rect, "z");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.y = EditorGUI.FloatField(rect, value.z);

            return value;
        }
    }
    class VFXVector2PropertyIM : VFXPropertyIM<Vector2>
    {
        public override Vector2 OnParameterGUI(VFXDataAnchorController controller, Vector2 value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(controller, label);
            GUILayout.Label("x", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.x = EditorGUILayout.FloatField(value.x, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("y", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.y = EditorGUILayout.FloatField(value.y, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }

        public override Vector2 OnParameterGUI(Rect rect, Vector2 value, string label)
        {
            Label(rect, label);

            rect.xMin += m_LabelWidth;

            float paramWidth = Mathf.Floor(rect.width / 2);
            float labelWidth = 20;

            rect.width = labelWidth;
            GUI.Label(rect, "x");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.x = EditorGUI.FloatField(rect, value.x);

            rect.xMin += rect.width;
            rect.width = labelWidth;
            GUI.Label(rect, "y");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.y = EditorGUI.FloatField(rect, value.y);

            return value;
        }
    }
    class VFXVector4PropertyIM : VFXPropertyIM<Vector4>
    {
        public override Vector4 OnParameterGUI(VFXDataAnchorController controller, Vector4 value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(controller, label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space((controller.depth + 1) * depthOffset);
            GUILayout.Label("x", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.x = EditorGUILayout.FloatField(value.x, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("y", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.y = EditorGUILayout.FloatField(value.y, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("z", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.z = EditorGUILayout.FloatField(value.z, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("w", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.w = EditorGUILayout.FloatField(value.w, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }

        public override Vector4 OnParameterGUI(Rect rect, Vector4 value, string label)
        {
            Label(rect, label);

            rect.xMin += m_LabelWidth;

            float paramWidth = Mathf.Floor(rect.width / 4);
            float labelWidth = 20;

            rect.width = labelWidth;
            GUI.Label(rect, "x");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.x = EditorGUI.FloatField(rect, value.x);

            rect.xMin += rect.width;
            rect.width = labelWidth;
            GUI.Label(rect, "y");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.y = EditorGUI.FloatField(rect, value.y);

            rect.xMin += rect.width;
            rect.width = labelWidth;
            GUI.Label(rect, "z");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.y = EditorGUI.FloatField(rect, value.z);

            rect.xMin += rect.width;
            rect.width = labelWidth;
            GUI.Label(rect, "w");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.y = EditorGUI.FloatField(rect, value.w);

            return value;
        }
    }
    /*
    class VFXColorPropertyIM : VFXPropertyIM<Color>
    {
        public override Color OnParameterGUI(VFXDataAnchorPresenter controller, Color value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(controller, label);
            Color startValue = value;
            Color color = EditorGUILayout.ColorField(new GUIContent(""), value, true, true, true, new ColorPickerHDRConfig(-10, 10, -10, 10));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space((controller.depth + 1) * depthOffset);
            GUILayout.Label("r", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.r = EditorGUILayout.FloatField(value.r, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("g", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.g = EditorGUILayout.FloatField(value.g, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("b", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.b = EditorGUILayout.FloatField(value.b, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("a", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.a = EditorGUILayout.FloatField(value.a, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return startValue != value ? value : color;
        }

        public override Color OnParameterGUI(Rect rect, Color value, string label)
        {
            Label(rect, label);
            rect.xMin += m_LabelWidth;

            Color color = EditorGUI.ColorField(rect, new GUIContent(""), value, true, true, true, new ColorPickerHDRConfig(-10, 10, -10, 10));


            return color;
        }
    }*/
    class VFXAnimationCurvePropertyIM : VFXPropertyIM<AnimationCurve>
    {
        public override bool isNumeric { get { return false; } }
        public override AnimationCurve OnParameterGUI(VFXDataAnchorController controller, AnimationCurve value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(controller, label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            value = EditorGUILayout.CurveField(value);
            GUILayout.EndHorizontal();

            return value;
        }

        public override AnimationCurve OnParameterGUI(Rect rect, AnimationCurve value, string label)
        {
            Label(rect, label);
            rect.xMin += m_LabelWidth;

            value = EditorGUI.CurveField(rect, value);

            return value;
        }
    }
    class VFXGradientPropertyIM : VFXPropertyIM<Gradient>
    {
        public override bool isNumeric { get { return false; } }
        public override Gradient OnParameterGUI(VFXDataAnchorController controller, Gradient value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(controller, label);
            value = EditorGUILayout.GradientField(value);
            GUILayout.EndHorizontal();
            return value;
        }

        public override Gradient OnParameterGUI(Rect rect, Gradient value, string label)
        {
            Label(rect, label);
            rect.xMin += m_LabelWidth;
            value = EditorGUI.GradientField(rect, value);
            return value;
        }
    }
    class VFXObjectPropertyIM<T> : VFXPropertyIM<T> where T : Object
    {
        public override bool isNumeric { get { return false; } }
        public override T OnParameterGUI(VFXDataAnchorController controller, T value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(controller, label);
            value = (T)EditorGUILayout.ObjectField(value, typeof(T), false);
            GUILayout.EndHorizontal();
            return value;
        }

        public override T OnParameterGUI(Rect rect, T value, string label)
        {
            Label(rect, label);
            rect.xMin += m_LabelWidth;
            value = (T)EditorGUI.ObjectField(rect, value, typeof(T), false);
            return value;
        }
    }
}
