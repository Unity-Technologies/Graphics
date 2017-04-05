using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = System.Object;

namespace UnityEditor.VFX
{
    class VFXSerializer
    {

        public static string Save(object obj)
        {
            if( obj.GetType().IsPrimitive)
            {
                return obj.ToString();
            }
            else if(obj is UnityEngine.Object ) //type is a unity object
            {
                return ""; //TODO use code from favorites
            }
            else
            {

                /*if( ! obj.GetType().GetCustomAttributes(false).Any(t=>t is SerializableAttribute) )
                {
                    Debug.LogError("using non serializable and not UnityEngine.Object class or struct in VFXSerialized: "+obj.GetType().FullName );
                }*/
                return JsonUtility.ToJson(obj);
            }
        }

        public static object Load(System.Type type,string text)
        {
            if( type.IsPrimitive)
            {
                return Convert.ChangeType(text, type);
            }
            else if( typeof(UnityEngine.Object).IsAssignableFrom(type) )
            {
                return null;
            }
            else
            {
                return JsonUtility.FromJson(text,type);
            }
        }
    }
}