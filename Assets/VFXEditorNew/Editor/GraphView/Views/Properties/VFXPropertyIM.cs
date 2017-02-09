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
        public abstract void OnGUI(VFXNodeBlockPresenter block, int index, GUIStyle style);


        public const float kLabelWidth = 70;
        public static VFXPropertyIM Create(VFXNodeBlockPresenter block, int index)
        {
            FieldInfo field = block.GetPropertiesType().GetFields()[index];


            if( field.FieldType == typeof(float))
            {
                return new VFXFloatPropertyIM();
            }
            else if (field.FieldType == typeof(Vector2))
            {
                return new VFXVector2PropertyIM();
            }
            else if (field.FieldType == typeof(Vector3))
            {
                return new VFXVector3PropertyIM();
            }
            else if (field.FieldType == typeof(Vector4))
            {
                return new VFXVector4PropertyIM();
            }
            else if (field.FieldType == typeof(Color))
            {
                return new VFXColorPropertyIM();
            }
            else if (field.FieldType == typeof(Texture2D))
            {
                return new VFXObjectPropertyIM<Texture2D>();
            }
            else if (field.FieldType == typeof(Texture3D))
            {
                return new VFXObjectPropertyIM<Texture3D>();
            }
            else
            {
                return new VFXDefaultPropertyIM();
            }
        }
    }

    abstract class VFXPropertyIM<T> : VFXPropertyIM
    {
        public override void OnGUI(VFXNodeBlockPresenter block, int index,GUIStyle style)
        {
            FieldInfo field = block.GetPropertiesType().GetFields()[index];
            T obj = (T)field.GetValue(block.GetCurrentProperties());

            obj = OnParameterGUI(field.Name,obj, style);

            field.SetValue(block.GetCurrentProperties(), obj);
        }



        public abstract T OnParameterGUI(string name, T value, GUIStyle style);
    }



    class VFXDefaultPropertyIM : VFXPropertyIM
    {
        public override void OnGUI(VFXNodeBlockPresenter block, int index, GUIStyle style)
        {
            FieldInfo field = block.GetPropertiesType().GetFields()[index];

            EditorGUILayout.LabelField(field.Name, "");
        }

    }


    class VFXFloatPropertyIM : VFXPropertyIM<float>
    {
        public override float OnParameterGUI(string name,float value, GUIStyle style)
        {


            GUILayout.BeginHorizontal();
            GUILayout.Label(name, style,GUILayout.Width(kLabelWidth), GUILayout.Height(style.fontSize * 1.25f));
            return EditorGUILayout.FloatField(value,style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.EndHorizontal();
        }
    }
    class VFXVector3PropertyIM : VFXPropertyIM<Vector3>
    {
        public override Vector3 OnParameterGUI(string name, Vector3 value, GUIStyle style)
        {

            GUILayout.BeginHorizontal();
            GUILayout.Label(name, style, GUILayout.Width(kLabelWidth), GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.Label("x", style, GUILayout.Height(style.fontSize * 1.25f));
            value.x = EditorGUILayout.FloatField(value.x, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.Label("y", style, GUILayout.Height(style.fontSize * 1.25f));
            value.y = EditorGUILayout.FloatField(value.y, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.Label("z", style, GUILayout.Height(style.fontSize * 1.25f));
            value.z = EditorGUILayout.FloatField(value.z, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXVector2PropertyIM : VFXPropertyIM<Vector2>
    {
        public override Vector2 OnParameterGUI(string name, Vector2 value, GUIStyle style)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, style, GUILayout.Width(kLabelWidth), GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.Label("x", style, GUILayout.Height(style.fontSize * 1.25f));
            value.x = EditorGUILayout.FloatField(value.x, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.Label("y", style, GUILayout.Height(style.fontSize * 1.25f));
            value.y = EditorGUILayout.FloatField(value.y, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXVector4PropertyIM : VFXPropertyIM<Vector4>
    {
        public override Vector4 OnParameterGUI(string name, Vector4 value, GUIStyle style)
        {
            GUILayout.Label(name, style, GUILayout.Width(kLabelWidth), GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("x", style, GUILayout.Height(style.fontSize * 1.25f));
            value.x = EditorGUILayout.FloatField(value.x, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.Label("y", style, GUILayout.Height(style.fontSize * 1.25f));
            value.y = EditorGUILayout.FloatField(value.y, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.Label("z", style, GUILayout.Height(style.fontSize * 1.25f));
            value.z = EditorGUILayout.FloatField(value.z, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.Label("w", style, GUILayout.Height(style.fontSize * 1.25f));
            value.w = EditorGUILayout.FloatField(value.w, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXColorPropertyIM : VFXPropertyIM<Color>
    {
        public override Color OnParameterGUI(string name, Color value, GUIStyle style)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, style, GUILayout.Width(kLabelWidth), GUILayout.Height(style.fontSize * 1.25f));
            EditorGUILayout.ColorField(value);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("r", style, GUILayout.Height(style.fontSize * 1.25f));
            value.r = EditorGUILayout.FloatField(value.r, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.Label("g", style, GUILayout.Height(style.fontSize * 1.25f));
            value.g = EditorGUILayout.FloatField(value.g, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.Label("b", style, GUILayout.Height(style.fontSize * 1.25f));
            value.b = EditorGUILayout.FloatField(value.b, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.Label("a", style, GUILayout.Height(style.fontSize * 1.25f));
            value.a = EditorGUILayout.FloatField(value.a, style, GUILayout.Height(style.fontSize * 1.25f));
            GUILayout.EndHorizontal();

            return value;
        }
    }
    class VFXObjectPropertyIM<T> : VFXPropertyIM<T> where T : Object
    {
        public override T OnParameterGUI(string name, T value, GUIStyle style)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, style, GUILayout.Width(kLabelWidth), GUILayout.Height(style.fontSize * 1.25f));
            return (T)EditorGUILayout.ObjectField(value,typeof(T),false);
            GUILayout.EndHorizontal();
        }
    }

}