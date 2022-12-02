using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Analytics;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class HDAnalyticsDefaults
    {
        const int k_MaxEventsPerHour = 10;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.hdrp";

        struct DefaultsEventData
        {
            internal const string k_EventName = "uHDRPDefaults";

            // Naming convention for analytics data
            public string[] default_settings;
        }

        // We only need to send this event manually when we add new members or change values of the HDRP asset.
        [MenuItem("internal:Edit/Rendering/Analytics/Generate HDRP default values analytics", priority = 1)]
        static void GenerateDefaultValues()
        {
            if (!EditorAnalytics.enabled || EditorAnalytics.RegisterEventWithLimit(DefaultsEventData.k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey) != AnalyticsResult.Ok)
                return;

            var data = new DefaultsEventData()
            {
                default_settings = RenderPipelineSettings.NewDefault().ToNestedColumn()
            };

            EditorAnalytics.SendEventWithLimit(DefaultsEventData.k_EventName, data);
        }
    }

    class HDAnalyticsTests
    {
        [Test]
        public void ToNestedColumnsWithDefaultsReturnsTheCorrectValue()
        {
            var setting = new IntScalableSetting(new[] { 1, 2, 3 }, ScalableSettingSchemaId.With3Levels);
            RenderPipelineSettings defaults = RenderPipelineSettings.NewDefault();
            RenderPipelineSettings current = RenderPipelineSettings.NewDefault();
            current.maximumLODLevel = setting;
            current.supportDecals = false;

            var diff = current.ToNestedColumnWithDefault(defaults, true);
            Assert.AreEqual(new string[] { "{\"supportDecals\":\"False\"}", "{\"maximumLODLevel.m_Values\":\"[1,2,3]\"}" }, diff);
        }

        const string k_DefaultsDirectory = "Packages/com.unity.render-pipelines.high-definition/Tests/Editor/HDAnalyticsTests_Defaults.txt";

        [Test][Ignore("Sync RP Asset values")]
        public void CheckDefaultAnalyticsAreUpToDateOnBigQuery()
        {
            var currentDefaults = string.Join(",", RenderPipelineSettings.NewDefault().ToNestedColumn());

            //File.WriteAllText(k_DefaultsDirectory, currentDefaults); // Uncomment this line to update the file

            var defaultsFromFile = File.ReadAllText(k_DefaultsDirectory);

            Assert.AreEqual(defaultsFromFile, currentDefaults, @$"Modifications where found on the {nameof(RenderPipelineSettings)}. Whenever you update the {nameof(HDRenderPipelineAsset)} you must:
                - Update the default values on BigQuery (Edit/Rendering/Analytics/Generate HDRP default values analytics)
                - Regenerate the file {k_DefaultsDirectory}.");
        }
    }
}
