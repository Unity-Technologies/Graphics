using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Tests
{
    public class HelperTests
    {
        private Dictionary<Type, int> assetTypeCount;
        private string k_targetFolder = "Assets/HelperResources";
        private List<string> testStrings;
        private string[] substrings;

        [SetUp]
        public void SetUp()
        {
            testStrings = new List<string>
            {
                "",
                " ",
                "\t",
                "\n",
                "normalString",
                "With Special Characters !@#$%^&*()",
                new string('a', 10000),
                "UPPER",
                "lower",
                "MiXeD",
                null
            };

            substrings = new[]
            {
                "normal",
                " ",
                "Special",
                "UPPER",
                "lower",
                "mixed",
                null
            };

            assetTypeCount = new Dictionary<Type, int>();
            string[] assetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (string path in assetPaths)
            {
                if (path.StartsWith(k_targetFolder))
                {
                    Object asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
                    if (asset != null)
                    {
                        Type assetType = asset.GetType();

                        if (assetTypeCount.ContainsKey(assetType))
                        {
                            assetTypeCount[assetType]++;
                        }
                        else
                        {
                            assetTypeCount.Add(assetType, 1);
                        }
                    }
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            assetTypeCount.Clear();
        }

        [Test]
        public void ValidateContainsAnyString()
        {
            Assert.False(testStrings[0].ContainsAny(substrings));
            Assert.True(testStrings[1].ContainsAny(substrings));
            Assert.False(testStrings[2].ContainsAny(substrings));
            Assert.False(testStrings[3].ContainsAny(substrings));
            Assert.True(testStrings[4].ContainsAny(substrings));
            Assert.True(testStrings[5].ContainsAny(substrings));
            Assert.False(testStrings[6].ContainsAny(substrings));
            Assert.True(testStrings[7].ContainsAny(substrings));
            Assert.True(testStrings[8].ContainsAny(substrings));
            Assert.False(testStrings[9].ContainsAny(substrings));
            Assert.False(testStrings[10].ContainsAny(substrings));
        }

        [Test]
        public void ValidateFindDataOfType()
        {
            bool success = true;
            foreach (var entry in assetTypeCount)
            {
                Type type = entry.Key;
                int count = 0;

                MethodInfo genericMethod = typeof(AssetDatabaseHelper).GetMethod("FindAssets");
                MethodInfo method = genericMethod.MakeGenericMethod(type);

                object[] parameters = new object[] { null };
                var assets = method.Invoke(null, parameters) as IEnumerable;

                foreach (var asset in assets)
                {
                    if (AssetDatabase.GetAssetPath(asset as Object).StartsWith(k_targetFolder))
                    {
                        count++;
                    }
                }
                if (count != entry.Value)
                {
                    success = false;
                    break;
                }
            }
            Assert.True(success);
        }
    }
}
