using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    public static class VFXSerializer
    {
        [System.Serializable]
        public struct TypedSerializedData
        {
            public string data;
            public string type;
            public string assembly;

            public static TypedSerializedData Null = new TypedSerializedData();
        }


        public static TypedSerializedData SaveWithType(object obj)
        {
            TypedSerializedData data = new TypedSerializedData();
            data.data = VFXSerializer.Save(obj);
            data.assembly = obj.GetType().Assembly.FullName;
            data.type = obj.GetType().FullName;

            return data;
        }

        public static object LoadWithType(TypedSerializedData data)
        {
            if (!string.IsNullOrEmpty(data.data))
            {
                //m_Value = SerializationHelper.Deserialize<object>(m_SerializableValue, null);

                var assembly = Assembly.Load(data.assembly);
                if (assembly == null)
                {
                    Debug.LogError("Can't load assembly " + data.assembly + " for type" + data.type);
                    return null;
                }

                System.Type type = assembly.GetType(data.type);
                if (type == null)
                {
                    Debug.LogError("Can't find type " + data.type+ " in assembly" + data.assembly);
                    return null;
                }


                return VFXSerializer.Load(type, data.data);
            }

            return null;
        }


        public static string Save(object obj)
        {
            if( obj.GetType().IsPrimitive)
            {
                return obj.ToString();
            }
            else if(obj is UnityEngine.Object ) //type is a unity object
            {

                var identifier = InspectorFavoritesManager.GetFavoriteIdentifierFromInstanceID((obj as Object).GetInstanceID());


                return JsonUtility.ToJson(identifier); //TODO use code from favorites
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
                var identifier = JsonUtility.FromJson<FavoriteIdentifier>(text);

                int instanceID = InspectorFavoritesManager.GetInstanceIDFromFavoriteIdentifier(identifier);

                return EditorUtility.InstanceIDToObject(instanceID);
            }
            else
            {
                return JsonUtility.FromJson(text,type);
            }
        }
    }
}