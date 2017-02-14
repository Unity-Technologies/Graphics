using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.RMGUI;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    abstract class VFXPropertyIM
    {
        public bool OnGUI(VFXNodeBlockPresenter presenter, ref VFXNodeBlockPresenter.PropertyInfo infos, VFXPropertyUI.GUIStyles styles)
        {
            EditorGUI.BeginChangeCheck();

            bool savedExpanded = infos.expanded;

            DoOnGUI(presenter, ref infos,styles);

            if (EditorGUI.EndChangeCheck())
            {
                if (savedExpanded != infos.expanded)
                    if (infos.expanded)
                    {
                        presenter.ExpandPath(infos.path);
                    }
                    else
                    {
                        presenter.RetractPath(infos.path);
                    }
                else
                {
                    presenter.PropertyValueChanged(ref infos);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        protected abstract void DoOnGUI(VFXNodeBlockPresenter presenter, ref VFXNodeBlockPresenter.PropertyInfo infos, VFXPropertyUI.GUIStyles styles);


        public const float kLabelWidth = 70;
        public static VFXPropertyIM Create(Type type)
        {
            if(type == typeof(float))
            {
                return new VFXFloatPropertyIM();
            }
            else if (type == typeof(Vector2))
            {
                return new VFXVector2PropertyIM();
            }
            else if (type == typeof(Vector3))
            {
                return new VFXVector3PropertyIM();
            }
            else if (type == typeof(Vector4))
            {
                return new VFXVector4PropertyIM();
            }
            else if (type == typeof(Color))
            {
                return new VFXColorPropertyIM();
            }
            else if (type == typeof(Texture2D))
            {
                return new VFXObjectPropertyIM<Texture2D>();
            }
            else if (type == typeof(Texture3D))
            {
                return new VFXObjectPropertyIM<Texture3D>();
            }
            else
            {
                return new VFXDefaultPropertyIM();
            }
        }



        public void Label(ref VFXNodeBlockPresenter.PropertyInfo infos, VFXPropertyUI.GUIStyles styles)
        {
            if (infos.depth > 0)
                GUILayout.Space(infos.depth * depthOffset);
            GUILayout.BeginVertical();
            GUILayout.Space(3);
            if (infos.expandable)
                infos.expanded = GUILayout.Toggle(infos.expanded, "", styles.GetGUIStyleForExpandableType(infos.type), GUILayout.Width(iconSize), GUILayout.Height(iconSize));
            else
                GUILayout.Label("", styles.GetGUIStyleForType(infos.type), GUILayout.Width(iconSize), GUILayout.Height(iconSize));
            GUILayout.EndVertical();
            GUILayout.Label(infos.name, styles.baseStyle, GUILayout.Width(kLabelWidth), GUILayout.Height(styles.lineHeight));
        }

        public const int iconSize = 14;
        public const float depthOffset = 20;
    }

    abstract class VFXPropertyIM<T> : VFXPropertyIM
    {
        protected override void DoOnGUI(VFXNodeBlockPresenter presenter, ref VFXNodeBlockPresenter.PropertyInfo infos, VFXPropertyUI.GUIStyles styles)
        {

            infos.value = OnParameterGUI(ref infos, (T)infos.value, styles);

        }



        public abstract T OnParameterGUI(ref VFXNodeBlockPresenter.PropertyInfo infos, T value, VFXPropertyUI.GUIStyles styles);
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
        protected override void DoOnGUI(VFXNodeBlockPresenter presenter, ref VFXNodeBlockPresenter.PropertyInfo infos, VFXPropertyUI.GUIStyles styles)
        {

            GUILayout.BeginHorizontal();
            Label(ref infos,styles);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

    }


    class VFXFloatPropertyIM : VFXPropertyIM<float>
    {
        public override float OnParameterGUI(ref VFXNodeBlockPresenter.PropertyInfo infos, float value, VFXPropertyUI.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(ref infos,styles);
            value = EditorGUILayout.FloatField(value, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXVector3PropertyIM : VFXPropertyIM<Vector3>
    {
        public override Vector3 OnParameterGUI(ref VFXNodeBlockPresenter.PropertyInfo infos, Vector3 value, VFXPropertyUI.GUIStyles styles)
        {

            GUILayout.BeginHorizontal();
            Label(ref infos,styles);
            GUILayout.Label("x", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.x = EditorGUILayout.FloatField(value.x, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.Label("y", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.y = EditorGUILayout.FloatField(value.y, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.Label("z", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.z = EditorGUILayout.FloatField(value.z, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXVector2PropertyIM : VFXPropertyIM<Vector2>
    {
        public override Vector2 OnParameterGUI(ref VFXNodeBlockPresenter.PropertyInfo infos, Vector2 value, VFXPropertyUI.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(ref infos,styles);
            GUILayout.Label("x", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.x = EditorGUILayout.FloatField(value.x, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.Label("y", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.y = EditorGUILayout.FloatField(value.y, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXVector4PropertyIM : VFXPropertyIM<Vector4>
    {
        public override Vector4 OnParameterGUI(ref VFXNodeBlockPresenter.PropertyInfo infos, Vector4 value, VFXPropertyUI.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(ref infos,styles);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space((infos.depth+1) * depthOffset);
            GUILayout.Label("x", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.x = EditorGUILayout.FloatField(value.x, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.Label("y", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.y = EditorGUILayout.FloatField(value.y, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.Label("z", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.z = EditorGUILayout.FloatField(value.z, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.Label("w", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.w = EditorGUILayout.FloatField(value.w, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXColorPropertyIM : VFXPropertyIM<Color>
    {
        public override Color OnParameterGUI(ref VFXNodeBlockPresenter.PropertyInfo infos, Color value, VFXPropertyUI.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(ref infos,styles);
            EditorGUILayout.ColorField(value);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space((infos.depth + 1) * depthOffset);
            GUILayout.Label("r", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.r = EditorGUILayout.FloatField(value.r, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.Label("g", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.g = EditorGUILayout.FloatField(value.g, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.Label("b", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.b = EditorGUILayout.FloatField(value.b, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.Label("a", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.a = EditorGUILayout.FloatField(value.a, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXObjectPropertyIM<T> : VFXPropertyIM<T> where T : Object
    {
        public override T OnParameterGUI(ref VFXNodeBlockPresenter.PropertyInfo infos, T value, VFXPropertyUI.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(ref infos, styles);
            value = (T)EditorGUILayout.ObjectField(value,typeof(T),false);
            GUILayout.EndHorizontal();
            return value;
        }
    }

}