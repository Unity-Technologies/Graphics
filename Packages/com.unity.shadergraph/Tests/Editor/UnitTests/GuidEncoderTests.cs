using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    // Expected names are copied from material files saved when Shader Graph ran on Mono, never recomputed
    class GuidEncoderTests
    {
        [TestCase("70bb7ddd-238c-44d0-8e7a-b792ddd7c5b3", "BF3EAE1A")]
        [TestCase("fde825b2-079c-4d51-9a42-774b8ead11dc", "76EF7348")]
        [TestCase("c82f835f-d40b-4574-a801-dac572ec4b23", "D39A4978")]
        [TestCase("6bbf43fd-2742-4c69-8b00-becd8e8dc52f", "90AA7A8")]
        public void EncodeLegacy_MatchesNamesStoredInMaterials(string guid, string expected)
        {
            Assert.AreEqual(expected, GuidEncoder.EncodeLegacy(Guid.Parse(guid)));
        }

        [Test]
        public void LegacyGraphUpgrade_GeneratesReferenceNamesStoredInMaterials()
        {
            var legacyGraph = new GraphData0
            {
                m_SerializedProperties = new List<SerializationHelper.JSONSerializedElement>
                {
                    LegacyProperty("TextureShaderProperty", "Normal", "70bb7ddd-238c-44d0-8e7a-b792ddd7c5b3", @"{""m_SerializedTexture"": """", ""m_Guid"": """"}"),
                    LegacyProperty("Vector1ShaderProperty", "Alpha Clip Threshold", "6bbf43fd-2742-4c69-8b00-becd8e8dc52f", "0.5"),
                    LegacyProperty("ColorShaderProperty", "Color", "5efc6675-c883-47a9-a276-c4e784c4a361", @"{""r"": 1.0, ""g"": 1.0, ""b"": 1.0, ""a"": 1.0}"),
                    LegacyProperty("Vector2ShaderProperty", "Tiling", "0324b5a9-eb4a-48ef-b64b-0aff32410ef0", @"{""x"": 4.0, ""y"": 2.0}"),
                },
            };
            var graph = new GraphData();

            MultiJson.Deserialize(graph, JsonUtility.ToJson(legacyGraph));

            string[] referenceNames = graph.properties.Select(p => p.referenceName).ToArray();
            CollectionAssert.AreEqual(
                new[] { "Texture2D_BF3EAE1A", "Vector1_90AA7A8", "Color_6DBB962E", "Vector2_C19C925C" },
                referenceNames);
        }

        static SerializationHelper.JSONSerializedElement LegacyProperty(string typeName, string displayName, string guid, string valueJson)
        {
            return new SerializationHelper.JSONSerializedElement
            {
                typeInfo = new SerializationHelper.TypeSerializationInfo { fullName = "UnityEditor.ShaderGraph." + typeName },
                JSONnodeData = @"{""m_Value"": " + valueJson +
                    @", ""m_Name"": """ + displayName +
                    @""", ""m_GeneratePropertyBlock"": true, ""m_Guid"": {""m_GuidSerialized"": """ + guid +
                    @"""}, ""m_OverrideReferenceName"": """"}",
            };
        }
    }
}
