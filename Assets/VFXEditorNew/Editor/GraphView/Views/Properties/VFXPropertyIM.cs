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
        public bool OnGUI(VFXBlockDataAnchorPresenter presenter, VFXEditableDataAnchor.GUIStyles styles)
        {
            EditorGUI.BeginChangeCheck();

            if(!presenter.editable)
            {
                GUI.enabled = false;
            }

            object result = DoOnGUI(presenter,styles);

            
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
        protected abstract object DoOnGUI(VFXBlockDataAnchorPresenter presenter, VFXEditableDataAnchor.GUIStyles styles);


        public const float kLabelWidth = 100;



        static Dictionary<Type, Type> m_PropertyIMTypes = new Dictionary<Type, Type>
        {
            {typeof(float),typeof(VFXFloatPropertyIM) },
            {typeof(Vector2),typeof(VFXVector2PropertyIM) },
            {typeof(Vector3),typeof(VFXVector3PropertyIM) },
            {typeof(Vector4),typeof(VFXVector4PropertyIM) },
            {typeof(Color),typeof(VFXColorPropertyIM) },
            {typeof(Texture2D),typeof(VFXObjectPropertyIM<Texture2D>) },
            {typeof(Texture3D),typeof(VFXObjectPropertyIM<Texture3D>) },
            {typeof(Mesh),typeof(VFXObjectPropertyIM<Mesh>) },
            {typeof(int),typeof(VFXIntPropertyIM) },
            {typeof(Gradient),typeof(VFXGradientPropertyIM) },
            {typeof(AnimationCurve),typeof(VFXAnimationCurvePropertyIM) }
        };
        public static VFXPropertyIM Create(Type type)
        {
            Type propertyIMType;

            if(m_PropertyIMTypes.TryGetValue(type,out propertyIMType))
            {
                return System.Activator.CreateInstance(propertyIMType) as VFXPropertyIM;
            }
            else
            {
                return new VFXDefaultPropertyIM();
            }
        }



        public void Label(VFXBlockDataAnchorPresenter presenter, VFXEditableDataAnchor.GUIStyles styles)
        {

            if (presenter.depth > 0)
                GUILayout.Space(presenter.depth * depthOffset);
            GUILayout.BeginVertical();
            GUILayout.Space(3);

            bool expanded = presenter.expanded;
            if (presenter.expandable)
            {
                if (GUILayout.Toggle(presenter.expanded, "", styles.GetGUIStyleForExpandableType(presenter.anchorType), GUILayout.Width(iconSize), GUILayout.Height(iconSize)) != expanded)
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
                GUILayout.Label("", styles.GetGUIStyleForType(presenter.anchorType), GUILayout.Width(iconSize), GUILayout.Height(iconSize));
            }
            GUILayout.EndVertical();
            GUILayout.Label(presenter.name, styles.baseStyle, GUILayout.Width(kLabelWidth), GUILayout.Height(styles.lineHeight));
        }

        public const int iconSize = 14;
        public const float depthOffset = 12;
    }

    abstract class VFXPropertyIM<T> : VFXPropertyIM
    {
        protected override object DoOnGUI(VFXBlockDataAnchorPresenter presenter, VFXEditableDataAnchor.GUIStyles styles)
        {
            return OnParameterGUI(presenter, (T)presenter.value, styles);

        }



        public abstract T OnParameterGUI(VFXBlockDataAnchorPresenter presenter, T value, VFXEditableDataAnchor.GUIStyles styles);
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
        protected override object DoOnGUI(VFXBlockDataAnchorPresenter presenter, VFXEditableDataAnchor.GUIStyles styles)
        {

            GUILayout.BeginHorizontal();
            Label(presenter, styles);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            return null;
        }

    }


    class VFXFloatPropertyIM : VFXPropertyIM<float>
    {
        public override float OnParameterGUI(VFXBlockDataAnchorPresenter presenter, float value, VFXEditableDataAnchor.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, styles);
            value = EditorGUILayout.FloatField(value, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXIntPropertyIM : VFXPropertyIM<int>
    {
        public override int OnParameterGUI(VFXBlockDataAnchorPresenter presenter, int value, VFXEditableDataAnchor.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, styles);
            value = EditorGUILayout.IntField(value, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXVector3PropertyIM : VFXPropertyIM<Vector3>
    {
        public override Vector3 OnParameterGUI(VFXBlockDataAnchorPresenter presenter, Vector3 value, VFXEditableDataAnchor.GUIStyles styles)
        {

            GUILayout.BeginHorizontal();
            Label(presenter, styles);
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
        public override Vector2 OnParameterGUI(VFXBlockDataAnchorPresenter presenter, Vector2 value, VFXEditableDataAnchor.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, styles);
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
        public override Vector4 OnParameterGUI(VFXBlockDataAnchorPresenter presenter, Vector4 value, VFXEditableDataAnchor.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, styles);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space((presenter.depth+1) * depthOffset);
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
        public override Color OnParameterGUI(VFXBlockDataAnchorPresenter presenter, Color value, VFXEditableDataAnchor.GUIStyles styles)
        {
            Color startValue = value;
            GUILayout.BeginHorizontal();
            Label(presenter, styles);
            Color color = EditorGUILayout.ColorField(new GUIContent(""), value, true, true, true, new ColorPickerHDRConfig(-10, 10, -10, 10));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space((presenter.depth + 1) * depthOffset);
            GUILayout.Label("r", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.r = EditorGUILayout.FloatField(value.r, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.Label("g", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.g = EditorGUILayout.FloatField(value.g, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.Label("b", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.b = EditorGUILayout.FloatField(value.b, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.Label("a", styles.baseStyle, GUILayout.Height(styles.lineHeight));
            value.a = EditorGUILayout.FloatField(value.a, styles.baseStyle, GUILayout.Height(styles.lineHeight));
            GUILayout.EndHorizontal();

            return startValue != value ? value : color;
        }
    }
    class VFXAnimationCurvePropertyIM : VFXPropertyIM<AnimationCurve>
    {
        public override AnimationCurve OnParameterGUI(VFXBlockDataAnchorPresenter presenter, AnimationCurve value, VFXEditableDataAnchor.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, styles);
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
        public override Gradient OnParameterGUI(VFXBlockDataAnchorPresenter presenter, Gradient value, VFXEditableDataAnchor.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, styles);
            value = EditorGUILayout.GradientField(value);
            GUILayout.EndHorizontal();
            return value;
        }
    }
    class VFXObjectPropertyIM<T> : VFXPropertyIM<T> where T : Object
    {
        public override T OnParameterGUI(VFXBlockDataAnchorPresenter presenter, T value, VFXEditableDataAnchor.GUIStyles styles)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, styles);
            value = (T)EditorGUILayout.ObjectField(value,typeof(T),false);
            GUILayout.EndHorizontal();
            return value;
        }
    }

}