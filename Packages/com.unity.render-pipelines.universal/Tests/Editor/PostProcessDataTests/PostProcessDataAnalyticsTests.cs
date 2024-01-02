using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.Pool;
using UnityEngine.Rendering.Universal;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.Rendering.Universal
{
    class PostProcessDataAnalyticsTests
    {
        private static PostProcessDataAnalytics.Analytic.PropertyToGUIDs[] s_Datas = new[]
        {
            new PostProcessDataAnalytics.Analytic.PropertyToGUIDs()
            {
                propertyName = "bloomPS",
                defaultGUID = "5f1864addb451f54bae8c86d230f736e",
                usedGUIDs = new List<string>()
                {
                    "a176945d0e562084691bce8e11084c6a",
                }
            },
            new PostProcessDataAnalytics.Analytic.PropertyToGUIDs()
            {
                propertyName = "bloomPS",
                defaultGUID = "5f1864addb451f54bae8c86d230f736e",
                usedGUIDs = new List<string>()
                {
                    "a176945d0e562084691bce8e11084c6a",
                    "a176945d0e562084691bce8e11084c6a"
                }
            },
            new PostProcessDataAnalytics.Analytic.PropertyToGUIDs()
                {
                    propertyName = "bloomPS",
                    defaultGUID = "5f1864addb451f54bae8c86d230f736e",
                    usedGUIDs = new List<string>()
                    {
                        "ace59f7f124432f4f923bd3bfa93e0ad",
                        "a176945d0e562084691bce8e11084c6a"
                    }
                }
        };

        private static IEnumerable<(string[], PostProcessDataAnalytics.Analytic.PropertyToGUIDs)> TestDataExtractData()
        {
            yield return (
                new string[]
                {
                    "Packages/com.unity.render-pipelines.universal/Tests/Editor/PostProcessDataTests/PostProcessData BloomPS Different.asset",
                },
                s_Datas[0]);

            yield return (
                new string[]
                {
                    "Packages/com.unity.render-pipelines.universal/Tests/Editor/PostProcessDataTests/PostProcessData BloomPS Different.asset",
                    "Packages/com.unity.render-pipelines.universal/Tests/Editor/PostProcessDataTests/PostProcessData BloomPS Different.asset",
                },
                s_Datas[1]);

            yield return (
                new string[]
                {
                    "Packages/com.unity.render-pipelines.universal/Tests/Editor/PostProcessDataTests/PostProcessData BloomPS Different 1.asset",
                    "Packages/com.unity.render-pipelines.universal/Tests/Editor/PostProcessDataTests/PostProcessData BloomPS Different.asset",
                },
                s_Datas[2]);
        }

        [Test][TestCaseSource(nameof(TestDataExtractData))]
        public void DataIsExtractedCorrectly((string[] input, PostProcessDataAnalytics.Analytic.PropertyToGUIDs expected) testCase)
        {
            using (ListPool<PostProcessData>.Get(out var tmp))
            {
                foreach (var i in testCase.input)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<PostProcessData>(i);
                    Assert.IsNotNull(asset, $"Unable to load asset at {i}");
                    tmp.Add(asset);
                }

                var data = PostProcessDataAnalytics.Analytic.ExtractData(tmp);
                var bloom = data.FirstOrDefault(d =>
                    d.propertyName.Equals(nameof(PostProcessData.ShaderResources.bloomPS)));
                Assert.IsNotNull(bloom);
                Assert.AreEqual(testCase.expected.propertyName, bloom.propertyName);
                Assert.AreEqual(testCase.expected.defaultGUID, bloom.defaultGUID);
                CollectionAssert.AreEqual(testCase.expected.usedGUIDs, bloom.usedGUIDs);
            }
        }

        private static IEnumerable<(PostProcessDataAnalytics.Analytic.PropertyToGUIDs, PostProcessDataAnalytics.Analytic.Usage)> TestData()
        {
            yield return (s_Datas[0], PostProcessDataAnalytics.Analytic.Usage.ModifiedForTheProject);
            yield return (s_Datas[1], PostProcessDataAnalytics.Analytic.Usage.ModifiedForTheProject);
            yield return (s_Datas[2], PostProcessDataAnalytics.Analytic.Usage.ModifiedForEachQualityLevel);
        }

        [Test]
        [TestCaseSource(nameof(TestData))]
        public void GenerateMapWithDifferencesTests((PostProcessDataAnalytics.Analytic.PropertyToGUIDs input, PostProcessDataAnalytics.Analytic.Usage expected) testCase)
        {
            var dataToSent =
                PostProcessDataAnalytics.Analytic.GatherDataToBeSent(new [] {testCase.input });

            int count = 0;
            foreach (PostProcessDataAnalytics.Analytic.AnalyticsData i in dataToSent)
            {
                if (i.property.Equals(nameof(PostProcessData.ShaderResources.bloomPS)))
                    Assert.AreEqual(i.usage, testCase.expected.ToString());

                ++count;
            }
            Assert.AreEqual(1, count);
        }
    }
}

