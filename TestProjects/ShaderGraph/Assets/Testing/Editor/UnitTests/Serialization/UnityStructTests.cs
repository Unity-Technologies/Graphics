using Importers;
using Importers.Converters;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ShaderGraph;

namespace UnityEditor.ShaderGraph.UnitTests.Serialization
{
    public class UnityStructTests
    {
        [Test]
        public void CanSerializeAndDeserializeVector2()
        {
            var originalValue = new Vector2(1.123f, 655f);

            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new ContractResolver() };
            var json = JsonConvert.SerializeObject(originalValue, jsonSerializerSettings);
            var deserializedValue = JsonConvert.DeserializeObject<Vector2>(json, jsonSerializerSettings);

            Assert.AreEqual(originalValue, deserializedValue);
        }

        [Test]
        public void CanSerializeAndDeserializeVector3()
        {
            var originalValue = new Vector3(1.123f, 655f, 43.344f);

            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new ContractResolver() };
            var json = JsonConvert.SerializeObject(originalValue, jsonSerializerSettings);
            var deserializedValue = JsonConvert.DeserializeObject<Vector3>(json, jsonSerializerSettings);

            Assert.AreEqual(originalValue, deserializedValue);
        }

        [Test]
        public void CanSerializeAndDeserializeVector4()
        {
            var originalValue = new Vector4(1.123f, 655f, 43.344f, 985885.34445f);

            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new ContractResolver() };
            var json = JsonConvert.SerializeObject(originalValue, jsonSerializerSettings);
            var deserializedValue = JsonConvert.DeserializeObject<Vector4>(json, jsonSerializerSettings);

            Assert.AreEqual(originalValue, deserializedValue);
        }

        [Test]
        public void CanSerializeAndDeserializeQuaternion()
        {
            var originalValue = new Quaternion(1.123f, 655f, 43.344f, 985885.34445f);

            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new ContractResolver() };
            var json = JsonConvert.SerializeObject(originalValue, jsonSerializerSettings);
            var deserializedValue = JsonConvert.DeserializeObject<Quaternion>(json, jsonSerializerSettings);

            Assert.AreEqual(originalValue, deserializedValue);
        }

        [Test]
        public void CanSerializeAndDeserializeColor()
        {
            var originalValue = new Color(1.123f, 655f, 43.344f, 985885.34445f);

            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new ContractResolver() };
            var json = JsonConvert.SerializeObject(originalValue, jsonSerializerSettings);
            var deserializedValue = JsonConvert.DeserializeObject<Color>(json, jsonSerializerSettings);

            Assert.AreEqual(originalValue, deserializedValue);
        }

        [Test]
        public void CanSerializeAndDeserializeBounds()
        {
            var originalValue = new Bounds(new Vector3(1.123f, 655f, 43.344f), new Vector3(23.3f, 0.4553f, 985885.34445f));

            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new ContractResolver() };
            var json = JsonConvert.SerializeObject(originalValue, jsonSerializerSettings);
            var deserializedValue = JsonConvert.DeserializeObject<Bounds>(json, jsonSerializerSettings);

            Assert.AreEqual(originalValue, deserializedValue);
        }

        [Test]
        public void CanSerializeAndDeserializeRect()
        {
            var originalValue = new Rect(1.123f, 655f, 43.344f, 985885.34445f);

            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new ContractResolver() };
            var json = JsonConvert.SerializeObject(originalValue, jsonSerializerSettings);
            var deserializedValue = JsonConvert.DeserializeObject<Rect>(json, jsonSerializerSettings);

            Assert.AreEqual(originalValue, deserializedValue);
        }
    }
}
