using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    abstract class VFXPropertyIM
    {
        public abstract void OnGUI(VFXNodeBlockPresenter block, int index);



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
        public override void OnGUI(VFXNodeBlockPresenter block, int index)
        {
            FieldInfo field = block.GetPropertiesType().GetFields()[index];
            T obj = (T)field.GetValue(block.GetCurrentProperties());

            obj = OnParameterGUI(field.Name,obj);

            field.SetValue(block.GetCurrentProperties(), obj);
        }



        public abstract T OnParameterGUI(string name, T value);
    }



    class VFXDefaultPropertyIM : VFXPropertyIM
    {
        public override void OnGUI(VFXNodeBlockPresenter block, int index)
        {
            FieldInfo field = block.GetPropertiesType().GetFields()[index];

            EditorGUILayout.LabelField(field.Name, "");
        }

    }


    class VFXFloatPropertyIM : VFXPropertyIM<float>
    {
        public override float OnParameterGUI(string name,float value)
        {
            return EditorGUILayout.FloatField(name, value);
        }
    }
    class VFXVector3PropertyIM : VFXPropertyIM<Vector3>
    {
        public override Vector3 OnParameterGUI(string name, Vector3 value)
        {
            return EditorGUILayout.Vector3Field(name, value);
        }
    }
    class VFXVector2PropertyIM : VFXPropertyIM<Vector2>
    {
        public override Vector2 OnParameterGUI(string name, Vector2 value)
        {
            return EditorGUILayout.Vector2Field(name, value);
        }
    }
    class VFXVector4PropertyIM : VFXPropertyIM<Vector4>
    {
        public override Vector4 OnParameterGUI(string name, Vector4 value)
        {
            return EditorGUILayout.Vector4Field(name, value);
        }
    }
    class VFXColorPropertyIM : VFXPropertyIM<Color>
    {
        public override Color OnParameterGUI(string name, Color value)
        {
            return EditorGUILayout.ColorField(name, value);
        }
    }
    class VFXObjectPropertyIM<T> : VFXPropertyIM<T> where T : Object
    {
        public override T OnParameterGUI(string name, T value)
        {
            return (T)EditorGUILayout.ObjectField(name, value,typeof(T),false);
        }
    }

}