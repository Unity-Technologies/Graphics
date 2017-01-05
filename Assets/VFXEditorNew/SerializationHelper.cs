using System;
using System.Collections.Generic;
using System.Reflection;

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

        public static JSONSerializedElement Serialize<T>(T item)
        {
            if (item == null)
                throw new ArgumentNullException("item", "Can not serialize null element");

            var typeInfo = GetTypeSerializableAsString(item.GetType());
            var data = JsonUtility.ToJson(item, true);

            if (string.IsNullOrEmpty(data))
                throw new ArgumentException(string.Format("Can not serialize {0}", item));
            ;

            return new JSONSerializedElement
                   {
                       typeInfo = typeInfo,
                       JSONnodeData = data
                   };
        }

        private static TypeSerializationInfo DoTypeRemap(TypeSerializationInfo info, Dictionary<TypeSerializationInfo, TypeSerializationInfo> remapper)
        {
            TypeSerializationInfo foundInfo;
            if (remapper.TryGetValue(info, out foundInfo))
                return foundInfo;
            return info;
        }

        public static T Deserialize<T>(JSONSerializedElement item, Dictionary<TypeSerializationInfo, TypeSerializationInfo> remapper,  params object[] constructorArgs) where T : class
        {
            if (!item.typeInfo.IsValid() || string.IsNullOrEmpty(item.JSONnodeData))
                throw new ArgumentException(string.Format("Can not deserialize {0}, it is invalid", item));

            TypeSerializationInfo info = item.typeInfo;
            if (remapper != null)
                info = DoTypeRemap(info, remapper);

            var type = GetTypeFromSerializedString(info);
            if (type == null)
                throw new ArgumentException(string.Format("Can not deserialize {0}, type {1} is invalid", info));

            T instance;
            try
            {
                instance = Activator.CreateInstance(
                    type, 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, 
                    null, 
                    constructorArgs, 
                    null) as T;
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
                }
            }
            return result;
        }
    }
}
