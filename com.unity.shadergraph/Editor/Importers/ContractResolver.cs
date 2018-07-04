using System;
using System.Runtime.Serialization;
using Newtonsoft.Json.Serialization;
using UnityEditor.Importers;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEngine.ShaderGraph
{
    public class ContractResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            var jsonContract = base.CreateContract(objectType);

            if (objectType == typeof(Vector2))
                jsonContract.Converter = new Vector2Converter();
            else if (objectType == typeof(Vector3))
                jsonContract.Converter = new Vector3Converter();
            else if (objectType == typeof(Vector4))
                jsonContract.Converter = new Vector4Converter();
            else if (objectType == typeof(Quaternion))
                jsonContract.Converter = new QuaternionConverter();
            else if (objectType == typeof(Color))
                jsonContract.Converter = new ColorConverter();
            else if (objectType == typeof(Bounds))
                jsonContract.Converter = new BoundsConverter();
            else if (objectType == typeof(Rect))
                jsonContract.Converter = new RectConverter();
            else if (Attribute.IsDefined(objectType, typeof(JsonVersionedAttribute), true))
                jsonContract.Converter = new UpgradeConverter(objectType);
            else if (!Attribute.IsDefined(objectType, typeof(DataContractAttribute), true)
                && Attribute.IsDefined(objectType, typeof(SerializableAttribute), true)
                && !objectType.IsAbstract
                && !objectType.IsGenericType)
                jsonContract.Converter = new UnityConverter();

            return jsonContract;
        }
    }
}
