using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [Serializable]
    public class SerializableType : ISerializationCallbackReceiver
    {
        public static implicit operator SerializableType(Type value)
        {
            return new SerializableType(value);
        }

        public static implicit operator Type(SerializableType value)
        {
            return value.m_Type;
        }

        private SerializableType() {}
        public SerializableType(Type type)
        {
            m_Type = type;
        }

        public virtual void OnBeforeSerialize()
        {
            m_SerializableType = m_Type.AssemblyQualifiedName;
        }

        public virtual void OnAfterDeserialize()
        {
            m_Type = Type.GetType(m_SerializableType);
        }

        [NonSerialized]
        private Type m_Type;
        [SerializeField]
        private string m_SerializableType;
    }

    [Serializable]
    public class VFXSerializableObject
    {
        private VFXSerializableObject() {}

        public VFXSerializableObject(Type type,object obj) : this(type)
        {
            Set(obj);
        }

        public VFXSerializableObject(Type type) 
        {
            m_Type = type;
        }

        public object Get()
        {
            return VFXSerializer.Load(m_Type, m_SerializableObject);
        }

        public object Get<T>()
        {
            return (T)Get();
        }

        public void Set(object obj)
        {
            if (obj == null)
                m_SerializableObject = string.Empty;
            else
            {
                if (!((Type)m_Type).IsAssignableFrom(obj.GetType()))
                    throw new ArgumentException(string.Format("Cannot assing an object of type {0} to VFXSerializedObject of type {1}",obj.GetType(),(Type)m_Type));
                m_SerializableObject = VFXSerializer.Save(obj);
            }
        }

        [SerializeField]
        private SerializableType m_Type;

       // [NonSerialized]
        //private object m_Object;
        [SerializeField]
        private string m_SerializableObject;
    }


    public static class VFXSerializer
    {
        [System.Serializable]
        public struct TypedSerializedData
        {
            public string data;
            public string type;

            public static TypedSerializedData Null = new TypedSerializedData();
        }

        [Serializable]
        private struct ObjectWrapper
        {
            public UnityEngine.Object obj;
        }

        public static TypedSerializedData SaveWithType(object obj)
        {
            TypedSerializedData data = new TypedSerializedData();
            data.data = VFXSerializer.Save(obj);
            data.type = obj.GetType().AssemblyQualifiedName;

            return data;
        }

        public static object LoadWithType(TypedSerializedData data)
        {
            if (!string.IsNullOrEmpty(data.data))
            {
                System.Type type = Type.GetType(data.type);
                if (type == null)
                {
                    Debug.LogError("Can't find type " + data.type);
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
                //var identifier = InspectorFavoritesManager.GetFavoriteIdentifierFromInstanceID((obj as Object).GetInstanceID());
                //return JsonUtility.ToJson(identifier); //TODO use code from favorites

                ObjectWrapper wrapper = new ObjectWrapper { obj = obj as UnityEngine.Object };
                return EditorJsonUtility.ToJson(wrapper);
            }
            else
            {
                return EditorJsonUtility.ToJson(obj);
            }
        }

       /* public static void LoadOverwrite(object dst, string text)
        {

        }*/

        public static object Load(System.Type type,string text)
        {
            if( type.IsPrimitive)
            {
                return Convert.ChangeType(text, type);
            }
            else if( typeof(UnityEngine.Object).IsAssignableFrom(type) )
            {
               /* var identifier = JsonUtility.FromJson<FavoriteIdentifier>(text);

                int instanceID = InspectorFavoritesManager.GetInstanceIDFromFavoriteIdentifier(identifier);

                return EditorUtility.InstanceIDToObject(instanceID);*/
                object obj = new ObjectWrapper();
                EditorJsonUtility.FromJsonOverwrite(text, obj);

                return ((ObjectWrapper)obj).obj;
            }
            else
            {
                object obj = Activator.CreateInstance(type);
                EditorJsonUtility.FromJsonOverwrite(text,obj);
                return obj;
            }
        }
    }
}
