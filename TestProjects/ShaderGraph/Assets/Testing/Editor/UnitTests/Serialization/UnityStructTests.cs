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
        public void CanSerializeAndDeserializeVector4()
        {
            var originalVector = new Vector4(1.123f, 655f, 43.344f, 985885.34445f);

            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new ContractResolver() };
            var json = JsonConvert.SerializeObject(originalVector, jsonSerializerSettings);
            var deserializedVector = JsonConvert.DeserializeObject<Vector4>(json, jsonSerializerSettings);

            Assert.AreEqual(originalVector, deserializedVector);
        }
    }
}
