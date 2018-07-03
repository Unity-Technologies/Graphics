using System;
using System.Collections;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ShaderGraph;
using UnityEngine.Assertions;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.UnitTests.Serialization
{
    public class UnityStructTests
    {
        [Serializable]
        struct BoundsWrapper
        {
            public Bounds bounds;

            public BoundsWrapper(Bounds bounds)
            {
                this.bounds = bounds;
            }

            public override string ToString()
            {
                return bounds.ToString();
            }
        }

        [Serializable]
        struct RectWrapper
        {
            public Rect rect;

            public RectWrapper(Rect rect)
            {
                this.rect = rect;
            }

            public override string ToString()
            {
                return rect.ToString();
            }
        }

        static float NextFloat(System.Random random, float minValue = -1024f, float maxValue = 1024f)
        {
            return (float)(random.NextDouble() * (maxValue - minValue) + minValue);
        }

        static IEnumerable TestCases()
        {
            var random = new System.Random(8371);
            yield return new Vector2(NextFloat(random), NextFloat(random));
            yield return new Vector3(NextFloat(random), NextFloat(random), NextFloat(random));
            yield return new Vector4(NextFloat(random), NextFloat(random), NextFloat(random), NextFloat(random));
            yield return new Quaternion(NextFloat(random), NextFloat(random), NextFloat(random), NextFloat(random));
            yield return new Color(NextFloat(random), NextFloat(random), NextFloat(random), NextFloat(random));
            yield return new BoundsWrapper(new Bounds(new Vector3(NextFloat(random), NextFloat(random), NextFloat(random)), new Vector3(NextFloat(random), NextFloat(random), NextFloat(random))));
            yield return new RectWrapper(new Rect(NextFloat(random), NextFloat(random), NextFloat(random), NextFloat(random)));
        }

        [TestCaseSource("TestCases")]
        public void CanSerializeAndDeserializeValue<T>(T typedValue)
        {
            object originalValue = typedValue;
            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new ContractResolver() };
            var json = JsonConvert.SerializeObject(originalValue, jsonSerializerSettings);
            var deserializedValue = JsonConvert.DeserializeObject<T>(json, jsonSerializerSettings);

            Assert.AreEqual(typedValue, deserializedValue);
        }

        [TestCaseSource("TestCases")]
        public void CanDeserializeUnitySerializedValue<T>(T originalValue)
        {
            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new ContractResolver() };
            var unityJson = JsonUtility.ToJson(originalValue);
            var deserializedValue = JsonConvert.DeserializeObject<T>(unityJson, jsonSerializerSettings);

            Assert.AreEqual(originalValue, deserializedValue);
        }
    }
}
