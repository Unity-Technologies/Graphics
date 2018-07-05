using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Importers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEditor.Importers;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEngine.ShaderGraph
{
    class ContractResolver : DefaultContractResolver
    {
        public static readonly ContractResolver instance = new ContractResolver();

        ContractResolver()
        {
            NamingStrategy = new UnityNamingStrategy();
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);

            if (objectType == typeof(Vector2))
                contract.Converter = new Vector2Converter();
            else if (objectType == typeof(Vector3))
                contract.Converter = new Vector3Converter();
            else if (objectType == typeof(Vector4))
                contract.Converter = new Vector4Converter();
            else if (objectType == typeof(Quaternion))
                contract.Converter = new QuaternionConverter();
            else if (objectType == typeof(Color))
                contract.Converter = new ColorConverter();
            else if (objectType == typeof(Bounds))
                contract.Converter = new BoundsConverter();
            else if (objectType == typeof(Rect))
                contract.Converter = new RectConverter();
            else if (Attribute.IsDefined(objectType, typeof(JsonVersionedAttribute), true))
            {
                contract.Converter = new UpgradeConverter();
                var property = new JsonProperty
                {
                    DeclaringType = contract.UnderlyingType,
                    PropertyName = "$version",
                    UnderlyingName = "$version",
                    PropertyType = typeof(int),
                    ValueProvider = new ConstantValueProvider(2),
                    Readable = true,
                    Writable = false,
                };
                contract.Properties.Insert(0, property);
            }
            else if (!Attribute.IsDefined(objectType, typeof(DataContractAttribute), true)
                && !Attribute.IsDefined(objectType, typeof(JsonObjectAttribute), true)
                && !Attribute.IsDefined(objectType, typeof(JsonDictionaryAttribute), true)
                && !Attribute.IsDefined(objectType, typeof(JsonArrayAttribute), true)
                && Attribute.IsDefined(objectType, typeof(SerializableAttribute), true)
                && !objectType.IsAbstract
                && !objectType.IsGenericType)
                contract.Converter = new UnityConverter();



            return contract;
        }
    }
}
