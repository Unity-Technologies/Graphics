using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ShaderGraph;

namespace UnityEditor.ShaderGraph.UnitTests.Serialization
{
    public class GraphTests
    {
        [Test]
        public void CanSerializeAndDeserializeMaterialGraph()
        {
            var graph = new GraphData();
            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new ContractResolver() };
            var json = JToken.Parse(JsonConvert.SerializeObject(graph, jsonSerializerSettings)).ToString(Formatting.Indented);
            Debug.Log(json);
        }
    }
}
