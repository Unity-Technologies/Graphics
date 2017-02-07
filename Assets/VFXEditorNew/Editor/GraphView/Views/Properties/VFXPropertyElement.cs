using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    abstract class VFXPropertyElement
    {
        public abstract void OnGUI(VFXNodeBlockPresenter block, int index);



        public static VFXPropertyElement Create(VFXNodeBlockPresenter block, int index)
        {
            FieldInfo field = block.GetPropertiesType().GetFields()[index];


            if( field.FieldType == typeof(float))
            {
                return new VFXFloatPropertyElement();
            }
            else if( field.FieldType == typeof(Vector3))
            {
                return new VFXVector3PropertyElement();
            }
            else
            {
                return new VFXDefaultPropertyElement();
            }
        }
    }

    abstract class VFXPropertyElement<T> : VFXPropertyElement
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



    class VFXDefaultPropertyElement : VFXPropertyElement
    {
        public override void OnGUI(VFXNodeBlockPresenter block, int index)
        {
            FieldInfo field = block.GetPropertiesType().GetFields()[index];

            EditorGUILayout.LabelField(field.Name, "");
        }

    }


    class VFXFloatPropertyElement : VFXPropertyElement<float>
    {
        public override float OnParameterGUI(string name,float value)
        {
            return EditorGUILayout.FloatField(name, value);
        }
    }
    class VFXVector3PropertyElement : VFXPropertyElement<Vector3>
    {
        public override Vector3 OnParameterGUI(string name, Vector3 value)
        {
            return EditorGUILayout.Vector3Field(name, value);
        }
    }

}