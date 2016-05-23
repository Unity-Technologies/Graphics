using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
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

        public static List<T> Deserialize<T>(List<JSONSerializedElement> list, object[] constructorArgs)
        {
            var result = new List<T>();

            Type[] types = constructorArgs.Select(x => x.GetType()).ToArray();

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
                    var constructorInfo = type.GetConstructor(types);
                    instance = (T) constructorInfo.Invoke(constructorArgs);
                }
                catch
                {
                    Debug.LogWarningFormat("Could not construct instance of: {0} as there is no single argument constuctor that takes a AbstractMaterialGraph", type);
                    continue;
                }
                JsonUtility.FromJsonOverwrite(element.JSONnodeData, instance);
                result.Add(instance);
            }
            return result;
        } 

    }
}
