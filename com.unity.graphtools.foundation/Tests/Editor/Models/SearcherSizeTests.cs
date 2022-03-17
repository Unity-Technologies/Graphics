using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class SearcherSizeTests
    {
        static readonly Vector2 k_DefaultSize = new Vector2(123, 456);
        const float k_DefaultRatio = 1.25f;
        static readonly Vector2 k_CreateNodeDefaultSize = new Vector2(789, 345);
        const float k_CreateNodeDefaultRatio = 2.05f;

        Preferences m_Preferences;

        [SetUp]
        public void Setup()
        {
            m_Preferences = Preferences.CreateTransient(nameof(SearcherSizeTests));
            m_Preferences.ResetSearcherSizes();
            m_Preferences.SetInitialSearcherSize(null, k_DefaultSize, k_DefaultRatio);
            m_Preferences.SetInitialSearcherSize(SearcherService.Usage.CreateNode, k_CreateNodeDefaultSize, k_CreateNodeDefaultRatio);
        }

        [Test]
        public void TestsSearcherSizeComputationAreCorrect()
        {
            var searcherSize = m_Preferences.GetSearcherSize(SearcherService.Usage.CreateNode);
            Assert.AreEqual(k_CreateNodeDefaultSize, searcherSize.Size);
            Assert.AreEqual(k_CreateNodeDefaultRatio, searcherSize.RightLeftRatio);

            searcherSize = m_Preferences.GetSearcherSize(null);
            Assert.AreEqual(k_DefaultSize, searcherSize.Size);
            Assert.AreEqual(k_DefaultRatio, searcherSize.RightLeftRatio);

            string someUsage = "some-usage";
            searcherSize = m_Preferences.GetSearcherSize(someUsage);
            Assert.AreEqual(k_DefaultSize, searcherSize.Size);
            Assert.AreEqual(k_DefaultRatio, searcherSize.RightLeftRatio);

            Vector2 newSize = new Vector2(312.0f, 357.0f);
            float newRatio = 1.1f;

            m_Preferences.SetSearcherSize(SearcherService.Usage.CreateNode, newSize, newRatio);
            searcherSize = m_Preferences.GetSearcherSize(SearcherService.Usage.CreateNode);
            Assert.AreEqual(newSize, searcherSize.Size);
            Assert.AreEqual(newRatio, searcherSize.RightLeftRatio);

            m_Preferences.SetSearcherSize(someUsage, newSize, newRatio);
            searcherSize = m_Preferences.GetSearcherSize(someUsage);
            Assert.AreEqual(newSize, searcherSize.Size);
            Assert.AreEqual(newRatio, searcherSize.RightLeftRatio);

            searcherSize = m_Preferences.GetSearcherSize(null);
            Assert.AreEqual(k_DefaultSize, searcherSize.Size);
            Assert.AreEqual(k_DefaultRatio, searcherSize.RightLeftRatio);
        }
    }
}
