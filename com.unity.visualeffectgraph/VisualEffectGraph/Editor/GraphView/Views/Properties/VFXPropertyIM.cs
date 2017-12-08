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

        public object OnGUI(Rect rect, string label, object value)
        {
            return DoOnGUI(rect, label, value);
        }

        public virtual bool isNumeric { get { return true; } }

        protected abstract object DoOnGUI(VFXDataAnchorPresenter presenter);
        protected abstract object DoOnGUI(Rect rect, string label, object value);


        public float m_LabelWidth = 100;


        static Dictionary<Type, Type> m_PropertyIMTypes = new Dictionary<Type, Type>
        {
            {typeof(float), typeof(VFXFloatPropertyIM) },
            {typeof(Vector2), typeof(VFXVector2PropertyIM) },
            {typeof(Vector3), typeof(VFXVector3PropertyIM) },
            {typeof(Vector4), typeof(VFXVector4PropertyIM) },
            //{typeof(Color), typeof(VFXColorPropertyIM) },
            {typeof(Texture2D), typeof(VFXObjectPropertyIM<Texture2D>) },
            {typeof(Texture3D), typeof(VFXObjectPropertyIM<Texture3D>) },
            {typeof(Mesh), typeof(VFXObjectPropertyIM<Mesh>) },
            {typeof(int), typeof(VFXIntPropertyIM) },
            {typeof(uint), typeof(VFXUIntPropertyIM) },
            {typeof(bool), typeof(VFXBoolPropertyIM) },
            {typeof(Gradient), typeof(VFXGradientPropertyIM) },
            {typeof(AnimationCurve), typeof(VFXAnimationCurvePropertyIM) },
            {typeof(Position), typeof(VFXPositionPropertyIM) },
            {typeof(DirectionType), typeof(VFXDirectionPropertyIM) },
            {typeof(Vector), typeof(VFXVectorPropertyIM) }
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
                    bool expanded = presenter.expandedSelf;
                    if (GUILayout.Toggle(presenter.expandedSelf, "", VFXDataGUIStyles.instance.GetGUIStyleForExpandableType(presenter.portType), GUILayout.Width(iconSize), GUILayout.Height(iconSize)) != expanded)
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
                    GUILayout.Label("", VFXDataGUIStyles.instance.GetGUIStyleForType(presenter.portType), GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                }
            }
            GUILayout.EndVertical();
            GUILayout.Label(label, GUI.skin.label, GUILayout.Width(m_LabelWidth), GUILayout.Height(VFXDataGUIStyles.instance.lineHeight));
        }

        public void Label(Rect rect, string label)
        {
            rect.width = m_LabelWidth;
            GUI.Label(rect, label, GUI.skin.label);
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

        protected override object DoOnGUI(Rect rect, string label, object value)
        {
            return OnParameterGUI(rect, (T)value, label);
        }

        public abstract T OnParameterGUI(VFXDataAnchorPresenter presenter, T value, string label);
        public abstract T OnParameterGUI(Rect rect, T value, string label);
    }

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

        protected override object DoOnGUI(Rect rect, string label, object value)
        {
            GUILayout.BeginHorizontal();
            Label(rect, label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return value;
        }
    }
}
