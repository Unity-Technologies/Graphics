using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Graphing
{
    public static class SerializationHelper
    {
        [Serializable]
        public struct JSONSerializedElement
        {
            [SerializeField]
            public string typeName;

            [SerializeField]
            public string JSONnodeData;
        }

        private static string GetTypeSerializableAsString(Type type)
        {
            if (type == null)
                return string.Empty;

            return string.Format("{0}, {1}", type.FullName, type.Assembly.GetName().Name);
        }

        private static Type GetTypeFromSerializedString(string type)
        {
            if (string.IsNullOrEmpty(type))
                return null;

            return Type.GetType(type);
        }

        public static List<JSONSerializedElement> Serialize<T>(List<T> list)
        {
            var result = new List<JSONSerializedElement>(list.Count);

            foreach (var element in list)
            {
                if (element == null)
                    continue;

                var typeName = GetTypeSerializableAsString(element.GetType());
                var data = JsonUtility.ToJson(element, true);

                if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(data))
                    continue;

                result.Add(new JSONSerializedElement()
                {
                    typeName = typeName,
                    JSONnodeData = data
                });
            }
            return result;
        }

        public static List<T> Deserialize<T>(List<JSONSerializedElement> list, object[] constructorArgs) where T : class 
        {
            var result = new List<T>();

            foreach (var element in list) 
            {
                if (string.IsNullOrEmpty(element.typeName) || string.IsNullOrEmpty(element.JSONnodeData))
                    continue;

                var type = GetTypeFromSerializedString(element.typeName);
                if (type == null)
                {
                    Debug.LogWarningFormat("Could not find node of type {0} in loaded assemblies", element.typeName);
                    continue;
                }

                T instance;
                try
                { 
                    instance = Activator.CreateInstance(type, constructorArgs) as T;
                }
                catch (Exception e)
                {
                    Debug.LogWarningFormat("Could not construct instance of: {0} - {1}", type, e);
                    continue;
                }

                if (instance != null)
                {
                    JsonUtility.FromJsonOverwrite(element.JSONnodeData, instance);
                    result.Add(instance);
                }
            }
            return result;  
        } 
    }
}
