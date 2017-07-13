using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    abstract class VFXPropertyIM
    {
        public bool OnGUI(VFXDataAnchorPresenter presenter)
        {
            EditorGUI.BeginChangeCheck();

            if (!presenter.editable)
            {
                GUI.enabled = false;
            }

            object result = DoOnGUI(presenter);


            GUI.enabled = true;

            if (EditorGUI.EndChangeCheck())
            {
                presenter.SetPropertyValue(result);

                return true;
            }
            else
            {
                return false;
            }
        }

        public object OnGUI(string label, object value)
        {
            return DoOnGUI(label, value);
        }

        public virtual bool isNumeric { get { return true; } }

        protected abstract object DoOnGUI(VFXDataAnchorPresenter presenter);
        protected abstract object DoOnGUI(string label, object value);


        public float m_LabelWidth = 100;


        static Dictionary<Type, Type> m_PropertyIMTypes = new Dictionary<Type, Type>
        {
            {typeof(float), typeof(VFXFloatPropertyIM) },
            {typeof(Vector2), typeof(VFXVector2PropertyIM) },
            {typeof(Vector3), typeof(VFXVector3PropertyIM) },
            {typeof(Vector4), typeof(VFXVector4PropertyIM) },
            {typeof(Color), typeof(VFXColorPropertyIM) },
            {typeof(Texture2D), typeof(VFXObjectPropertyIM<Texture2D>) },
            {typeof(Texture3D), typeof(VFXObjectPropertyIM<Texture3D>) },
            {typeof(Mesh), typeof(VFXObjectPropertyIM<Mesh>) },
            {typeof(int), typeof(VFXIntPropertyIM) },
            {typeof(Gradient), typeof(VFXGradientPropertyIM) },
            {typeof(AnimationCurve), typeof(VFXAnimationCurvePropertyIM) }
        };
        public static VFXPropertyIM Create(Type type, float labelWidth)
        {
            Type propertyIMType;

            if (m_PropertyIMTypes.TryGetValue(type, out propertyIMType))
            {
                var property = System.Activator.CreateInstance(propertyIMType) as VFXPropertyIM;
                property.m_LabelWidth = labelWidth;

                return property;
            }
            else
            {
                var property = new VFXDefaultPropertyIM();
                property.m_LabelWidth = labelWidth;
                return property;
            }
        }

        public void Label(VFXDataAnchorPresenter presenter, string label)
        {
            if (presenter != null && presenter.depth > 0)
                GUILayout.Space(presenter.depth * depthOffset);
            GUILayout.BeginVertical();
            GUILayout.Space(3);

            if (presenter != null)
            {
                if (presenter.expandable)
                {
                    bool expanded = presenter.expanded;
                    if (GUILayout.Toggle(presenter.expanded, "", VFXDataGUIStyles.instance.GetGUIStyleForExpandableType(presenter.anchorType), GUILayout.Width(iconSize), GUILayout.Height(iconSize)) != expanded)
                    {
                        if (!expanded)
                        {
                            presenter.ExpandPath();
                        }
                        else
                        {
                            presenter.RetractPath();
                        }

                        // remove the change check to avoid property being regarded as modified
                        EditorGUI.EndChangeCheck();
                        EditorGUI.BeginChangeCheck();
                    }
                }
                else
                {
                    GUILayout.Label("", VFXDataGUIStyles.instance.GetGUIStyleForType(presenter.anchorType), GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                }
            }
            GUILayout.EndVertical();
            GUILayout.Label(label, GUI.skin.label, GUILayout.Width(m_LabelWidth), GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
        }

        public const int iconSize = 16;
        public const float depthOffset = 12;
    }

    abstract class VFXPropertyIM<T> : VFXPropertyIM
    {
        protected override object DoOnGUI(VFXDataAnchorPresenter presenter)
        {
            return OnParameterGUI(presenter, (T)presenter.value, presenter.name);
        }

        protected override object DoOnGUI(string label, object value)
        {
            return OnParameterGUI(null, (T)value, label);
        }

        public abstract T OnParameterGUI(VFXDataAnchorPresenter presenter, T value, string label);
    }


    /*
    abstract class VFXSpacedPropertyIM : VFXPropertyIM
    {
        protected override void DoOnGUI(VFXNodeBlockPresenter presenter, ref VFXNodeBlockPresenter.PropertyInfo infos, VFXPropertyUI.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(ref infos, styles);

            GUILayout.EndHorizontal();
        }

        protected void SpaceControl(ref VFXNodeBlockPresenter.PropertyInfo infos)
        {

        }
    }
    abstract class VFXSpacedPropertyIM<T> : VFXSpacedPropertyIM
    {
        protected override void DoOnGUI(VFXNodeBlockPresenter presenter, ref VFXNodeBlockPresenter.PropertyInfo infos, VFXPropertyUI.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(ref infos, styles);
            infos.value = OnParameterGUI(ref infos, (T)infos.value, styles);
            GUILayout.EndHorizontal();
        }



        public abstract T OnParameterGUI(ref VFXNodeBlockPresenter.PropertyInfo infos, T value, VFXPropertyUI.GUIStyles styles);
    }
    */

    class VFXDefaultPropertyIM : VFXPropertyIM
    {
        public override bool isNumeric { get { return false; } }
        protected override object DoOnGUI(VFXDataAnchorPresenter presenter)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, presenter.name);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            return null;
        }

        protected override object DoOnGUI(string label, object value)
        {
            GUILayout.BeginHorizontal();
            Label(null, label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return value;
        }
    }

    class VFXFloatPropertyIM : VFXPropertyIM<float>
    {
        public override float OnParameterGUI(VFXDataAnchorPresenter presenter, float value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, label);
            value = EditorGUILayout.FloatField(value, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXIntPropertyIM : VFXPropertyIM<int>
    {
        public override int OnParameterGUI(VFXDataAnchorPresenter presenter, int value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, label);
            value = EditorGUILayout.IntField(value, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXVector3PropertyIM : VFXPropertyIM<Vector3>
    {
        public override Vector3 OnParameterGUI(VFXDataAnchorPresenter presenter, Vector3 value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, label);
            GUILayout.Label("x", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.x = EditorGUILayout.FloatField(value.x, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("y", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.y = EditorGUILayout.FloatField(value.y, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("z", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.z = EditorGUILayout.FloatField(value.z, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXVector2PropertyIM : VFXPropertyIM<Vector2>
    {
        public override Vector2 OnParameterGUI(VFXDataAnchorPresenter presenter, Vector2 value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, label);
            GUILayout.Label("x", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.x = EditorGUILayout.FloatField(value.x, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("y", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.y = EditorGUILayout.FloatField(value.y, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXVector4PropertyIM : VFXPropertyIM<Vector4>
    {
        public override Vector4 OnParameterGUI(VFXDataAnchorPresenter presenter, Vector4 value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space((presenter.depth + 1) * depthOffset);
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
    }
    class VFXColorPropertyIM : VFXPropertyIM<Color>
    {
        public override Color OnParameterGUI(VFXDataAnchorPresenter presenter, Color value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, label);
            Color startValue = value;
            Color color = EditorGUILayout.ColorField(new GUIContent(""), value, true, true, true, new ColorPickerHDRConfig(-10, 10, -10, 10));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space((presenter.depth + 1) * depthOffset);
            GUILayout.Label("r", GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.r = EditorGUILayout.FloatField(value.r, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("g",  GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.g = EditorGUILayout.FloatField(value.g, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("b",  GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.b = EditorGUILayout.FloatField(value.b, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.Label("a",  GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            value.a = EditorGUILayout.FloatField(value.a, GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
            GUILayout.EndHorizontal();

            return startValue != value ? value : color;
        }
    }
    class VFXAnimationCurvePropertyIM : VFXPropertyIM<AnimationCurve>
    {
        public override bool isNumeric { get { return false; } }
        public override AnimationCurve OnParameterGUI(VFXDataAnchorPresenter presenter, AnimationCurve value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            value = EditorGUILayout.CurveField(value);
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXGradientPropertyIM : VFXPropertyIM<Gradient>
    {
        public override bool isNumeric { get { return false; } }
        public override Gradient OnParameterGUI(VFXDataAnchorPresenter presenter, Gradient value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, label);
            value = EditorGUILayout.GradientField(value);
            GUILayout.EndHorizontal();
            return value;
        }
    }
    class VFXObjectPropertyIM<T> : VFXPropertyIM<T> where T : Object
    {
        public override bool isNumeric { get { return false; } }
        public override T OnParameterGUI(VFXDataAnchorPresenter presenter, T value, string label)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, label);
            value = (T)EditorGUILayout.ObjectField(value, typeof(T), false);
            GUILayout.EndHorizontal();
            return value;
        }
    }
}
