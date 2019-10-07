using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    sealed class ContractResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var result = base.GetSerializableMembers(objectType);

            while (objectType != null)
            {
                var fieldInfos = objectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var fieldInfo in fieldInfos)
                {
                    if (!result.Any(x => x == fieldInfo || x.Name == fieldInfo.Name) && fieldInfo.GetCustomAttribute<SerializeField>() != null)
                    {
                        result.Add(fieldInfo);
                    }
                }

                objectType = objectType.BaseType;
            }

            return result;
        }

        protected override JsonProperty CreateProperty(MemberInfo memberInfo, MemberSerialization memberSerialization)
        {
            memberSerialization = MemberSerialization.OptIn;
            if (memberInfo is FieldInfo fieldInfo
                && ((fieldInfo.IsPublic && fieldInfo.GetCustomAttribute<NonSerializedAttribute>() == null)
                    || fieldInfo.GetCustomAttribute<SerializeField>() != null))
            {
                memberSerialization = MemberSerialization.Fields;
            }

            var jsonProperty = base.CreateProperty(memberInfo, memberSerialization);
            return jsonProperty;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var jsonProperties = (List<JsonProperty>)base.CreateProperties(type, memberSerialization);
            var extraProperties = new List<JsonProperty>();
            foreach (var jsonProperty in jsonProperties)
            {
                var attributes = jsonProperty.AttributeProvider.GetAttributes(true);
                var propertyName = jsonProperty.PropertyName;
                foreach (var attribute in attributes)
                {
                    if (attribute is JsonUpgradeAttribute upgradeAttribute)
                    {
                        var upgradeProperty = jsonProperty.Clone();
                        upgradeProperty.Readable = false;
                        upgradeProperty.PropertyName = upgradeAttribute.name;
                        if (upgradeAttribute.converterType != null)
                        {
                            upgradeProperty.Converter = (JsonConverter)Activator.CreateInstance(upgradeAttribute.converterType,
                                upgradeAttribute.converterParams);
                        }

                        extraProperties.Add(upgradeProperty);
                    }
                }

                if (propertyName.StartsWith("m_") && propertyName.Length > 2)
                {
                    var clonedProperty = jsonProperty.Clone();
                    clonedProperty.PropertyName = char.ToLower(propertyName[2]) + propertyName.Substring(3);
                    extraProperties.Add(clonedProperty);
                    jsonProperty.Readable = false;
                }
            }

            // TODO: Verify if this is still needed
            foreach (var extraProperty in extraProperties)
            {
                jsonProperties.RemoveAll(x => x.PropertyName == extraProperty.PropertyName);
                jsonProperties.Add(extraProperty);
            }

            jsonProperties.Sort((x1, x2) => x1.PropertyName.CompareTo(x2.PropertyName));
            return jsonProperties;
        }
    }
}
