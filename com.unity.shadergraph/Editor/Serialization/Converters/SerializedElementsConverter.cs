using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    sealed class SerializedElementsConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var baseType = objectType.GenericTypeArguments[0];
            var result = (IList)(existingValue ?? Activator.CreateInstance(objectType));
            var typeRemapping = GraphUtil.GetLegacyTypeRemapping();
            var serializedElements = serializer.Deserialize<List<SerializationHelper.JSONSerializedElement>>(reader);
            var referenceResolver = (ReferenceResolver)serializer.ReferenceResolver;
            var set = referenceResolver.jsonStore;
            var jObjects = referenceResolver.jObjects;
            foreach (var serializedElement in serializedElements)
            {
                var typeInfo = SerializationHelper.DoTypeRemap(serializedElement.typeInfo, typeRemapping);
                Type type = null;
                foreach (var candidateType in TypeCache.GetTypesDerivedFrom(baseType))
                {
                    if (candidateType.FullName == typeInfo.fullName)
                    {
                        type = candidateType;
                        break;
                    }
                }

                if (type == null)
                {
                    Debug.LogWarning($"Type {typeInfo.fullName} not found.");
                    return null;
                }

                var element = (IJsonObject)Activator.CreateInstance(type);
                result.Add(element);
                set.Add(element);
                jObjects.Add(new DeserializationPair(element, JObject.Parse(serializedElement.JSONnodeData)));
            }

            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType
                && objectType.GetGenericTypeDefinition() == typeof(List<>)
                && typeof(IJsonObject).IsAssignableFrom(objectType.GenericTypeArguments[0]);
        }

        public override bool CanWrite => false;
    }
}
