using System;
using System.Collections.Generic;

namespace UnityEngine.Graphing
{
    public static class SerializationHelper
    {
        [Serializable]
        public struct TypeSerializationInfo
        {
            [SerializeField]
            public string fullName;

            [SerializeField]
            public string assemblyName;

            public bool IsValid()
            {
                return !string.IsNullOrEmpty(fullName) && !string.IsNullOrEmpty(assemblyName);
            }

            public string SearchString()
            {
                if (!IsValid())
                    return string.Empty;

                return string.Format("{0}, {1}", fullName, assemblyName);
            }
        }

        [Serializable]
        public struct JSONSerializedElement
        {
            [SerializeField]
            public TypeSerializationInfo typeInfo;

            [SerializeField]
            public string JSONnodeData;
        }

        private static TypeSerializationInfo GetTypeSerializableAsString(Type type)
        {
            return new TypeSerializationInfo
            {
                fullName = type.FullName,
                assemblyName = type.Assembly.GetName().Name
            };
        }

        private static Type GetTypeFromSerializedString(TypeSerializationInfo typeInfo)
        {
            if (!typeInfo.IsValid())
                return null;

            return Type.GetType(typeInfo.SearchString());
        }

        public static List<JSONSerializedElement> Serialize<T>(List<T> list)
        {
            var result = new List<JSONSerializedElement>(list.Count);

            foreach (var element in list)
            {
                if (element == null)
                    continue;

                var typeInfo = GetTypeSerializableAsString(element.GetType());
                var data = JsonUtility.ToJson(element, true);

                if (string.IsNullOrEmpty(data))
                    continue;

                result.Add(new JSONSerializedElement()
                {
                    typeInfo = typeInfo,
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
                if (!element.typeInfo.IsValid() || string.IsNullOrEmpty(element.JSONnodeData))
                    continue;

                var type = GetTypeFromSerializedString(element.typeInfo);
                if (type == null)
                {
                    Debug.LogWarningFormat("Could not find node of type {0} in loaded assemblies", element.typeInfo.SearchString());
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
