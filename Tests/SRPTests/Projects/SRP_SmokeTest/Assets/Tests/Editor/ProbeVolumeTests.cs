using System;
using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Tests
{
    public class ProbeVolumeTests
    {
        [Test]
        public void GetHDRPLightingGroupReturnsNotNull()
        {
            var lightingGroup = ProbeVolumeEditor.GetHDRPLightingGroup();
            Assert.That((int)lightingGroup, Is.EqualTo(1 << 5));
        }

        [Test]
        public void GetHDRPProbeVolumeEnumReturnsNotNull()
        {
            var volumeEnum = ProbeVolumeEditor.GetHDRPProbeVolumeEnum();
            Assert.That((int)volumeEnum, Is.EqualTo(1 << 1));
        }

        [Test]
        public void GetHDRPQualitySettingsHelpBoxReturnsNotNull()
        {
            var helpBoxMethod = ProbeVolumeEditor.GetHDRPQualitySettingsHelpBox();
            Assert.IsNotNull(helpBoxMethod);
        }

        [Test]
        public void GetURPLightingGroupReturnsNotNull()
        {
            var lightingGroup = ProbeVolumeEditor.GetURPLightingGroup();
            Assert.That((int)lightingGroup, Is.EqualTo(1 << 3));
        }

        [Test]
        public void GetURPQualitySettingsHelpBoxReturnsNotNull()
        {
            var helpBoxMethod = ProbeVolumeEditor.GetURPQualitySettingsHelpBox();
            Assert.IsNotNull(helpBoxMethod);
        }

        static TestCaseData[] s_IndexOfCases =
        {
            new(new[] { "a", "b", "c" }, "b", 1),
            new(new[] { "a", "b", "c" }, "d", -1),
            new(new[] { "a", "b", "c" }, "a", 0),
            new(new[] { "a", "b", "c" }, "c", 2),
            new(new[] { "a", "b", "c" }, string.Empty, -1),
            new(Array.Empty<string>(), "c", -1),
            new(Array.Empty<string>(), "", -1),
        };

        [Test, TestCaseSource(nameof(s_IndexOfCases))]
        public void IndexOf(string[] array, string value, int expectedIndex)
        {
            var index = ProbeVolumeEditor.IndexOf(array, value);
            Assert.That(index, Is.EqualTo(expectedIndex));
        }

        [Test]
        public void GetURPSupportsLayersDoesntThrowException()
        {
            var asset = AssetDatabase.LoadMainAssetAtPath("Assets/PipelineAssets/UniversalRenderPipelineAsset.asset") as RenderPipelineAsset;
            var supportsLayers = ProbeVolumeBakingSetEditor.GetURPSupportsLayers(typeof(UniversalRenderPipelineAsset), asset);
            Assert.IsNotNull(supportsLayers);
        }

        [Test]
        public void GetHDRPSupportsLayersDoesntThrowException()
        {
            var asset = AssetDatabase.LoadMainAssetAtPath("Assets/PipelineAssets/HDRenderPipelineAsset.asset") as RenderPipelineAsset;
            var supportsLayers = ProbeVolumeBakingSetEditor.GetHDRPSupportsLayers(typeof(HDRenderPipelineAsset), asset);
            Assert.IsNotNull(supportsLayers);
        }
    }
}
