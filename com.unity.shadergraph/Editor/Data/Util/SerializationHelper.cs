using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.Graphing
{
    static class SerializationHelper
    {
        [Serializable]
        public struct TypeSerializationInfo
        {
            [SerializeField]
            public string fullName;

            public bool IsValid()
            {
                return !string.IsNullOrEmpty(fullName);
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

        public static JSONSerializedElement nullElement
        {
            get
            {
                return new JSONSerializedElement();
            }
        }

        public static TypeSerializationInfo GetTypeSerializableAsString(Type type)
        {
            return new TypeSerializationInfo
            {
                fullName = type.FullName
            };
        }

        static Type GetTypeFromSerializedString(TypeSerializationInfo typeInfo)
        {
            if (!typeInfo.IsValid())
                return null;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType(typeInfo.fullName);
                if (type != null)
                    return type;
            }

            return null;
        }

        public static JSONSerializedElement Serialize<T>(T item)
        {
            if(item is JsonObject jsonObject)
                return new JSONSerializedElement() { JSONnodeData = jsonObject.Serialize() };

            if (item == null)
                throw new ArgumentNullException("item", "Can not serialize null element");

            //check if unknownnode type - if so, return saved metadata
            //unknown node type will need onbeforeserialize to set guid and edges and all the things
            var typeInfo = GetTypeSerializableAsString(item.GetType());
            var data = JsonUtility.ToJson(item, true);

            if (string.IsNullOrEmpty(data))
                throw new ArgumentException(string.Format("Can not serialize {0}", item));
            

            return new JSONSerializedElement
            {
                typeInfo = typeInfo,
                JSONnodeData = data
            };
        }

        static TypeSerializationInfo DoTypeRemap(TypeSerializationInfo info, Dictionary<TypeSerializationInfo, TypeSerializationInfo> remapper)
        {
            TypeSerializationInfo foundInfo;
            if (remapper.TryGetValue(info, out foundInfo))
                return foundInfo;
            return info;
        }

        public static T Deserialize<T>(JSONSerializedElement item, Dictionary<TypeSerializationInfo, TypeSerializationInfo> remapper,  params object[] constructorArgs) where T : class
        {
            T instance;
            if (typeof(T) == typeof(JsonObject) || typeof(T).IsSubclassOf(typeof(JsonObject)))
            {
                try
                {
                    var culture = CultureInfo.CurrentCulture;
                    var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                    instance = Activator.CreateInstance(typeof(T), flags, null, constructorArgs, culture) as T;
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("Could not construct instance of: {0}", typeof(T)), e);
                }

                MultiJson.Deserialize(instance as JsonObject, item.JSONnodeData);
                return instance;
            }

            if (!item.typeInfo.IsValid() || string.IsNullOrEmpty(item.JSONnodeData))
                throw new ArgumentException(string.Format("Can not deserialize {0}, it is invalid", item));

            TypeSerializationInfo info = item.typeInfo;
            info.fullName = info.fullName.Replace("UnityEngine.MaterialGraph", "UnityEditor.ShaderGraph");
            info.fullName = info.fullName.Replace("UnityEngine.Graphing", "UnityEditor.Graphing");
            if (remapper != null)
                info = DoTypeRemap(info, remapper);

            var type = GetTypeFromSerializedString(info);
            //if type is null but T is an abstract material node, instead we create an unknowntype node
            if (type == null)
            {
                    throw new ArgumentException(string.Format("Can not deserialize ({0}), type is invalid", info.fullName));
            }

            try
            {
                var culture = CultureInfo.CurrentCulture;
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                instance = Activator.CreateInstance(type, flags, null, constructorArgs, culture) as T;
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Could not construct instance of: {0}", type), e);
            }

            if (instance != null)
            {
                JsonUtility.FromJsonOverwrite(item.JSONnodeData, instance);
                return instance;
            }
            Debug.Log("UhOh");
            return null;
        }

        public static List<JSONSerializedElement> Serialize<T>(IEnumerable<T> list)
        {
            var result = new List<JSONSerializedElement>();
            if (list == null)
                return result;

            foreach (var element in list)
            {
                try
                {
                    result.Add(Serialize(element));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            return result;
        }

        public static List<T> Deserialize<T>(IEnumerable<JSONSerializedElement> list, Dictionary<TypeSerializationInfo, TypeSerializationInfo> remapper, params object[] constructorArgs) where T : class
        {
            var result = new List<T>();
            if (list == null)
                return result;

            foreach (var element in list)
            {
                try
                {
                    result.Add(Deserialize<T>(element, remapper));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError(element.JSONnodeData);
                }
            }
            return result;
        }
    }
}
